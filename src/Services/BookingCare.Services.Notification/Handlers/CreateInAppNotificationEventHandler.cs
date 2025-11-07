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
            @event.UserId, @event.Type, @event.Content.TitleVi);

        try
        {
            // Create notification DTO using composition (content from event.Content)
            var notificationDto = new CreateNotificationDto
            {
                UserId = @event.UserId,
                Type = @event.Type,
                // Copy properties from NotificationContent
                TitleVi = @event.Content.TitleVi,
                TitleEn = @event.Content.TitleEn,
                ContentVi = @event.Content.ContentVi,
                ContentEn = @event.Content.ContentEn,
                Metadata = @event.Content.Metadata,
                ActionUrl = @event.Content.ActionUrl,
                Icon = @event.Content.Icon,
                Priority = @event.Content.Priority,
                ExpirationDays = @event.Content.ExpirationDays
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


