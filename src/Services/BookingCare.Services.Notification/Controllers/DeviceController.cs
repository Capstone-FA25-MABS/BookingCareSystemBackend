using BookingCare.Services.Notification.Models.DTOs;
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
    /// Register a device for push notifications
    /// </summary>
    /// <param name="request">Device registration request containing device name and FCM token</param>
    /// <returns>Device registration result with assigned device ID</returns>
    [HttpPost("register")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Register([FromBody] DeviceRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", new List<string> { "DeviceName and Token are required" });
        }

        var device = _deviceStore.AddOrUpdate(request.DeviceName, request.Token);
        _logger.LogInformation("Device registered: {DeviceName} with ID {DeviceId}", request.DeviceName, device.Id);

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