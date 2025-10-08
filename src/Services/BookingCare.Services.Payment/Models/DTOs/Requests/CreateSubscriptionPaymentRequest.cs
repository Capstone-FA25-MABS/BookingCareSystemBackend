using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request DTO to create a subscription payment (hospital subscribes to a plan)
/// </summary>
public class CreateSubscriptionPaymentRequest
{
    /// <summary>
    /// Subscription ID (required)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Hospital ID (required)
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    public Guid PaymentMethodId { get; set; }
}