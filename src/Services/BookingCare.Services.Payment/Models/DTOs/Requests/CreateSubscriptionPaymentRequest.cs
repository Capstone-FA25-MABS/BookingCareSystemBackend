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
}