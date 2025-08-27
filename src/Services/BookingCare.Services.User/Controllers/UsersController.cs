using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.User.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "User", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetUserResult(int id)
    {
        // TODO: Implement User logic
        return Ok(new { UserId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateUserRequest([FromBody] object UserRequest)
    {
        // TODO: Save to DB or process User request
        return Ok(new { Message = "User request created successfully!" });
    }
}