using BookingCare.Services.Appointment.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Appointment.BackgroundServices;

/// <summary>
/// Background service to periodically clean up expired reschedule tokens
/// Runs every hour to remove tokens that have expired but appointment wasn't completed
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public TokenCleanupService(
        ILogger<TokenCleanupService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Token Cleanup Service is performing cleanup at: {Time}", DateTime.UtcNow);

                await CleanupExpiredTokensAsync();

                _logger.LogInformation("Token Cleanup Service completed cleanup. Next run in {Hours} hour(s)",
                    _cleanupInterval.TotalHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired tokens");
            }

            // Wait for the specified interval before running again
            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Token Cleanup Service is stopping");
    }

    private async Task CleanupExpiredTokensAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var appointmentRepository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();

        var now = DateTime.UtcNow;

        // Get appointments with expired tokens
        var appointments = await appointmentRepository.GetAppointmentsWithExpiredTokensAsync(now);

        if (appointments.Count == 0)
        {
            _logger.LogInformation("No expired tokens found");
            return;
        }

        _logger.LogInformation("Found {Count} appointments with expired tokens", appointments.Count);

        var clearedCount = 0;
        foreach (var appointment in appointments)
        {
            try
            {
                // Clear token and pending reschedule action
                appointment.RescheduleToken = null;
                appointment.RescheduleTokenExpiry = null;
                appointment.PendingRescheduleAction = null;
                appointment.PendingNewDoctorId = null;
                appointment.PendingNewAppointmentDate = null;
                appointment.PendingNewAppointmentTimeId = null;
                appointment.AssignedDoctorId = null; // Clear soft reservation
                appointment.SoftReservedUntil = null;

                await appointmentRepository.UpdateAppointmentAsync(appointment);
                clearedCount++;

                _logger.LogDebug("Cleared expired token for appointment {AppointmentId}", appointment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear token for appointment {AppointmentId}", appointment.Id);
            }
        }

        _logger.LogInformation("Successfully cleared {Count} expired tokens", clearedCount);
    }
}

