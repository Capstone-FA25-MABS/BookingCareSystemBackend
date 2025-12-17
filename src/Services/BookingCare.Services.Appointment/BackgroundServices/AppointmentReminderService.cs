using BookingCare.Services.Appointment.Helpers;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using AppointmentTime = BookingCare.Shared.Common.Enums.AppointmentTime;

namespace BookingCare.Services.Appointment.BackgroundServices;

/// <summary>
/// Background service that sends appointment reminders to patients
/// - 24 hours before appointment
/// - 1 hour before appointment
/// Publishes AppointmentReminderEvent to Notification Service
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _startupDelay;

    public AppointmentReminderService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentReminderService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Check every 15 minutes for upcoming appointments
        var checkIntervalMinutes = configuration
            .GetValue<int>("BackgroundServices:AppointmentReminder:CheckIntervalMinutes", 15);
        var startupDelaySeconds = configuration
            .GetValue<int>("BackgroundServices:AppointmentReminder:StartupDelaySeconds", 60);

        _checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);
        _startupDelay = TimeSpan.FromSeconds(startupDelaySeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AppointmentReminderService starting. Check interval: {Interval}, Startup delay: {Delay}",
            _checkInterval, _startupDelay);

        await Task.Delay(_startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUpcomingAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing appointment reminders");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("AppointmentReminderService stopping");
    }

    private async Task ProcessUpcomingAppointmentsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for upcoming appointments to send reminders...");

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var grpcClients = scope.ServiceProvider.GetRequiredService<GrpcClientWrapper>();

        var now = DateTime.UtcNow;

        // Process 24-hour reminders
        await ProcessRemindersAsync(
            repository, eventBus, grpcClients,
            now, 24, "24_HOURS", cancellationToken);

        // Process 1-hour reminders
        await ProcessRemindersAsync(
            repository, eventBus, grpcClients,
            now, 1, "1_HOUR", cancellationToken);
    }

    private async Task ProcessRemindersAsync(
        IAppointmentRepository repository,
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        DateTime now,
        int hoursBeforeAppointment,
        string reminderType,
        CancellationToken cancellationToken)
    {
        // Get Vietnam timezone for accurate time calculations
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVietnam = TimeZoneInfo.ConvertTimeFromUtc(now, vietnamTimeZone);

        // Calculate target time in Vietnam timezone
        var targetTimeVietnam = nowVietnam.AddHours(hoursBeforeAppointment);

        // Create window around target time (±10 minutes)
        var windowStartVietnam = targetTimeVietnam.AddMinutes(-10);
        var windowEndVietnam = targetTimeVietnam.AddMinutes(10);

        // Convert back to UTC for database query
        var windowStart = TimeZoneInfo.ConvertTimeToUtc(windowStartVietnam, vietnamTimeZone);
        var windowEnd = TimeZoneInfo.ConvertTimeToUtc(windowEndVietnam, vietnamTimeZone);

        var appointments = await repository.GetAppointmentsForReminderAsync(
            windowStart, windowEnd, hoursBeforeAppointment);

        if (!appointments.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Found {Count} appointments for {ReminderType} reminder. Target time (Vietnam): {TargetTime}",
            appointments.Count, reminderType, targetTimeVietnam);

        foreach (var appointment in appointments)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                // Verify this appointment actually needs reminder at this time
                if (ShouldSendReminder(appointment, nowVietnam, hoursBeforeAppointment, vietnamTimeZone))
                {
                    await SendReminderAsync(
                        eventBus, grpcClients, appointment,
                        reminderType, hoursBeforeAppointment, cancellationToken);

                    // Mark reminder as sent to avoid duplicate reminders
                    await repository.MarkReminderSentAsync(
                        appointment.Id, hoursBeforeAppointment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send {ReminderType} reminder for appointment {AppointmentId}",
                    reminderType, appointment.Id);
            }
        }
    }

    /// <summary>
    /// Verify if reminder should be sent by calculating exact appointment time
    /// Uses RefundPolicyHelper logic to combine date + time slot
    /// </summary>
    private static bool ShouldSendReminder(
        AppointmentEntity appointment,
        DateTime nowVietnam,
        int hoursBeforeAppointment,
        TimeZoneInfo vietnamTimeZone)
    {
        try
        {
            // Parse appointment time slot to get start time
            var startTime = ParseAppointmentStartTime(appointment.AppointmentTimeId);
            if (!startTime.HasValue)
            {
                return false;
            }

            // Convert appointment date from UTC to Vietnam timezone
            var appointmentDateVietnam = TimeZoneInfo.ConvertTimeFromUtc(
                appointment.AppointmentDate, vietnamTimeZone);

            // Combine date with time slot (result is in Vietnam timezone)
            var fullAppointmentTimeVietnam = appointmentDateVietnam.Date
                .Add(startTime.Value.ToTimeSpan());

            // Calculate hours until appointment
            var hoursUntilAppointment = (fullAppointmentTimeVietnam - nowVietnam).TotalHours;

            // Check if we're in the reminder window (±30 minutes)
            return Math.Abs(hoursUntilAppointment - hoursBeforeAppointment) <= 0.5;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parse AppointmentTimeId enum to get start time (same logic as RefundPolicyHelper)
    /// </summary>
    private static TimeOnly? ParseAppointmentStartTime(AppointmentTime appointmentTimeId)
    {
        try
        {
            // Format: AT_08_00_08_30 (8:00-8:30)
            var timeString = appointmentTimeId.ToString();
            var parts = timeString.Split('_');

            if (parts.Length >= 3 && parts[0] == "AT" &&
                int.TryParse(parts[1], out int hour) &&
                int.TryParse(parts[2], out int minute))
            {
                return new TimeOnly(hour, minute);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task SendReminderAsync(
        IEventBus eventBus,
        GrpcClientWrapper grpcClients,
        AppointmentEntity appointment,
        string reminderType,
        int hoursBeforeAppointment,
        CancellationToken cancellationToken)
    {
        // Get patient info
        var patientRequest = new GetUserBasicInfoRequest
        {
            Id = appointment.PatientId.ToString()
        };
        var patientResponse = await grpcClients.UserClient.GetUserBasicInfoAsync(patientRequest);

        var patientFullName = $"{patientResponse.FirstName} {patientResponse.LastName}".Trim();
        if (string.IsNullOrEmpty(patientFullName))
        {
            patientFullName = "Quý khách";
        }

        // Get hospital info
        var hospitalName = "Bệnh viện";
        string? hospitalAddress = null;
        if (appointment.HospitalId.HasValue)
        {
            var hospitalRequest = new GetHospitalBasicInfoRequest
            {
                Id = appointment.HospitalId.Value.ToString()
            };
            var hospitalResponse = await grpcClients.HospitalClient
                .GetHospitalBasicInfoAsync(hospitalRequest);

            if (!string.IsNullOrEmpty(hospitalResponse?.Name))
            {
                hospitalName = hospitalResponse.Name;
                hospitalAddress = hospitalResponse.Address;
            }
        }

        // Get doctor info
        string? doctorName = null;
        if (appointment.DoctorId.HasValue)
        {
            var doctorRequest = new GetDoctorBasicInfoRequest
            {
                Id = appointment.DoctorId.Value.ToString()
            };
            var doctorResponse = await grpcClients.DoctorClient
                .GetDoctorBasicInfoAsync(doctorRequest);

            if (doctorResponse != null)
            {
                doctorName = $"{doctorResponse.FirstName} {doctorResponse.LastName}".Trim();
            }
        }

        // Get specialty info
        string? specialtyName = null;
        if (appointment.SpecialtyId.HasValue)
        {
            var specialtyRequest = new GetSpecialtiesByIdsRequest();
            specialtyRequest.Ids.Add(appointment.SpecialtyId.Value.ToString());
            var specialtyResponse = await grpcClients.DoctorClient
                .GetSpecialtiesByIdsAsync(specialtyRequest);

            if (specialtyResponse?.Specialties?.Count > 0)
            {
                specialtyName = specialtyResponse.Specialties[0].Name;
            }
        }

        // Get service info
        string? serviceName = null;
        if (appointment.ServiceId.HasValue)
        {
            var serviceRequest = new ServiceMedical.Protos
                .GetServiceMedicalBasicInfoRequest
            {
                Id = appointment.ServiceId.Value.ToString()
            };
            var serviceResponse = await grpcClients.ServiceMedicalClient
                .GetServiceMedicalBasicInfoAsync(serviceRequest);

            if (!string.IsNullOrEmpty(serviceResponse?.Name))
            {
                serviceName = serviceResponse.Name;
            }
        }

        var reminderEvent = new AppointmentReminderEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            PatientAccountId = appointment.PatientAccountId,
            PatientEmail = patientResponse.Email ?? string.Empty,
            PatientPhone = patientResponse.Phone,
            PatientFullName = patientFullName,
            DoctorName = doctorName,
            HospitalName = hospitalName,
            HospitalAddress = hospitalAddress,
            SpecialtyName = specialtyName,
            ServiceName = serviceName,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentTime = AppointmentTimeHelper.GetAppointmentTimeDisplay(appointment.AppointmentTimeId),
            ReminderType = reminderType,
            HoursBeforeAppointment = hoursBeforeAppointment,
            ReminderSentAt = DateTime.UtcNow
        };

        await eventBus.PublishAsync(reminderEvent, null, cancellationToken);

        _logger.LogInformation(
            "Published {ReminderType} reminder for appointment {AppointmentId}, Patient: {PatientEmail}",
            reminderType, appointment.Id, patientResponse.Email);
    }
}
