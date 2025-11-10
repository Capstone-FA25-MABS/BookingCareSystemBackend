using BookingCare.Services.Notification.Hubs;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Notification.Services.Implementations;

/// <summary>
/// Service for pushing real-time notifications via SignalR
/// </summary>
public class NotificationPushService : BaseService, INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationPushService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationPushService> logger) : base(logger)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationToUserAsync(string userId, NotificationDto notification)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            // Send to user's personal group (all their connected clients)
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", notification);

            LogInfo("Sent notification {NotificationId} to user {UserId} via SignalR",
                null, notification.Id, userId);
        }, nameof(SendNotificationToUserAsync));
    }

    public async Task SendNotificationUpdateAsync(string userId, string notificationId, bool isRead)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            await _hubContext.Clients.Group(userId).SendAsync("NotificationUpdated", new
            {
                NotificationId = notificationId,
                IsRead = isRead
            });

            LogInfo("Sent notification update for {NotificationId} to user {UserId}",
                null, notificationId, userId);
        }, nameof(SendNotificationUpdateAsync));
    }

    public async Task SendUnreadCountUpdateAsync(string userId, long unreadCount)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            await _hubContext.Clients.Group(userId).SendAsync("UnreadCountUpdated", new
            {
                UnreadCount = unreadCount
            });

            LogInfo("Sent unread count update ({Count}) to user {UserId}",
                null, unreadCount, userId);
        }, nameof(SendUnreadCountUpdateAsync));
    }
}

