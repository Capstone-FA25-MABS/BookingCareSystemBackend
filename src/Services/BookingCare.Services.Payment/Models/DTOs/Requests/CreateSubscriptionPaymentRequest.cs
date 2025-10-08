using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a subscription payment (clinic subscribes to a plan)
/// </summary>
public class CreateSubscriptionPaymentRequest
{
    /// <summary>
    /// Subscription ID (required)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Clinic ID (required)
    /// </summary>
    public Guid ClinicId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}