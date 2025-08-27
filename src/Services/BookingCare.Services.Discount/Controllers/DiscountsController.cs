using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Discount.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Discount", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetDiscountResult(int id)
    {
        // TODO: Implement Discount logic
        return Ok(new { DiscountId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateDiscountRequest([FromBody] object DiscountRequest)
    {
        // TODO: Save to DB or process Discount request
        return Ok(new { Message = "Discount request created successfully!" });
    }
}