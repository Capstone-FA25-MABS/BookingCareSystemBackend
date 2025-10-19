namespace BookingCare.Services.Hospital.Enums;

/// <summary>
/// Represents the status of a subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and valid.
    /// </summary>
    ACTIVE,

    /// <summary>
    /// Subscription has expired (end_date < GETDATE()).
    /// </summary>
    EXPIRED,

    /// <summary>
    /// Subscription was cancelled by the hospital before expiry.
    /// </summary>
    CANCELLED,

    /// <summary>
    /// Subscription is pending payment or confirmation.
    /// </summary>
    PENDING,

    /// <summary>
    /// Subscription is in trial period.
    /// </summary>
    TRIAL
}
