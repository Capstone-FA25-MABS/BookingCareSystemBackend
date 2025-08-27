using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.ServiceMedical.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceMedicalsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "ServiceMedical", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetServiceMedicalResult(int id)
    {
        // TODO: Implement ServiceMedical logic
        return Ok(new { ServiceMedicalId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateServiceMedicalRequest([FromBody] object ServiceMedicalRequest)
    {
        // TODO: Save to DB or process ServiceMedical request
        return Ok(new { Message = "ServiceMedical request created successfully!" });
    }
}