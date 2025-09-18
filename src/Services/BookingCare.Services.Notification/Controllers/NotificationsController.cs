using BookingCare.Shared.Common.Controllers;
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
    /// Get notification result by ID
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <returns>Notification status and details</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult GetNotificationResult(int id)
    {
        // TODO: Implement Notification logic
        return Success(new { NotificationId = id, Status = "Processed" }, "Notification result retrieved successfully");
    }

    /// <summary>
    /// Create a new notification request
    /// </summary>
    /// <param name="notificationRequest">Notification request details</param>
    /// <returns>Confirmation of notification request creation</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult CreateNotificationRequest([FromBody] object notificationRequest)
    {
        // TODO: Save to DB or process Notification request
        return Success(new { Message = "Notification request created successfully!" });
    }
}