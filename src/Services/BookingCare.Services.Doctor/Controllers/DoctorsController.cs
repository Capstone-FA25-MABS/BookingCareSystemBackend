using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Doctor", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetDoctorResult(int id)
    {
        // TODO: Implement Doctor logic
        return Ok(new { DoctorId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateDoctorRequest([FromBody] object DoctorRequest)
    {
        // TODO: Save to DB or process Doctor request
        return Ok(new { Message = "Doctor request created successfully!" });
    }
}