using BookingCare.Shared.Common.Enums;

namespace BookingCare.Shared.Common.Models;

/// <summary>
/// Concrete class containing shared notification content properties
/// Can be used via inheritance (CreateNotificationDto) or composition (CreateInAppNotificationEvent)
/// Eliminates code duplication across notification-related classes
/// </summary>
public class NotificationContent
{
    /// <summary>
    /// Notification title in Vietnamese
    /// </summary>
    public string TitleVi { get; set; } = string.Empty;

    /// <summary>
    /// Notification title in English
    /// </summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// Notification content/message in Vietnamese
    /// </summary>
    public string ContentVi { get; set; } = string.Empty;

    /// <summary>
    /// Notification content/message in English
    /// </summary>
    public string ContentEn { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata (e.g., appointmentId, doctorName, etc.)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Optional action URL (e.g., /user/profile?tab=appointments&id=xxx)
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional icon class (e.g., "isax isax-calendar-tick")
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Notification priority (uses shared enum)
    /// Default: Normal
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Number of days before notification expires (soft delete)
    /// Default: 30 days
    /// </summary>
    public int ExpirationDays { get; set; } = 30;
}

