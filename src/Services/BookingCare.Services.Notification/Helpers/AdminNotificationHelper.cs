using BookingCare.Services.Auth.Protos;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Helpers;

/// <summary>
/// Helper class for creating admin notifications
/// Reduces code duplication across event handlers
/// </summary>
public class AdminNotificationHelper
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly AuthService.AuthServiceClient _authGrpcClient;

    public AdminNotificationHelper(
        ILogger logger,
        IEventBus eventBus,
        AuthService.AuthServiceClient authGrpcClient)
    {
        _logger = logger;
        _eventBus = eventBus;
        _authGrpcClient = authGrpcClient;
    }

    /// <summary>
    /// Create and publish notifications to all admin accounts
    /// </summary>
    /// <param name="content">Notification content with localized titles and messages</param>
    /// <param name="handlerName">Name of the calling handler for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task NotifyAllAdminsAsync(
        NotificationContent content,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all admin account IDs via gRPC
            var adminRequest = new GetAccountIdsByRoleRequest
            {
                Role = "Admin",
                ActiveOnly = true
            };

            var adminResponse = await _authGrpcClient.GetAccountIdsByRoleAsync(
                adminRequest,
                cancellationToken: cancellationToken);

            if (!adminResponse.Success || !adminResponse.AccountIds.Any())
            {
                _logger.LogWarning(
                    "[{HandlerName}] No admin accounts found to send notification. Message: {Message}",
                    handlerName,
                    adminResponse.Message
                );
                return;
            }

            _logger.LogInformation(
                "[{HandlerName}] Found {Count} admin accounts to notify",
                handlerName,
                adminResponse.AccountIds.Count
            );

            // Publish CreateInAppNotificationEvent for each admin
            var notificationTasks = adminResponse.AccountIds.Select(async adminId =>
            {
                try
                {
                    var notificationEvent = new CreateInAppNotificationEvent
                    {
                        UserId = adminId,
                        Type = NotificationType.HospitalRegistration,
                        Content = content
                    };

                    await _eventBus.PublishAsync(notificationEvent, null, cancellationToken);

                    _logger.LogDebug(
                        "[{HandlerName}] Published notification event for admin: {AdminId}",
                        handlerName,
                        adminId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[{HandlerName}] Failed to publish notification event for admin: {AdminId}",
                        handlerName,
                        adminId
                    );
                }
            });

            await Task.WhenAll(notificationTasks);

            _logger.LogInformation(
                "[{HandlerName}] Completed creating notifications for {Count} admins",
                handlerName,
                adminResponse.AccountIds.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{HandlerName}] Failed to create admin notifications",
                handlerName
            );
            // Don't re-throw - notification failure shouldn't break the main process
        }
    }
}
