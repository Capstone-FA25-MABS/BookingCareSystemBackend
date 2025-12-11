using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for workout plan operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/workout-plans")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class WorkoutPlansController : BaseApiController
{
    private readonly INutritionService _nutritionService;

    public WorkoutPlansController(INutritionService nutritionService)
    {
        _nutritionService = nutritionService;
    }

    /// <summary>
    /// Generate workout plan for a specific date
    /// </summary>
    [HttpPost("generate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GenerateWorkoutPlan(
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var targetDate = date ?? DateTime.UtcNow.Date;

        var workoutPlan = await _nutritionService.GenerateDailyWorkoutPlanAsync(
            userId, targetDate, cancellationToken);

        return Success(workoutPlan, "Workout plan generated successfully");
    }

    /// <summary>
    /// Get workout plan by date
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetWorkoutPlanByDate(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var workoutPlan = await _nutritionService.GetWorkoutPlanByDateAsync(
            userId, date, cancellationToken);

        if (workoutPlan == null)
        {
            return NotFound($"Workout plan not found for date: {date:yyyy-MM-dd}");
        }

        return Success(workoutPlan, "Workout plan retrieved successfully");
    }

    /// <summary>
    /// Get today's workout plan
    /// </summary>
    [HttpGet("today")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetTodayWorkoutPlan(CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var today = DateTime.UtcNow.Date;
        var workoutPlan = await _nutritionService.GetWorkoutPlanByDateAsync(
            userId, today, cancellationToken);

        if (workoutPlan == null)
        {
            return NotFound("No workout plan found for today. Generate one first.");
        }

        return Success(workoutPlan, "Today's workout plan retrieved successfully");
    }
}
