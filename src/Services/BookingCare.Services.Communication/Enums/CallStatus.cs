namespace BookingCare.Services.Communication.Enums;

/// <summary>
/// Represents the status of a call.
/// </summary>
public enum CallStatus
{
    /// <summary>
    /// The call was accepted and completed
    /// </summary>
    Accepted,

    /// <summary>
    /// The call was missed (not answered)
    /// </summary>
    Missed,

    /// <summary>
    /// The call was rejected by the receiver
    /// </summary>
    Rejected
}