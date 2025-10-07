namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Represents the billing cycle options for a subscription plan.
/// </summary>
public enum SubscriptionPlanBillingCycle
{
    /// <summary>
    /// Billing occurs every month.
    /// </summary>
    MONTHLY,

    /// <summary>
    /// Billing occurs every quarter (three months).
    /// </summary>
    QUARTERLY,

    /// <summary>
    /// Billing occurs every year.
    /// </summary>
    YEARLY
}