namespace BookingCare.Services.Appointment.Enums;

/// <summary>
/// Pending reschedule action types for lazy token generation
/// </summary>
public static class PendingRescheduleAction
{
    /// <summary>
    /// Patient wants to reschedule with same doctor (Option 1)
    /// </summary>
    public const string SAME_DOCTOR = "SAME_DOCTOR";

    /// <summary>
    /// Patient wants to choose a new doctor (Option 3)
    /// </summary>
    public const string NEW_DOCTOR = "NEW_DOCTOR";
}

