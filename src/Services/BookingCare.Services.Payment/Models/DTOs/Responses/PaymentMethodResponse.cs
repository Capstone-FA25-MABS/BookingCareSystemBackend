using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for payment method
/// </summary>
public class PaymentMethodResponse
{
    /// <summary>
    /// ID of the payment method
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Payment method name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the payment method
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL of the payment method image/logo
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Status of the payment method
    /// </summary>
    public PaymentMethodStatus Status { get; set; }
}