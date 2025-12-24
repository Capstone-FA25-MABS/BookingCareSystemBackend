namespace BookingCare.Services.Schedule.Enums;

/// <summary>
/// Represents the types of exceptions that can occur in the scheduling system.
/// </summary>
public enum ExceptionType
{
    /// <summary>
    /// Indicates that a specific slot is blocked and cannot be booked.
    /// </summary>
    BLOCK_SLOT = 1,

    /// <summary>
    /// Indicates that a previously blocked slot is now unblocked and available for booking.
    /// </summary>
    UNBLOCK_SLOT = 2,

    /// <summary>
    /// Represents a day off when no slots are available for booking.
    /// </summary>
    DAY_OFF = 3,

    /// <summary>
    /// Indicates a change in the capacity for a slot or schedule.
    /// </summary>
    CAPACITY_CHANGE = 4
}