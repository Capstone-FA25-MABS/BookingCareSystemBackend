using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Handlers;

/// <summary>
/// Event handler for processing appointment deletion requests when payment fails
/// Automatically deletes the appointment completely to free up the time slot
/// </summary>
public class AppointmentDeleteRequestedEventHandler : IIntegrationEventHandler<AppointmentDeleteRequestedIntegrationEvent>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AppointmentDeleteRequestedEventHandler> _logger;

    public AppointmentDeleteRequestedEventHandler(
        IAppointmentRepository appointmentRepository,
        ILogger<AppointmentDeleteRequestedEventHandler> logger)
    {
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentDeleteRequestedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AppointmentService] Received AppointmentDeleteRequestedIntegrationEvent - AppointmentId: {AppointmentId}, PaymentId: {PaymentId}, CorrelationId: {CorrelationId}",
            @event.AppointmentId, @event.PaymentId, @event.CorrelationId);

        try
        {
            // Get the appointment to verify it exists and check current status
            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(@event.AppointmentId);
            if (appointment == null)
            {
                _logger.LogWarning(
                    "[AppointmentService] Appointment not found for deletion - AppointmentId: {AppointmentId}, PaymentId: {PaymentId}",
                    @event.AppointmentId, @event.PaymentId);
                return;
            }

            // Log appointment details before deletion
            _logger.LogInformation(
                "[AppointmentService] Found appointment for deletion - AppointmentId: {AppointmentId}, PatientId: {PatientId}, DoctorId: {DoctorId}, Status: {Status}, Date: {Date}",
                appointment.Id, appointment.PatientId, appointment.DoctorId, appointment.Status, appointment.AppointmentDate);

            // Check if appointment is in a deletable state (PENDING or CONFIRMED)
            // Don't delete if already CANCELLED or COMPLETED
            if (appointment.Status == AppointmentStatus.CANCELLED)
            {
                _logger.LogInformation(
                    "[AppointmentService] Appointment {AppointmentId} is already cancelled, skipping deletion",
                    @event.AppointmentId);
                return;
            }

            if (appointment.Status == AppointmentStatus.COMPLETED)
            {
                _logger.LogWarning(
                    "[AppointmentService] Appointment {AppointmentId} is already completed, cannot delete",
                    @event.AppointmentId);
                return;
            }

            // Delete the appointment completely to free up the time slot
            var deleteSuccess = await _appointmentRepository.DeleteAppointmentAsync(appointment);

            if (deleteSuccess)
            {
                _logger.LogInformation(
                    "[AppointmentService] Successfully deleted appointment {AppointmentId} due to payment failure - PaymentId: {PaymentId}, Method: {PaymentMethod}, Reason: {Reason}",
                    @event.AppointmentId, @event.PaymentId, @event.PaymentMethod, @event.DeletionReason);
            }
            else
            {
                _logger.LogError(
                    "[AppointmentService] Failed to delete appointment {AppointmentId} after payment failure - PaymentId: {PaymentId}",
                    @event.AppointmentId, @event.PaymentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AppointmentService] Error processing appointment deletion request - AppointmentId: {AppointmentId}, PaymentId: {PaymentId}",
                @event.AppointmentId, @event.PaymentId);
        }
    }
}