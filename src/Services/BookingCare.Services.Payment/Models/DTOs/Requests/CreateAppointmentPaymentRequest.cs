using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a payment for an appointment (created by patient)
/// </summary>
public class CreateAppointmentPaymentRequest
{
    /// <summary>
    /// Appointment ID (required)
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Patient ID (required)
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}