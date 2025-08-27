using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Appointment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Appointment", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetAppointmentResult(int id)
    {
        // TODO: Implement Appointment logic
        return Ok(new { AppointmentResultId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateAppointmentRequest([FromBody] object AppointmentRequest)
    {
        // TODO: Save to DB or process Appointment request
        return Ok(new { Message = "Appointment request created successfully!" });
    }
}