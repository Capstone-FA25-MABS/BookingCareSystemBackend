using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for daily plan operations (combined meal + workout)
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/daily-plans")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize(Policy = "Role:Patient")]
public class DailyPlansController : BaseApiController
{
    private readonly INutritionService _nutritionService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DailyPlansController> _logger;

    public DailyPlansController(
        INutritionService nutritionService,
        IEventBus eventBus,
        ILogger<DailyPlansController> logger)
    {
        _nutritionService = nutritionService;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Get daily plan (meal + workout) for a specific date
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetDailyPlan(
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var targetDate = date ?? DateTime.UtcNow.Date;

        _logger.LogInformation("Getting daily plan for AccountId: {AccountId}, Date: {Date}",
            accountId, targetDate);

        var dailyPlan = await _nutritionService.GetDailyPlanAsync(
            accountId, targetDate, cancellationToken);

        // Return 404 if no plans exist for this date
        if (dailyPlan.MealPlan == null && dailyPlan.WorkoutPlan == null)
        {
            _logger.LogWarning("No daily plan found for AccountId: {AccountId}, Date: {Date}",
                accountId, targetDate);
            return NotFound("No daily plan found for this date");
        }

        return Success(dailyPlan, "Daily plan retrieved successfully");
    }

    /// <summary>
    /// Generate daily plan (meal + workout) for a specific date
    /// </summary>
    [HttpPost("generate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GenerateDailyPlan(
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var targetDate = date ?? DateTime.UtcNow.Date;

        _logger.LogInformation("Generating daily plan for AccountId: {AccountId}, Date: {Date}",
            accountId, targetDate);

        // Generate both meal and workout plans
        var mealPlan = await _nutritionService.GenerateDailyMealPlanAsync(
            accountId, targetDate, cancellationToken);
        var workoutPlan = await _nutritionService.GenerateDailyWorkoutPlanAsync(
            accountId, targetDate, cancellationToken);

        var dailyPlan = new DailyPlanDto
        {
            Date = targetDate,
            MealPlan = mealPlan,
            WorkoutPlan = workoutPlan,
            TotalCaloriesConsumed = mealPlan.TotalCalories,
            TotalCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
            CompletionPercentage = 0
        };

        // Publish notification events
        try
        {
            var mealPlanEvent = new DailyMealPlanGeneratedEvent
            {
                UserId = accountId,
                MealPlanId = mealPlan.Id,
                Date = targetDate,
                TotalCalories = mealPlan.TotalCalories,
                TotalProteinG = mealPlan.TotalProteinG,
                TotalCarbsG = mealPlan.TotalCarbsG,
                TotalFatG = mealPlan.TotalFatG,
                MealCount = mealPlan.Meals.Count,
                GeneratedAt = DateTime.UtcNow
            };
            await _eventBus.PublishAsync(mealPlanEvent);

            var workoutPlanEvent = new DailyWorkoutPlanGeneratedEvent
            {
                UserId = accountId,
                WorkoutPlanId = workoutPlan.Id,
                Date = targetDate,
                WorkoutType = workoutPlan.WorkoutType,
                DurationMinutes = workoutPlan.DurationMinutes,
                EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
                ExerciseCount = workoutPlan.Exercises.Count,
                GeneratedAt = DateTime.UtcNow
            };
            await _eventBus.PublishAsync(workoutPlanEvent);

            _logger.LogInformation(
                "Published nutrition notification events for AccountId: {AccountId}, Date: {Date}",
                accountId, targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification events for AccountId: {AccountId}", accountId);
            // Don't fail the request if notification publishing fails
        }

        _logger.LogInformation("Daily plan generated successfully for AccountId: {AccountId}", accountId);
        return Success(dailyPlan, "Daily plan generated successfully");
    }
}
