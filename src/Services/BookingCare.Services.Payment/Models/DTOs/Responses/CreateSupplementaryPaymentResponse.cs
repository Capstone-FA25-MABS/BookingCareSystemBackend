using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for creating supplementary payment with payment URL
/// Used for appointment price difference payments (Option 3)
/// </summary>
public class CreateSupplementaryPaymentResponse
{
    /// <summary>
    /// Original appointment ID
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Additional amount being paid
    /// </summary>
    [Required]
    [JsonRequired]
    public decimal AdditionalAmount { get; set; }

    /// <summary>
    /// Payment URL to redirect user to payment gateway
    /// </summary>
    [Required]
    [JsonRequired]
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Payment gateway used (PayOS, VNPay, etc.)
    /// </summary>
    [Required]
    [JsonRequired]
    public string PaymentGateway { get; set; } = string.Empty;

    /// <summary>
    /// Payment expiration time
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// Payment reference/transaction ID from gateway
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Unique identifier for this supplementary payment
    /// Used to track payment in callbacks
    /// </summary>
    [Required]
    [JsonRequired]
    public string SupplementaryPaymentId { get; set; } = string.Empty;
}
