using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Auth", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetAuthResult(int id)
    {
        // TODO: Implement Auth logic
        return Ok(new { AuthResultId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateAuthRequest([FromBody] object AuthRequest)
    {
        // TODO: Save to DB or process Auth request
        return Ok(new { Message = "Auth request created successfully!" });
    }
}