using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Notification.Controllers;

/// <summary>
/// Controller for device registration and management for push notifications
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class DeviceController : BaseApiController
{
    private readonly ILogger<DeviceController> _logger;
    private readonly DeviceStore _deviceStore;

    public DeviceController(ILogger<DeviceController> logger, DeviceStore deviceStore)
    {
        _logger = logger;
        _deviceStore = deviceStore;
    }

    /// <summary>
    /// Health check endpoint for Device service
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
            Service = "Notification-Device",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Register a device for push notifications (Form data format) 
    /// </summary>
    /// <param name="deviceName">Device name</param>
    /// <param name="token">FCM token</param>
    /// <returns>Device registration result with assigned device ID</returns>
    [HttpPost("register")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult RegisterForm([FromForm] string deviceName, [FromForm] string token)
    {
        if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid request data", new List<string> { "deviceName and token are required" });
        }

        var device = _deviceStore.AddOrUpdate(deviceName, token);
        _logger.LogInformation("Device registered: {DeviceName} with ID {DeviceId}", deviceName, device.Id);

        return Success(new { DeviceId = device.Id, DeviceName = device.Name }, "Device registered successfully");
    }

    /// <summary>
    /// Get all registered devices
    /// </summary>
    /// <returns>List of all registered devices</returns>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult GetAll()
    {
        var devices = _deviceStore.All;
        return Success(devices, "Devices retrieved successfully");
    }
}