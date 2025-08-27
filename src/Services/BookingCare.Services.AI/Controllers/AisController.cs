using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // This requires authentication for all endpoints
public class AisController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous] // Allow health check without authentication
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "AI", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetAiResult(int id)
    {
        // TODO: Implement AI logic
        return Ok(new { AiResultId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateAiRequest([FromBody] object aiRequest)
    {
        // TODO: Save to DB or process AI request
        return Ok(new { Message = "AI request created successfully!" });
    }
}
