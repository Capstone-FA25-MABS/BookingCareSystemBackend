using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Notification.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("register")]
    public IActionResult Register([FromBody] DeviceRegistrationRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.DeviceName) || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest("Invalid request data", new List<string> { "DeviceName and Token are required" });
        }

        var device = _deviceStore.AddOrUpdate(request.DeviceName, request.Token);
        _logger.LogInformation("Device registered: {DeviceName} with ID {DeviceId}", request.DeviceName, device.Id);

        return Success(new { DeviceId = device.Id, DeviceName = device.Name }, "Device registered successfully");
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var devices = _deviceStore.All;
        return Success(devices, "Devices retrieved successfully");
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var device = _deviceStore.Get(id);
        if (device == null)
        {
            return NotFound("Device not found");
        }

        return Success(device, "Device retrieved successfully");
    }
}
