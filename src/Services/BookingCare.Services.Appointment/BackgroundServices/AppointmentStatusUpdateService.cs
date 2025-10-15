using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Repositories;

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

        try
        {
            var today = DateTime.UtcNow.Date;

            // Get and process overdue PENDING appointments
            var overduePendingAppointments = await repository
                .GetOverdueAppointmentsByStatusAsync(AppointmentStatus.PENDING, today);

            if (overduePendingAppointments.Any())
            {
                _logger.LogInformation(
                    "Found {Count} overdue PENDING appointments to cancel",
                    overduePendingAppointments.Count);

                foreach (var appointment in overduePendingAppointments)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var success = await repository.CancelAppointmentAsync(
                        appointment,
                        OVERDUE_CANCELLATION_REASON,
                        SYSTEM_USER);

                    if (success)
                    {
                        _logger.LogInformation(
                            "Successfully cancelled overdue appointment {AppointmentId} for patient {PatientId}",
                            appointment.Id,
                            appointment.PatientId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to cancel overdue appointment {AppointmentId}",
                            appointment.Id);
                    }
                }
            }

            // Get and process overdue CONFIRMED appointments
            var overdueConfirmedAppointments = await repository
                .GetOverdueAppointmentsByStatusAsync(AppointmentStatus.CONFIRMED, today);

            if (overdueConfirmedAppointments.Any())
            {
                _logger.LogInformation(
                    "Found {Count} overdue CONFIRMED appointments to complete",
                    overdueConfirmedAppointments.Count);

                foreach (var appointment in overdueConfirmedAppointments)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var success = await repository.UpdateAppointmentStatusAsync(
                        appointment.Id,
                        AppointmentStatus.COMPLETED,
                        "Tự động hoàn thành - Đã qua ngày hẹn");

                    if (success)
                    {
                        _logger.LogInformation(
                            "Successfully completed overdue appointment {AppointmentId} for patient {PatientId}",
                            appointment.Id,
                            appointment.PatientId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to complete overdue appointment {AppointmentId}",
                            appointment.Id);
                    }
                }
            }

            if (!overduePendingAppointments.Any() && !overdueConfirmedAppointments.Any())
            {
                _logger.LogInformation("No overdue appointments found to process");
            }

            _logger.LogInformation("Completed processing overdue appointments");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while processing overdue appointments in repository operations");
            throw;
        }
    }
}

