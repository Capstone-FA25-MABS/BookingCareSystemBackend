using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Notification.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Notification", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetNotificationResult(int id)
    {
        // TODO: Implement Notification logic
        return Ok(new { NotificationId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateNotificationRequest([FromBody] object NotificationRequest)
    {
        // TODO: Save to DB or process Notification request
        return Ok(new { Message = "Notification request created successfully!" });
    }
}