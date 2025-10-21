using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Payment.Services.BackgroundServices;

/// <summary>
/// Background service that automatically handles pending payments that exceed the timeout period
/// - Checks for payments that have been in PENDING status for more than 15 minutes
/// - Updates payment status to FAILED
/// - Publishes events to delete associated appointments to free up time slots
/// </summary>
public class PendingPaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PendingPaymentCleanupService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _startupDelay;
    private readonly TimeSpan _paymentTimeout;
    private const string PAYMENT_TIMEOUT_REASON = "Thanh toán bị hủy - Quá thời gian cho phép";
    private const string APPOINTMENT_DELETION_REASON = "Xóa lịch hẹn do thanh toán thất bại - Quá thời gian thanh toán";

    public PendingPaymentCleanupService(
        IServiceProvider serviceProvider,
        ILogger<PendingPaymentCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Read configuration with defaults
        var checkIntervalMinutes = configuration
            .GetValue("BackgroundServices:PendingPaymentCleanup:CheckIntervalMinutes", 5);
        var startupDelaySeconds = configuration
            .GetValue("BackgroundServices:PendingPaymentCleanup:StartupDelaySeconds", 30);
        var paymentTimeoutMinutes = configuration
            .GetValue("BackgroundServices:PendingPaymentCleanup:PaymentTimeoutMinutes", 15);

        _checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);
        _startupDelay = TimeSpan.FromSeconds(startupDelaySeconds);
        _paymentTimeout = TimeSpan.FromMinutes(paymentTimeoutMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PendingPaymentCleanupService is starting. Check interval: {Interval}, Payment timeout: {Timeout}, Startup delay: {StartupDelay}",
            _checkInterval, _paymentTimeout, _startupDelay);

        // Wait for application startup to complete
        await Task.Delay(_startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverduePaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[PendingPaymentCleanup] Error occurred while processing overdue payments");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PendingPaymentCleanupService is stopping");
    }

    private async Task ProcessOverduePaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var cutoffTime = DateTime.UtcNow.Subtract(_paymentTimeout);

        // Get all pending payments that are older than the timeout period
        var overduePendingPayments = await paymentRepository.GetOverduePendingPaymentsAsync(cutoffTime);

        if (!overduePendingPayments.Any())
        {
            _logger.LogDebug("[PendingPaymentCleanup] No overdue pending payments found");
            return;
        }

        // Combined start and found message to reduce LogInformation calls
        _logger.LogInformation(
            "[PendingPaymentCleanup] Starting to process {Count} overdue pending payments (older than {CutoffTime})",
            overduePendingPayments.Count, cutoffTime);

        foreach (var payment in overduePendingPayments)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessOverduePaymentAsync(payment, paymentRepository, eventBus);
        }

        _logger.LogInformation("[PendingPaymentCleanup] Completed processing overdue pending payments");
    }

    private async Task ProcessOverduePaymentAsync(
        Models.Entities.PaymentEntity payment,
        IPaymentRepository paymentRepository,
        IEventBus eventBus)
    {
        try
        {
            _logger.LogInformation(
                "[PendingPaymentCleanup] Processing overdue payment: {PaymentId}, Created: {CreatedAt}, Appointment: {AppointmentId}",
                payment.Id, payment.CreatedAt, payment.AppointmentId);

            // Update payment status to FAILED
            var updateSuccess = await paymentRepository.UpdatePaymentStatusAsync(
                payment.Id,
                PaymentStatus.FAILED,
                PAYMENT_TIMEOUT_REASON);

            if (!updateSuccess)
            {
                _logger.LogError(
                    "[PendingPaymentCleanup] Failed to update payment status to FAILED for PaymentId: {PaymentId}",
                    payment.Id);
                return;
            }

            _logger.LogInformation(
                "[PendingPaymentCleanup] Successfully updated payment {PaymentId} status to FAILED",
                payment.Id);

            // If payment is associated with an appointment, publish event to delete the appointment
            if (payment.AppointmentId.HasValue)
            {
                await PublishAppointmentDeletionEventAsync(payment, eventBus);
            }
            else
            {
                _logger.LogInformation(
                    "[PendingPaymentCleanup] Payment {PaymentId} is not associated with an appointment, skipping appointment deletion",
                    payment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PendingPaymentCleanup] Error processing overdue payment: {PaymentId}",
                payment.Id);
        }
    }

    private async Task PublishAppointmentDeletionEventAsync(
        Models.Entities.PaymentEntity payment,
        IEventBus eventBus)
    {
        try
        {
            var deletionEvent = new AppointmentDeleteRequestedIntegrationEvent
            {
                AppointmentId = payment.AppointmentId!.Value,
                PatientId = payment.PatientId ?? Guid.Empty, // Handle nullable PatientId
                HospitalId = payment.HospitalId,
                DeletionReason = APPOINTMENT_DELETION_REASON,
                PaymentId = payment.Id,
                PaymentMethod = payment.PaymentMethod?.Name ?? "Unknown",
                PaymentFailureReason = PAYMENT_TIMEOUT_REASON,
                RequestedAt = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await eventBus.PublishAsync(deletionEvent);

            _logger.LogInformation(
                "[PendingPaymentCleanup] Published appointment deletion event for PaymentId: {PaymentId}, AppointmentId: {AppointmentId}",
                payment.Id, payment.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PendingPaymentCleanup] Failed to publish appointment deletion event for PaymentId: {PaymentId}, AppointmentId: {AppointmentId}",
                payment.Id, payment.AppointmentId);
        }
    }
}