using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Communication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunicationsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Communication", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetCommunicationResult(int id)
    {
        // TODO: Implement Communication logic
        return Ok(new { CommunicationResultId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateCommunicationRequest([FromBody] object CommunicationRequest)
    {
        // TODO: Save to DB or process Communication request
        return Ok(new { Message = "Communication request created successfully!" });
    }
}