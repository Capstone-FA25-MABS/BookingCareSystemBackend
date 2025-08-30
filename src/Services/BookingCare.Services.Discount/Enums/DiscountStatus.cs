namespace BookingCare.Services.Discount.Enums;

/// <summary>
/// Represents the status of a discount.
/// </summary>
public enum DiscountStatus
{
    /// <summary>
    /// The discount is currently active and can be used.
    /// </summary>
    ACTIVE,

    /// <summary>
    /// The discount is currently inactive and cannot be used.
    /// </summary>
    INACTIVE,

    /// <summary>
    /// The discount has expired and is no longer valid.
    /// </summary>
    EXPIRED
}