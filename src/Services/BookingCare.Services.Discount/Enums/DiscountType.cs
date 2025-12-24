namespace BookingCare.Services.Discount.Enums;

/// <summary>
/// Specifies the type of discount to be applied.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// A fixed amount discount, which subtracts a specific monetary value from the total.
    /// </summary>
    FIXED_AMOUNT,

    /// <summary>
    /// A percentage discount, which subtracts a percentage of the total amount.
    /// </summary>
    PERCENTAGE
}