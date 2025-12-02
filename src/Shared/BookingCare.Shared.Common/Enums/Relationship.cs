namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Relationship types for patient relatives
/// </summary>
public enum Relationship
{
    /// <summary>
    /// Parent (Cha/Mẹ)
    /// </summary>
    PARENT = 1,

    /// <summary>
    /// Child (Con)
    /// </summary>
    CHILD = 2,

    /// <summary>
    /// Spouse (Vợ/Chồng)
    /// </summary>
    SPOUSE = 3,

    /// <summary>
    /// Sibling (Anh/Chị/Em)
    /// </summary>
    SIBLING = 4,

    /// <summary>
    /// Grandparent (Ông/Bà)
    /// </summary>
    GRANDPARENT = 5,

    /// <summary>
    /// Grandchild (Cháu)
    /// </summary>
    GRANDCHILD = 6,

    /// <summary>
    /// Other relationship (Khác)
    /// </summary>
    OTHER = 99
}
