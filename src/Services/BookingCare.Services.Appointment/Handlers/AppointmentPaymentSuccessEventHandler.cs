using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Appointment.Services;

namespace BookingCare.Services.Appointment.Handlers;

/// <summary>
/// Event handler for processing successful payment events
/// Triggers email notification to patient about successful booking
/// </summary>
public class AppointmentPaymentSuccessEventHandler : IIntegrationEventHandler<AppointmentPaymentSuccessIntegrationEvent>
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentPaymentSuccessEventHandler> _logger;

    public AppointmentPaymentSuccessEventHandler(
        IAppointmentService appointmentService,
        ILogger<AppointmentPaymentSuccessEventHandler> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentPaymentSuccessIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AppointmentService] Received AppointmentPaymentSuccessIntegrationEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PaymentId: {PaymentId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}, CorrelationId: {CorrelationId}",
            @event.AppointmentId, @event.PatientId, @event.PaymentId, @event.Amount, @event.PaymentMethod, @event.CorrelationId);

        try
        {
            // Send booking success email notification to patient with payment amount
            var emailSent = await _appointmentService.SendAppointmentBookingSuccessEmailAsync(@event.AppointmentId, @event.PatientId, @event.AccountId, @event.Amount);

            if (emailSent)
            {
                _logger.LogInformation(
                    "[AppointmentService] Successfully processed payment success notification - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PaymentId: {PaymentId}, Amount: {Amount}",
                    @event.AppointmentId, @event.PatientId, @event.PaymentId, @event.Amount);
            }
            else
            {
                _logger.LogWarning(
                    "[AppointmentService] Failed to send booking success email - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PaymentId: {PaymentId}",
                    @event.AppointmentId, @event.PatientId, @event.PaymentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppointmentService] Error processing payment success notification - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PaymentId: {PaymentId}",
                @event.AppointmentId, @event.PatientId, @event.PaymentId);
        }
    }
}