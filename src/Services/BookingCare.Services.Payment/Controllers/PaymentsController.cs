using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Payment", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetPaymentResult(int id)
    {
        // TODO: Implement Payment logic
        return Ok(new { PaymentId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreatePaymentRequest([FromBody] object PaymentRequest)
    {
        // TODO: Save to DB or process Payment request
        return Ok(new { Message = "Payment request created successfully!" });
    }
}