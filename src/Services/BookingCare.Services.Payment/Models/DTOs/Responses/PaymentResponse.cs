using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for payment
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// ID of the payment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Appointment ID
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// Clinic ID
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Subscription ID
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

    /// <summary>
    /// Payment method name
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Discount ID (if discount was applied)
    /// </summary>
    public Guid? DiscountId { get; set; }

    /// <summary>
    /// Discount code (if discount was applied)
    /// Stored for audit trail and display purposes
    /// </summary>
    public string? DiscountCode { get; set; }

    /// <summary>
    /// Payment status
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Payment Intent ID from payment gateway (for refunds and tracking)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Appointment date (populated via gRPC call to Appointment Service)
    /// Only available when AppointmentId is not null
    /// </summary>
    public DateTime? AppointmentDate { get; set; }

    /// <summary>
    /// Appointment type (populated via gRPC call to Appointment Service)
    /// Only available when AppointmentId is not null
    /// </summary>
    public string? AppointmentType { get; set; }
}
