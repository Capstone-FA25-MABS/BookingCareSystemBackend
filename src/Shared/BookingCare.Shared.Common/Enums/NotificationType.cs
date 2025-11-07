namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Types of notifications in the system
/// Shared across all services to ensure consistency
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// General notification (default)
    /// </summary>
    General = 0,

    /// <summary>
    /// Appointment booking confirmed
    /// </summary>
    BookingConfirmation = 1,

    /// <summary>
    /// Appointment cancelled
    /// </summary>
    BookingCancellation = 2,

    /// <summary>
    /// Reminder for upcoming appointment
    /// </summary>
    BookingReminder = 3,

    /// <summary>
    /// Payment processed successfully
    /// </summary>
    PaymentSuccess = 4,

    /// <summary>
    /// Payment failed or rejected
    /// </summary>
    PaymentFailed = 5,

    /// <summary>
    /// Refund processed
    /// </summary>
    RefundProcessed = 6,

    /// <summary>
    /// User account updated (profile, settings, etc.)
    /// </summary>
    AccountUpdate = 7,

    /// <summary>
    /// System-wide announcement
    /// </summary>
    SystemAnnouncement = 8,

    /// <summary>
    /// System alert or warning
    /// </summary>
    SystemAlert = 9
}

