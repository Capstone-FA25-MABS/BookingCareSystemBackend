namespace BookingCare.Services.Schedule.Enums;

/// <summary>
/// Represents the scheduling patterns for appointments.
/// </summary>
public enum SchedulePatterns
{
    /// <summary>
    /// Morning pattern (e.g., 08:00 - 12:00)
    /// </summary>
    MORNING = 1,

    /// <summary>
    /// Afternoon pattern (e.g., 13:00 - 17:00)
    /// </summary>
    AFTERNOON = 2,

    /// <summary>
    /// Evening pattern (e.g., 17:00 - 21:00)
    /// </summary>
    EVENING = 3,

    /// <summary>
    /// Full day pattern (e.g., 08:00 - 21:00)
    /// </summary>
    FULL_DAY = 4
}