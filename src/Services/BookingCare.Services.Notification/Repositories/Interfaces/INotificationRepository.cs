using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Models.Entities;

namespace BookingCare.Services.Notification.Repositories.Interfaces;

public interface INotificationRepository
{
    /// <summary>
    /// Create a new notification
    /// </summary>
    Task<NotificationEntity> CreateAsync(NotificationEntity notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<NotificationEntity?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for a user with pagination
    /// </summary>
    Task<List<NotificationEntity>> GetUserNotificationsAsync(
        string userId,
        int skip,
        int limit,
        bool? isRead = null,
        NotificationType? notificationType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<long> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete notification
    /// </summary>
    Task<bool> DeleteAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all notifications for a user
    /// </summary>
    Task<long> DeleteAllAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread count for a user
    /// </summary>
    Task<long> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count for a user
    /// </summary>
    Task<long> GetTotalCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification counts by type for a user
    /// </summary>
    Task<Dictionary<NotificationType, long>> GetCountsByTypeAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default);
}

