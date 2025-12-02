namespace BookingCare.Services.Schedule.Enums;

/// <summary>
/// Target type for hold slot operations
/// </summary>
public enum HoldSlotTargetType
{
    Doctor = 0,
    ServiceMedical = 1,
    /// <summary>
    /// Specialty-based holding for "hospital assigns doctor" mode
    /// Uses capacity-based holding where multiple users can hold the same time slot
    /// </summary>
    Specialty = 2
}
