using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Clinic.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Clinic", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetClinicResult(int id)
    {
        // TODO: Implement Clinic logic
        return Ok(new { ClinicResultId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateClinicRequest([FromBody] object ClinicRequest)
    {
        // TODO: Save to DB or process Clinic request
        return Ok(new { Message = "Clinic request created successfully!" });
    }
}