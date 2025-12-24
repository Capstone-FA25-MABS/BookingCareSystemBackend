namespace BookingCare.Services.Discount.Enums;

/// <summary>
/// Specifies the entities to which a discount can be applied.
/// </summary>
public enum DiscountApplicableTo
{
    /// <summary>
    /// The discount is applicable to all entities.
    /// </summary>
    ALL,

    /// <summary>
    /// The discount is applicable to a specific specialty.
    /// </summary>
    SPECIALTY,

    /// <summary>
    /// The discount is applicable to a specific doctor.
    /// </summary>
    DOCTOR
}