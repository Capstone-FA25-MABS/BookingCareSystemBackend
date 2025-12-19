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
/// Controller for nutrition profile operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/nutrition-profiles")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class NutritionProfilesController : BaseApiController
{
    private readonly INutritionService _nutritionService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<NutritionProfilesController> _logger;

    public NutritionProfilesController(
        INutritionService nutritionService,
        IEventBus eventBus,
        ILogger<NutritionProfilesController> logger)
    {
        _nutritionService = nutritionService;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Create or update nutrition profile for current user
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> CreateOrUpdateProfile(
        [FromBody] CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        _logger.LogInformation("Creating/updating profile for AccountId: {AccountId}", accountId);

        var profile = await _nutritionService.CreateOrUpdateProfileAsync(
            accountId, dto, cancellationToken);

        return Success(profile, "Nutrition profile created/updated successfully");
    }

    /// <summary>
    /// Get nutrition profile for current user
    /// </summary>
    [HttpGet]
    [HttpGet("me")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        _logger.LogInformation("Getting profile for AccountId: {AccountId}", accountId);

        var profile = await _nutritionService.GetProfileByUserIdAsync(accountId, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("No profile found for AccountId: {AccountId}", accountId);
            return NotFound("Nutrition profile not found. Please create one first.");
        }

        _logger.LogInformation("Profile found for AccountId: {AccountId}, ProfileId: {ProfileId}",
            accountId, profile.Id);
        return Success(profile, "Profile retrieved successfully");
    }

    /// <summary>
    /// Calculate health metrics (BMI, BMR, TDEE, macro targets) without saving
    /// </summary>
    [HttpPost("calculate-metrics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> CalculateMetrics(
        [FromBody] CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var metrics = await _nutritionService.CalculateHealthMetricsAsync(accountId, dto, cancellationToken);
        return Success(metrics, "Metrics calculated successfully");
    }

    /// <summary>
    /// Test notification - Send meal and workout plan after 1 minute
    /// </summary>
    [HttpPost("test-notification")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public IActionResult TestNotification()
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

            _logger.LogInformation("Test notification requested by AccountId: {AccountId}", accountId);

            // Schedule notification sau 1 phút (không block request)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    _logger.LogInformation("Sending test notification to AccountId: {AccountId}", accountId);

                    var today = DateTime.UtcNow.Date;

                    // Generate plans
                    var mealPlan = await _nutritionService.GenerateDailyMealPlanAsync(
                        accountId, today, CancellationToken.None);
                    var workoutPlan = await _nutritionService.GenerateDailyWorkoutPlanAsync(
                        accountId, today, CancellationToken.None);

                    // Publish events (sẽ trigger email + web notification)
                    var mealPlanEvent = new DailyMealPlanGeneratedEvent
                    {
                        UserId = accountId,
                        MealPlanId = mealPlan.Id,
                        Date = today,
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
                        Date = today,
                        WorkoutType = workoutPlan.WorkoutType,
                        DurationMinutes = workoutPlan.DurationMinutes,
                        EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
                        ExerciseCount = workoutPlan.Exercises.Count,
                        GeneratedAt = DateTime.UtcNow
                    };
                    await _eventBus.PublishAsync(workoutPlanEvent);

                    _logger.LogInformation("Test notification sent successfully to AccountId: {AccountId}", accountId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending test notification to AccountId: {AccountId}", accountId);
                }
            });

            return Success<object?>(null, "Thông báo test sẽ được gửi sau 1 phút. Vui lòng kiểm tra email và thông báo web.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TestNotification");
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra" });
        }
    }
}

