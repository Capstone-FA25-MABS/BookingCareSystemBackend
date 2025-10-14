using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Payment.Helpers;

/// <summary>
/// Helper class for payment-related event operations
/// </summary>
public static class PaymentEventHelper
{
    /// <summary>
    /// Publish AppointmentDeleteRequestedIntegrationEvent when payment fails
    /// </summary>
    /// <param name="payment">Payment that failed</param>
    /// <param name="responseCode">Payment gateway response code</param>
    /// <param name="paymentMethod">Payment method (VNPay, PayOS, etc.)</param>
    /// <param name="requestId">Request ID for tracking</param>
    /// <param name="appointmentClient">gRPC client for appointment service</param>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="responseMessageFunc">Function to convert response code to human readable message</param>
    public static async Task PublishAppointmentDeleteEventAsync(
        PaymentResponse payment,
        string responseCode,
        string paymentMethod,
        string requestId,
        AppointmentService.AppointmentServiceClient appointmentClient,
        IEventBus eventBus,
        ILogger logger,
        Func<string, string> responseMessageFunc)
    {
        try
        {
            // Only publish deletion event for appointment payments
            if (!payment.AppointmentId.HasValue)
            {
                logger.LogInformation("{PaymentMethod} Callback #{RequestId} - Payment {PaymentId} is not for an appointment, skipping deletion event",
                    paymentMethod, requestId, payment.Id);
                return;
            }

            // Get appointment details via gRPC to populate the event
            var appointmentRequest = new GetDoctorIdByAppointmentIdRequest
            {
                AppointmentId = payment.AppointmentId.Value.ToString()
            };

            var appointmentResponse = await appointmentClient.GetDoctorIdByAppointmentIdAsync(appointmentRequest);
            var doctorId = appointmentResponse.Success && !string.IsNullOrEmpty(appointmentResponse.DoctorId)
                ? Guid.Parse(appointmentResponse.DoctorId)
                : (Guid?)null;

            var correlationId = Guid.NewGuid().ToString("N")[..8];
            var failureMessage = responseMessageFunc(responseCode);

            var deleteEvent = new AppointmentDeleteRequestedIntegrationEvent
            {
                AppointmentId = payment.AppointmentId.Value,
                PatientId = payment.PatientId ?? Guid.Empty,
                DoctorId = doctorId,
                HospitalId = null, // Will be populated by Appointment Service if needed
                AppointmentDate = DateTime.UtcNow, // Placeholder - will be populated by Appointment Service
                AppointmentType = 0, // Placeholder - will be populated by Appointment Service
                DeletionReason = $"Payment failed via {paymentMethod}: {failureMessage}",
                PaymentId = payment.Id,
                PaymentMethod = paymentMethod,
                PaymentFailureReason = $"{paymentMethod} Response Code: {responseCode} - {failureMessage}",
                RequestedAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            await eventBus.PublishAsync(deleteEvent);

            logger.LogInformation("{PaymentMethod} Callback #{RequestId} - Published AppointmentDeleteRequestedIntegrationEvent - AppointmentId: {AppointmentId}, PaymentId: {PaymentId}, CorrelationId: {CorrelationId}",
                paymentMethod, requestId, payment.AppointmentId.Value, payment.Id, correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{PaymentMethod} Callback #{RequestId} - Failed to publish AppointmentDeleteRequestedIntegrationEvent for PaymentId: {PaymentId}",
                paymentMethod, requestId, payment.Id);
            // Don't throw - payment processing should continue even if event publishing fails
        }
    }
}