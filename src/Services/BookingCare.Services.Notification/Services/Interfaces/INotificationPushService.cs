using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Services.Interfaces;

/// <summary>
/// Service for pushing notifications via SignalR
/// </summary>
public interface INotificationPushService
{
    /// <summary>
    /// Send notification to a specific user via SignalR
    /// </summary>
    Task SendNotificationToUserAsync(string userId, NotificationDto notification);

    /// <summary>
    /// Send notification update (e.g., mark as read) to user
    /// </summary>
    Task SendNotificationUpdateAsync(string userId, string notificationId, bool isRead);

    /// <summary>
    /// Send unread count update to user
    /// </summary>
    Task SendUnreadCountUpdateAsync(string userId, long unreadCount);
}

