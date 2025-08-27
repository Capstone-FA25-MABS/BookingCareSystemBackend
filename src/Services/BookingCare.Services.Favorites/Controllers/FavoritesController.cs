using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Favorites.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Favorites", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public IActionResult GetFavoriteResult(int id)
    {
        // TODO: Implement Favorites logic
        return Ok(new { FavoriteId = id, Status = "Processed" });
    }

    [HttpPost]
    public IActionResult CreateFavoriteRequest([FromBody] object FavoriteRequest)
    {
        // TODO: Save to DB or process Favorite request
        return Ok(new { Message = "Favorite request created successfully!" });
    }
}