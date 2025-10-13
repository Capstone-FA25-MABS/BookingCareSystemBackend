using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Appointment.Services;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Handlers;

/// <summary>
/// Event handler for processing payment completion events
/// Automatically updates appointment status to CONFIRMED when payment is completed
/// </summary>
public class PaymentCompletedEventHandler : IIntegrationEventHandler<PaymentCompletedIntegrationEvent>
{
    private readonly IAppointmentService _appointmentService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        IAppointmentService appointmentService,
        IEventBus eventBus,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _appointmentService = appointmentService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentCompletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AppointmentService] Received PaymentCompletedIntegrationEvent - PaymentId: {PaymentId}, AppointmentId: {AppointmentId}, CorrelationId: {CorrelationId}",
            @event.PaymentId, @event.AppointmentId, @event.CorrelationId);

        try
        {
            // Only process if this payment is for an appointment
            if (!@event.AppointmentId.HasValue)
            {
                _logger.LogInformation(
                    "[AppointmentService] Payment {PaymentId} is not for an appointment (TransactionType: {TransactionType}), skipping appointment update",
                    @event.PaymentId, @event.TransactionType);
                return;
            }

            // Get the appointment to check current status
            var appointment = await _appointmentService.GetAppointmentByIdForPatientAsync(@event.AppointmentId.Value);
            if (appointment == null)
            {
                _logger.LogWarning(
                    "[AppointmentService] Appointment not found for PaymentId: {PaymentId}, AppointmentId: {AppointmentId}",
                    @event.PaymentId, @event.AppointmentId);
                
                await PublishFailureEventAsync(@event, "Appointment not found");
                return;
            }

            // Check if appointment is in PENDING status (eligible for confirmation)
            if (appointment.Status != AppointmentStatus.PENDING)
            {
                _logger.LogWarning(
                    "[AppointmentService] Appointment {AppointmentId} is not in PENDING status (Current: {Status}), cannot confirm",
                    @event.AppointmentId, appointment.Status);
                
                await PublishFailureEventAsync(@event, $"Appointment status is {appointment.Status}, expected PENDING");
                return;
            }

            // Update appointment status to CONFIRMED
            var updateRequest = new UpdateAppointmentStatusRequest
            {
                Id = @event.AppointmentId.Value,
                Status = AppointmentStatus.CONFIRMED,
                Result = $"Appointment confirmed automatically after successful payment. PaymentId: {@event.PaymentId}"
            };

            var success = await _appointmentService.UpdateAppointmentStatusAsync(updateRequest);
            
            if (success)
            {
                _logger.LogInformation(
                    "[AppointmentService] Successfully confirmed appointment {AppointmentId} after payment {PaymentId}",
                    @event.AppointmentId, @event.PaymentId);

                // Publish success event
                await PublishSuccessEventAsync(@event, appointment);
            }
            else
            {
                _logger.LogError(
                    "[AppointmentService] Failed to update appointment {AppointmentId} status after payment {PaymentId}",
                    @event.AppointmentId, @event.PaymentId);

                await PublishFailureEventAsync(@event, "Failed to update appointment status");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppointmentService] Error processing payment completion - PaymentId: {PaymentId}, AppointmentId: {AppointmentId}",
                @event.PaymentId, @event.AppointmentId);

            await PublishFailureEventAsync(@event, ex.Message);
        }
    }

    /// <summary>
    /// Publish success event when appointment is confirmed
    /// </summary>
    private async Task PublishSuccessEventAsync(PaymentCompletedIntegrationEvent paymentEvent, AppointmentResponse appointment)
    {
        var statusUpdateEvent = new AppointmentStatusUpdatedIntegrationEvent
        {
            AppointmentId = appointment.Id,
            PatientId = paymentEvent.PatientId,
            PreviousStatus = AppointmentStatus.PENDING.ToString(),
            NewStatus = AppointmentStatus.CONFIRMED.ToString(),
            UpdatedAt = DateTime.UtcNow,
            PaymentId = paymentEvent.PaymentId,
            CorrelationId = paymentEvent.CorrelationId,
            Notes = $"Appointment confirmed automatically after successful payment via {paymentEvent.PaymentMethod}"
        };

        await _eventBus.PublishAsync(statusUpdateEvent);
        
        _logger.LogInformation(
            "[AppointmentService] Published AppointmentStatusUpdatedIntegrationEvent - AppointmentId: {AppointmentId}, CorrelationId: {CorrelationId}",
            appointment.Id, paymentEvent.CorrelationId);
    }

    /// <summary>
    /// Publish failure event when appointment confirmation fails
    /// </summary>
    private async Task PublishFailureEventAsync(PaymentCompletedIntegrationEvent paymentEvent, string errorMessage)
    {
        // For now, we just log the failure. In the future, you might want to create a specific failure event
        _logger.LogError(
            "[AppointmentService] Failed to process payment completion - PaymentId: {PaymentId}, AppointmentId: {AppointmentId}, Error: {Error}",
            paymentEvent.PaymentId, paymentEvent.AppointmentId, errorMessage);
        
        // Optional: Publish a failure event if needed by other services
        // var failureEvent = new PaymentCompletedProcessingFailedEvent { ... };
        // await _eventBus.PublishAsync(failureEvent);
    }
}