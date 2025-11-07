using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Generic event handler to create in-app notifications
/// This handler can process any CreateInAppNotificationEvent from any service
/// Creates persistent notifications in MongoDB and pushes them via SignalR
/// </summary>
public class CreateInAppNotificationEventHandler : IIntegrationEventHandler<CreateInAppNotificationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationPushService _notificationPushService;
    private readonly ILogger<CreateInAppNotificationEventHandler> _logger;

    public CreateInAppNotificationEventHandler(
        INotificationService notificationService,
        INotificationPushService notificationPushService,
        ILogger<CreateInAppNotificationEventHandler> logger)
    {
        _notificationService = notificationService;
        _notificationPushService = notificationPushService;
        _logger = logger;
    }

    public async Task HandleAsync(CreateInAppNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Creating in-app notification - UserId: {UserId}, Type: {Type}, TitleVi: {TitleVi}",
            @event.UserId, @event.Type, @event.TitleVi);

        try
        {
            // Create notification DTO directly from event (bilingual support!)
            var notificationDto = new CreateNotificationDto
            {
                UserId = @event.UserId,
                Type = @event.Type,
                TitleVi = @event.TitleVi,
                TitleEn = @event.TitleEn,
                ContentVi = @event.ContentVi,
                ContentEn = @event.ContentEn,
                Metadata = @event.Metadata,
                ActionUrl = @event.ActionUrl,
                Icon = @event.Icon,
                Priority = @event.Priority,
                ExpirationDays = @event.ExpirationDays
            };

            // Save to MongoDB
            var createdNotification = await _notificationService.CreateNotificationAsync(notificationDto, cancellationToken);

            // Push to user via SignalR
            await _notificationPushService.SendNotificationToUserAsync(@event.UserId, createdNotification);

            // Update unread count
            var summary = await _notificationService.GetSummaryAsync(@event.UserId, cancellationToken);
            await _notificationPushService.SendUnreadCountUpdateAsync(@event.UserId, summary.UnreadCount);

            _logger.LogInformation(
                "[NotificationService] Successfully created and pushed in-app notification - NotificationId: {NotificationId}, UserId: {UserId}",
                createdNotification.Id, @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to create in-app notification - UserId: {UserId}, Type: {Type}",
                @event.UserId, @event.Type);
        }
    }
}


