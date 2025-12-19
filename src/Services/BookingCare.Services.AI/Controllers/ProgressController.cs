using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for progress tracking and statistics
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/progress")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize(Policy = "Role:Patient")]
public class ProgressController : BaseApiController
{
    private readonly INutritionService _nutritionService;

    public ProgressController(INutritionService nutritionService)
    {
        _nutritionService = nutritionService;
    }

    /// <summary>
    /// Get progress statistics for current user
    /// </summary>
    [HttpGet("stats")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetProgressStats(CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var stats = await _nutritionService.GetProgressStatsAsync(userId, cancellationToken);

        return Success(stats, "Progress statistics retrieved successfully");
    }
}
