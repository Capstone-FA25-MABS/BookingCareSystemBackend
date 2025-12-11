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
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var profile = await _nutritionService.CreateOrUpdateProfileAsync(
            userId, dto, cancellationToken);

        return Success(profile, "Nutrition profile created/updated successfully");
    }

    /// <summary>
    /// Get nutrition profile for current user
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var profile = await _nutritionService.GetProfileByUserIdAsync(userId, cancellationToken);

        if (profile == null)
        {
            return NotFound("Nutrition profile not found. Please create one first.");
        }

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
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var metrics = await _nutritionService.CalculateHealthMetricsAsync(userId, dto, cancellationToken);
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
            var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            
            _logger.LogInformation("Test notification requested by UserId: {UserId}", userId);
            
            // Schedule notification sau 1 phút (không block request)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    
                    _logger.LogInformation("Sending test notification to UserId: {UserId}", userId);
                    
                    var today = DateTime.UtcNow.Date;
                    
                    // Generate plans
                    var mealPlan = await _nutritionService.GenerateDailyMealPlanAsync(
                        userId, today, CancellationToken.None);
                    var workoutPlan = await _nutritionService.GenerateDailyWorkoutPlanAsync(
                        userId, today, CancellationToken.None);
                    
                    // Publish events (sẽ trigger email + web notification)
                    var mealPlanEvent = new DailyMealPlanGeneratedEvent
                    {
                        UserId = userId,
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
                        UserId = userId,
                        WorkoutPlanId = workoutPlan.Id,
                        Date = today,
                        WorkoutType = workoutPlan.WorkoutType,
                        DurationMinutes = workoutPlan.DurationMinutes,
                        EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
                        ExerciseCount = workoutPlan.Exercises.Count,
                        GeneratedAt = DateTime.UtcNow
                    };
                    await _eventBus.PublishAsync(workoutPlanEvent);
                    
                    _logger.LogInformation("Test notification sent successfully to UserId: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending test notification to UserId: {UserId}", userId);
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

