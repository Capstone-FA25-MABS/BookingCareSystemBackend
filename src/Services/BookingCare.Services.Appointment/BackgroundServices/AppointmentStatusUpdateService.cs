using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Helpers;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.BackgroundServices;

/// <summary>
/// Background service that automatically updates appointment statuses based on appointment date
/// - PENDING appointments past their date -> CANCELLED (with overdue reason)
/// - CONFIRMED appointments past their date -> COMPLETED
/// </summary>
public class AppointmentStatusUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentStatusUpdateService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _startupDelay;
    private const string OVERDUE_CANCELLATION_REASON = "Tự động hủy - Quá hạn ngày hẹn";
    private const string NO_DOCTOR_ASSIGNED_REASON = "Tự động hủy - Bệnh viện không gán bác sĩ trước ngày hẹn";
    private const string SYSTEM_USER = "SYSTEM";

    public AppointmentStatusUpdateService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentStatusUpdateService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Read configuration with defaults
        var checkIntervalHours = configuration
            .GetValue<int>("BackgroundServices:AppointmentStatusUpdate:CheckIntervalHours", 1);
        var startupDelaySeconds = configuration
            .GetValue<int>("BackgroundServices:AppointmentStatusUpdate:StartupDelaySeconds", 30);

        _checkInterval = TimeSpan.FromHours(checkIntervalHours);
        _startupDelay = TimeSpan.FromSeconds(startupDelaySeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AppointmentStatusUpdateService is starting. Check interval: {Interval}, Startup delay: {StartupDelay}",
            _checkInterval, _startupDelay);

        // Wait for application startup to complete
        await Task.Delay(_startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while processing overdue appointments");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("AppointmentStatusUpdateService is stopping");
    }

    private async Task ProcessOverdueAppointmentsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process overdue appointments...");

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var grpcClients = scope.ServiceProvider.GetRequiredService<GrpcClientWrapper>();

        var today = DateTime.UtcNow.Date;

        // Process overdue PENDING appointments (includes specialty appointments without doctor)
        await ProcessPendingAppointmentsAsync(repository, eventBus, grpcClients, today, cancellationToken);

        // Process overdue CONFIRMED appointments
        await ProcessConfirmedAppointmentsAsync(repository, today, cancellationToken);

        _logger.LogInformation("Completed processing overdue appointments");
    }

    private async Task ProcessPendingAppointmentsAsync(
        IAppointmentRepository repository,
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        var overduePendingAppointments = await repository
            .GetOverdueAppointmentsByStatusAsync(AppointmentStatus.PENDING, referenceDate);

        if (!overduePendingAppointments.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Found {Count} overdue PENDING appointments to cancel",
            overduePendingAppointments.Count);

        await CancelOverdueAppointmentsAsync(
            repository,
            eventBus,
            grpcClients,
            overduePendingAppointments,
            cancellationToken);
    }

    private async Task ProcessConfirmedAppointmentsAsync(
        IAppointmentRepository repository,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        var overdueConfirmedAppointments = await repository
            .GetOverdueAppointmentsByStatusAsync(AppointmentStatus.CONFIRMED, referenceDate);

        if (!overdueConfirmedAppointments.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Found {Count} overdue CONFIRMED appointments to complete",
            overdueConfirmedAppointments.Count);

        await CompleteOverdueAppointmentsAsync(
            repository,
            overdueConfirmedAppointments,
            cancellationToken);
    }

    private async Task CancelOverdueAppointmentsAsync(
        IAppointmentRepository repository,
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        List<AppointmentEntity> appointments,
        CancellationToken cancellationToken)
    {
        foreach (var appointment in appointments)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Check if this is a specialty appointment (hospital assigns doctor) without doctor assigned
            var isSpecialtyAppointmentWithoutDoctor = appointment.SpecialtyId.HasValue
                && !appointment.DoctorId.HasValue
                && !appointment.ServiceId.HasValue;

            var cancellationReason = isSpecialtyAppointmentWithoutDoctor
                ? NO_DOCTOR_ASSIGNED_REASON
                : OVERDUE_CANCELLATION_REASON;

            var success = await repository.CancelAppointmentAsync(
                appointment,
                cancellationReason,
                SYSTEM_USER);

            LogAppointmentUpdateResult(success, appointment.Id, appointment.PatientId, "cancelled");

            // If cancellation was successful and it's a specialty appointment without doctor,
            // publish notification event to inform the patient
            if (success && isSpecialtyAppointmentWithoutDoctor)
            {
                await PublishNoDoctorAssignedNotificationAsync(
                    eventBus,
                    grpcClients,
                    appointment,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Publish notification event when appointment is auto-cancelled due to hospital not assigning doctor
    /// </summary>
    private async Task PublishNoDoctorAssignedNotificationAsync(
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        AppointmentEntity appointment,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get patient info via gRPC
            var patientRequest = new GetUserBasicInfoRequest { Id = appointment.PatientId.ToString() };
            var patientResponse = await grpcClients.UserClient.GetUserBasicInfoAsync(patientRequest);

            var patientFullName = $"{patientResponse.FirstName} {patientResponse.LastName}".Trim();
            if (string.IsNullOrEmpty(patientFullName))
            {
                patientFullName = "Quý khách";
            }

            // Get hospital info via gRPC
            var hospitalName = "Bệnh viện";
            if (appointment.HospitalId.HasValue)
            {
                var hospitalRequest = new GetHospitalBasicInfoRequest { Id = appointment.HospitalId.Value.ToString() };
                var hospitalResponse = await grpcClients.HospitalClient.GetHospitalBasicInfoAsync(hospitalRequest);
                if (!string.IsNullOrEmpty(hospitalResponse?.Name))
                {
                    hospitalName = hospitalResponse.Name;
                }
            }

            // Get specialty info via gRPC
            var specialtyName = "Chuyên khoa";
            if (appointment.SpecialtyId.HasValue)
            {
                var specialtyRequest = new GetSpecialtiesByIdsRequest();
                specialtyRequest.Ids.Add(appointment.SpecialtyId.Value.ToString());
                var specialtyResponse = await grpcClients.DoctorClient.GetSpecialtiesByIdsAsync(specialtyRequest);
                if (specialtyResponse?.Specialties?.Count > 0)
                {
                    specialtyName = specialtyResponse.Specialties[0].Name;
                }
            }

            // Get appointment time display
            var appointmentTime = AppointmentTimeHelper.GetAppointmentTimeDisplay(appointment.AppointmentTimeId);

            // Publish notification event
            var notificationEvent = new AppointmentAutoCancelledDueToNoDoctorEvent
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                PatientAccountId = appointment.PatientAccountId,
                PatientEmail = patientResponse.Email ?? string.Empty,
                PatientPhone = patientResponse.Phone,
                PatientFullName = patientFullName,
                HospitalName = hospitalName,
                SpecialtyName = specialtyName,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointmentTime,
                CancelledAt = DateTime.UtcNow,
                CancellationReason = NO_DOCTOR_ASSIGNED_REASON
            };

            await eventBus.PublishAsync(notificationEvent, null, cancellationToken);

            _logger.LogInformation(
                "Published AppointmentAutoCancelledDueToNoDoctorEvent - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                appointment.Id, patientResponse.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish notification for auto-cancelled appointment - AppointmentId: {AppointmentId}",
                appointment.Id);
            // Don't rethrow - notification failure shouldn't break the cancellation process
        }
    }

    private async Task CompleteOverdueAppointmentsAsync(
        IAppointmentRepository repository,
        List<AppointmentEntity> appointments,
        CancellationToken cancellationToken)
    {
        foreach (var appointment in appointments)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var success = await repository.UpdateAppointmentStatusAsync(
                appointment.Id,
                AppointmentStatus.COMPLETED,
                "Tự động hoàn thành - Đã qua ngày hẹn");

            LogAppointmentUpdateResult(success, appointment.Id, appointment.PatientId, "completed");
        }
    }

    private void LogAppointmentUpdateResult(bool success, Guid appointmentId, Guid patientId, string action)
    {
        if (success)
        {
            _logger.LogInformation(
                "Successfully {Action} overdue appointment {AppointmentId} for patient {PatientId}",
                action,
                appointmentId,
                patientId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to {Action} overdue appointment {AppointmentId}",
                action,
                appointmentId);
        }
    }
}

