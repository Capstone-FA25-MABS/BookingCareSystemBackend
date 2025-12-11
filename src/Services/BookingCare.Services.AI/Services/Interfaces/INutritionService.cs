using BookingCare.Services.AI.Models.DTOs;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service interface for nutrition recommendations
/// </summary>
public interface INutritionService
{
    /// <summary>
    /// Create or update nutrition profile for a user
    /// </summary>
    Task<NutritionProfileDto> CreateOrUpdateProfileAsync(
        Guid userId,
        CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get nutrition profile by user ID
    /// </summary>
    Task<NutritionProfileDto?> GetProfileByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate health metrics
    /// </summary>
    Task<HealthMetricsDto> CalculateHealthMetricsAsync(
        Guid userId,
        CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate daily meal plan for a user
    /// </summary>
    Task<MealPlanDto> GenerateDailyMealPlanAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get meal plan by date
    /// </summary>
    Task<MealPlanDto?> GetMealPlanByDateAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate daily workout plan for a user
    /// </summary>
    Task<WorkoutPlanDto> GenerateDailyWorkoutPlanAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workout plan by date
    /// </summary>
    Task<WorkoutPlanDto?> GetWorkoutPlanByDateAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active nutrition profiles (for background service)
    /// </summary>
    Task<List<NutritionProfileDto>> GetAllActiveProfilesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark meal plan notification as sent
    /// </summary>
    Task MarkMealPlanNotificationSentAsync(
        Guid mealPlanId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark workout plan notification as sent
    /// </summary>
    Task MarkWorkoutPlanNotificationSentAsync(
        Guid workoutPlanId,
        CancellationToken cancellationToken = default);
}
