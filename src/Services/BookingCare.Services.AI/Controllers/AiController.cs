using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
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
