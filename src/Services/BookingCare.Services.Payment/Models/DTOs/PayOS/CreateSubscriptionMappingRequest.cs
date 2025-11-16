namespace BookingCare.Services.Payment.Models.DTOs.PayOS;

/// <summary>
/// Request to create a PayOS subscription payment mapping
/// </summary>
public class CreateSubscriptionMappingRequest
{
    /// <summary>
    /// Payment ID in the system
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Order code from PayOS
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// Subscription Plan ID
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }

    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Is this an upgrade (nullable for clarity)
    /// </summary>
    public bool? IsUpgrade { get; set; }

    /// <summary>
    /// Current Hospital Subscription ID (for upgrade)
    /// </summary>
    public Guid? CurrentHospitalSubscriptionId { get; set; }

    /// <summary>
    /// Plan type (billing cycle): MONTHLY, QUARTERLY, YEARLY
    /// </summary>
    public string? PlanType { get; set; }

    /// <summary>
    /// Expiration time (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
