namespace BookingCare.Services.Communication.Enums;

/// <summary>
/// Represents the status of a message.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// The message has been sent.
    /// </summary>
    SENT,

    /// <summary>
    /// The message has been delivered.
    /// </summary>
    DELIVERED,

    /// <summary>
    /// The message has been read.
    /// </summary>
    READ,

    /// <summary>
    /// The message has not been read.
    /// </summary>
    UNREAD,

    /// <summary>
    /// The message has been recalled by sender.
    /// </summary>
    RECALLED,
}
