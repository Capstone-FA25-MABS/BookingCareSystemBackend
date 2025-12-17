using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for meal plan operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/meal-plans")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize(Policy = "Role:Patient")]
public class MealPlansController : BaseApiController
{
    private readonly INutritionService _nutritionService;

    public MealPlansController(INutritionService nutritionService)
    {
        _nutritionService = nutritionService;
    }

    /// <summary>
    /// Generate meal plan for a specific date
    /// </summary>
    [HttpPost("generate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GenerateMealPlan(
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var targetDate = date ?? DateTime.UtcNow.Date;

        var mealPlan = await _nutritionService.GenerateDailyMealPlanAsync(
            userId, targetDate, cancellationToken);

        return Success(mealPlan, "Meal plan generated successfully");
    }

    /// <summary>
    /// Get meal plan by date
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetMealPlanByDate(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var mealPlan = await _nutritionService.GetMealPlanByDateAsync(
            userId, date, cancellationToken);

        if (mealPlan == null)
        {
            return NotFound($"Meal plan not found for date: {date:yyyy-MM-dd}");
        }

        return Success(mealPlan, "Meal plan retrieved successfully");
    }

    /// <summary>
    /// Get today's meal plan
    /// </summary>
    [HttpGet("today")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetTodayMealPlan(CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var today = DateTime.UtcNow.Date;
        var mealPlan = await _nutritionService.GetMealPlanByDateAsync(
            userId, today, cancellationToken);

        if (mealPlan == null)
        {
            return NotFound("No meal plan found for today. Generate one first.");
        }

        return Success(mealPlan, "Today's meal plan retrieved successfully");
    }

    /// <summary>
    /// Mark a meal as completed
    /// </summary>
    [HttpPost("{id}/complete")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> CompleteMeal(
        Guid id,
        [FromBody] CompleteItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var mealPlan = await _nutritionService.CompleteMealAsync(
            id, dto.ItemIndex, cancellationToken);

        return Success(mealPlan, "Meal marked as completed");
    }
}
