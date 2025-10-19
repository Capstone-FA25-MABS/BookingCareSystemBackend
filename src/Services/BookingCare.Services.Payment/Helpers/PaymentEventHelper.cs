using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Payment.Helpers;

/// <summary>
/// Parameters for publishing appointment delete event
/// </summary>
public class AppointmentDeleteEventParams
{
    /// <summary>
    /// Payment that failed
    /// </summary>
    public required PaymentResponse Payment { get; set; }

    /// <summary>
    /// Payment gateway response code
    /// </summary>
    public required string ResponseCode { get; set; }

    /// <summary>
    /// Payment method (VNPay, PayOS, etc.)
    /// </summary>
    public required string PaymentMethod { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Function to convert response code to human readable message
    /// </summary>
    public required Func<string, string> ResponseMessageFunc { get; set; }
}

/// <summary>
/// Dependencies for event publishing
/// </summary>
public class PaymentEventDependencies
{
    /// <summary>
    /// gRPC client for appointment service
    /// </summary>
    public required AppointmentService.AppointmentServiceClient AppointmentClient { get; set; }

    /// <summary>
    /// Event bus for publishing events
    /// </summary>
    public required IEventBus EventBus { get; set; }

    /// <summary>
    /// Logger instance
    /// </summary>
    public required ILogger Logger { get; set; }
}

/// <summary>
/// Helper class for payment-related event operations
/// </summary>
public static class PaymentEventHelper
{
    /// <summary>
    /// Publish AppointmentDeleteRequestedIntegrationEvent when payment fails
    /// </summary>
    /// <param name="eventParams">Event parameters</param>
    /// <param name="dependencies">Event dependencies</param>
    public static async Task PublishAppointmentDeleteEventAsync(
        AppointmentDeleteEventParams eventParams,
        PaymentEventDependencies dependencies)
    {
        try
        {
            // Only publish deletion event for appointment payments
            if (!eventParams.Payment.AppointmentId.HasValue)
            {
                dependencies.Logger.LogInformation("{PaymentMethod} Callback #{RequestId} - Payment {PaymentId} is not for an appointment, skipping deletion event",
                    eventParams.PaymentMethod, eventParams.RequestId, eventParams.Payment.Id);
                return;
            }

            // Get appointment details via gRPC to populate the event
            var appointmentRequest = new GetDoctorIdByAppointmentIdRequest
            {
                AppointmentId = eventParams.Payment.AppointmentId.Value.ToString()
            };

            var appointmentResponse = await dependencies.AppointmentClient.GetDoctorIdByAppointmentIdAsync(appointmentRequest);
            var doctorId = appointmentResponse.Success && !string.IsNullOrEmpty(appointmentResponse.DoctorId)
                ? Guid.Parse(appointmentResponse.DoctorId)
                : (Guid?)null;

            var correlationId = Guid.NewGuid().ToString("N")[..8];
            var failureMessage = eventParams.ResponseMessageFunc(eventParams.ResponseCode);

            var deleteEvent = new AppointmentDeleteRequestedIntegrationEvent
            {
                AppointmentId = eventParams.Payment.AppointmentId.Value,
                PatientId = eventParams.Payment.PatientId ?? Guid.Empty,
                DoctorId = doctorId,
                HospitalId = null, // Will be populated by Appointment Service if needed
                AppointmentDate = DateTime.UtcNow, // Placeholder - will be populated by Appointment Service
                AppointmentType = 0, // Placeholder - will be populated by Appointment Service
                DeletionReason = $"Payment failed via {eventParams.PaymentMethod}: {failureMessage}",
                PaymentId = eventParams.Payment.Id,
                PaymentMethod = eventParams.PaymentMethod,
                PaymentFailureReason = $"{eventParams.PaymentMethod} Response Code: {eventParams.ResponseCode} - {failureMessage}",
                RequestedAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            await dependencies.EventBus.PublishAsync(deleteEvent);

            dependencies.Logger.LogInformation("{PaymentMethod} Callback #{RequestId} - Published AppointmentDeleteRequestedIntegrationEvent - AppointmentId: {AppointmentId}, PaymentId: {PaymentId}, CorrelationId: {CorrelationId}",
                eventParams.PaymentMethod, eventParams.RequestId, eventParams.Payment.AppointmentId.Value, eventParams.Payment.Id, correlationId);
        }
        catch (Exception ex)
        {
            dependencies.Logger.LogError(ex, "{PaymentMethod} Callback #{RequestId} - Failed to publish AppointmentDeleteRequestedIntegrationEvent for PaymentId: {PaymentId}",
                eventParams.PaymentMethod, eventParams.RequestId, eventParams.Payment.Id);
            // Don't throw - payment processing should continue even if event publishing fails
        }
    }

}