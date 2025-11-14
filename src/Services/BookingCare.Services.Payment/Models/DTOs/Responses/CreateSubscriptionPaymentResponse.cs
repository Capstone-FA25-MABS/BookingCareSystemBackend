namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for creating subscription payment with payment URL
/// </summary>
public class CreateSubscriptionPaymentResponse
{
    /// <summary>
    /// Payment information
    /// </summary>
    public PaymentResponse Payment { get; set; } = null!;

    /// <summary>
    /// Payment URL from payment gateway (PayOS/VNPay)
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Payment gateway used (PayOS/VNPay)
    /// </summary>
    public string PaymentGateway { get; set; } = string.Empty;

    /// <summary>
    /// Payment expiration time
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// Payment reference number from gateway
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Indicates if this is an upgrade (true) or new subscription (false)
    /// </summary>
    public bool IsUpgrade { get; set; }

    /// <summary>
    /// Subscription ID (subscription plan ID)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Current subscription ID (only for upgrade)
    /// </summary>
    public Guid? CurrentSubscriptionId { get; set; }
}
