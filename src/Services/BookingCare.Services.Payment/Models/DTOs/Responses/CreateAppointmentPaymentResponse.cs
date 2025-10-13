using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for creating appointment payment with payment URL
/// </summary>
public class CreateAppointmentPaymentResponse
{
    /// <summary>
    /// Created payment information
    /// </summary>
    [Required]
    [JsonRequired]
    public PaymentResponse Payment { get; set; } = new();

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
    /// Additional payment reference/transaction ID from gateway
    /// </summary>
    public string? PaymentReference { get; set; }
}