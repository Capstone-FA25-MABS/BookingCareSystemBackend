using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Review.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Review", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetReviewResult(int id)
    {
        // TODO: Implement Review logic
        return Ok(new { ReviewId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateReviewRequest([FromBody] object ReviewRequest)
    {
        // TODO: Save to DB or process Review request
        return Ok(new { Message = "Review request created successfully!" });
    }
}