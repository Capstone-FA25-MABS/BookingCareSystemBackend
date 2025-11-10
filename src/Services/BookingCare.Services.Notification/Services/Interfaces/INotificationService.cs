using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Create and send a notification to user
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user notifications with pagination
    /// </summary>
    Task<List<NotificationDto>> GetUserNotificationsAsync(
        string userId,
        int pageNumber,
        int pageSize,
        bool? isRead = null,
        NotificationType? notificationType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<long> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(string userId, string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all notifications for a user
    /// </summary>
    Task<long> DeleteAllNotificationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification summary (total and unread count)
    /// </summary>
    Task<NotificationSummaryDto> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification counts by type
    /// </summary>
    Task<Dictionary<NotificationType, long>> GetCountsByTypeAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default);
}

