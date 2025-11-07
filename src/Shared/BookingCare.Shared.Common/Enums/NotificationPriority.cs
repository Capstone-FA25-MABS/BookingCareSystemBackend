namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Priority levels for notifications
/// Shared across all services to ensure consistency
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority (informational)
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority (important)
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent priority (requires immediate attention)
    /// </summary>
    Urgent = 3
}

