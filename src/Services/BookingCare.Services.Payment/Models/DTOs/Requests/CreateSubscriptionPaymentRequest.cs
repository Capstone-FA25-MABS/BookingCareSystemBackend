using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    [Required]
    [JsonRequired]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Hospital ID (required)
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required]
    [JsonRequired]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method ID
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Indicates if this is an upgrade from an existing subscription
    /// </summary>
    [JsonRequired]
    public bool IsUpgrade { get; set; } = false;

    /// <summary>
    /// Current hospital subscription ID (required only if IsUpgrade is true)
    /// This is the ID from HospitalSubscription table, not SubscriptionPlan
    /// </summary>
    public Guid? CurrentHospitalSubscriptionId { get; set; }

    /// <summary>
    /// Plan type (billing cycle): MONTHLY, QUARTERLY, YEARLY
    /// Used for frontend redirect after payment
    /// </summary>
    public string? PlanType { get; set; }
}
