using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.ServiceMedical.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ServiceMedicalsController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "ServiceMedical", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetServiceMedicalResult(int id)
    {
        // TODO: Implement ServiceMedical logic
        return Ok(new { ServiceMedicalId = id, Status = "Processed" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult CreateServiceMedicalRequest([FromBody] object ServiceMedicalRequest)
    {
        // TODO: Save to DB or process ServiceMedical request
        return Ok(new { Message = "ServiceMedical request created successfully!" });
    }
}