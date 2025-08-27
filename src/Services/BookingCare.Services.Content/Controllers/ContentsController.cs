using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Content.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Content", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetContentResult(int id)
    {
        // TODO: Implement Content logic
        return Ok(new { ContentId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateContentRequest([FromBody] object ContentRequest)
    {
        // TODO: Save to DB or process Content request
        return Ok(new { Message = "Content request created successfully!" });
    }
}