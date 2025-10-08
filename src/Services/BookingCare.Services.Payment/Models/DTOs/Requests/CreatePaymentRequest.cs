using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a new payment
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Appointment ID (optional)
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// Clinic ID (optional)
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// Patient ID (optional)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Subscription ID (optional - used for clinics subscribing to a plan)
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction type
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}