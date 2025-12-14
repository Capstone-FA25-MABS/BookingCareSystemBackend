using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Notification.Controllers;

/// <summary>
/// Controller for general notification operations
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly INotificationPushService _notificationPushService;

    public NotificationsController(
        INotificationService notificationService,
        INotificationPushService notificationPushService)
    {
        _notificationService = notificationService;
        _notificationPushService = notificationPushService;
    }

    /// <summary>
    /// Health check endpoint for general notification service
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Success(new
        {
            Status = "Healthy",
            Service = "Notification-General",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get current user's notifications with pagination
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin,Patient")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        [FromQuery] NotificationType? notificationType = null,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var notifications = await _notificationService.GetUserNotificationsAsync(
            accountId.ToString(), pageNumber, pageSize, isRead, notificationType, cancellationToken);

        return Success(notifications, "Notifications retrieved successfully");
    }

    /// <summary>
    /// Get notification summary (total and unread count)
    /// </summary>
    [HttpGet("summary")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin,Patient")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var summary = await _notificationService.GetSummaryAsync(accountId.ToString(), cancellationToken);

        return Success(summary, "Summary retrieved successfully");
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{notificationId}/read")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var accountIdStr = accountId.ToString();
        var result = await _notificationService.MarkAsReadAsync(accountIdStr, notificationId, cancellationToken);

        if (!result)
        {
            return NotFound("Notification not found");
        }

        // Push update via SignalR
        await _notificationPushService.SendNotificationUpdateAsync(accountIdStr, notificationId, true);

        // Update unread count
        var summary = await _notificationService.GetSummaryAsync(accountIdStr, cancellationToken);
        await _notificationPushService.SendUnreadCountUpdateAsync(accountIdStr, summary.UnreadCount);

        return Success(true, "Notification marked as read");
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var accountIdStr = accountId.ToString();
        var count = await _notificationService.MarkAllAsReadAsync(accountIdStr, cancellationToken);

        // Update unread count via SignalR
        await _notificationPushService.SendUnreadCountUpdateAsync(accountIdStr, 0);

        return Success(count, $"Marked {count} notifications as read");
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> DeleteNotification(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var accountIdStr = accountId.ToString();
        var result = await _notificationService.DeleteNotificationAsync(accountIdStr, notificationId, cancellationToken);

        if (!result)
        {
            return NotFound("Notification not found");
        }

        // Update unread count via SignalR
        var summary = await _notificationService.GetSummaryAsync(accountIdStr, cancellationToken);
        await _notificationPushService.SendUnreadCountUpdateAsync(accountIdStr, summary.UnreadCount);

        return Success(true, "Notification deleted");
    }

    /// <summary>
    /// Delete all notifications for the current user
    /// </summary>
    [HttpDelete("delete-all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> DeleteAllNotifications(CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var accountIdStr = accountId.ToString();
        var count = await _notificationService.DeleteAllNotificationsAsync(accountIdStr, cancellationToken);

        // Update unread count via SignalR (should be 0 after deleting all)
        await _notificationPushService.SendUnreadCountUpdateAsync(accountIdStr, 0);

        return Success(count, $"Deleted {count} notifications");
    }

    /// <summary>
    /// Get notification counts by type for the current user
    /// </summary>
    [HttpGet("counts-by-type")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetCountsByType(
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var counts = await _notificationService.GetCountsByTypeAsync(accountId.ToString(), isRead, cancellationToken);

        return Success(counts, "Counts retrieved successfully");
    }
}

