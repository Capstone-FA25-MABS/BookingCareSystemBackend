using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.AI.BackgroundServices;

/// <summary>
/// Background service to generate daily meal and workout plans for all users
/// Runs daily at 6:00 AM
/// </summary>
public class DailyNutritionPlanService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyNutritionPlanService> _logger;
    private readonly TimeSpan _scheduledTime = new TimeSpan(6, 0, 0); // 6:00 AM

    public DailyNutritionPlanService(
        IServiceProvider serviceProvider,
        ILogger<DailyNutritionPlanService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Nutrition Plan Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var scheduledDateTime = now.Date.Add(_scheduledTime);

                // If we've passed today's scheduled time, schedule for tomorrow
                if (now > scheduledDateTime)
                {
                    scheduledDateTime = scheduledDateTime.AddDays(1);
                }

                var delay = scheduledDateTime - now;
                _logger.LogInformation(
                    "Next nutrition plan generation scheduled at {ScheduledTime} (in {Delay})",
                    scheduledDateTime, delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await GenerateDailyPlansForAllUsersAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Daily Nutrition Plan Service");
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task GenerateDailyPlansForAllUsersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting daily nutrition plan generation for all users");

        using var scope = _serviceProvider.CreateScope();
        var nutritionService = scope.ServiceProvider.GetRequiredService<INutritionService>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        try
        {
            // Get all active nutrition profiles
            var profiles = await nutritionService.GetAllActiveProfilesAsync(cancellationToken);
            _logger.LogInformation("Found {Count} active nutrition profiles", profiles.Count);

            var today = DateTime.UtcNow.Date;
            var successCount = 0;
            var errorCount = 0;

            foreach (var profile in profiles)
            {
                try
                {
                    // Generate meal plan
                    var mealPlan = await nutritionService.GenerateDailyMealPlanAsync(
                        profile.UserId, today, cancellationToken);

                    // Publish meal plan event
                    var mealPlanEvent = new DailyMealPlanGeneratedEvent
                    {
                        UserId = profile.UserId,
                        MealPlanId = mealPlan.Id,
                        Date = today,
                        TotalCalories = mealPlan.TotalCalories,
                        TotalProteinG = mealPlan.TotalProteinG,
                        TotalCarbsG = mealPlan.TotalCarbsG,
                        TotalFatG = mealPlan.TotalFatG,
                        MealCount = mealPlan.Meals.Count,
                        GeneratedAt = DateTime.UtcNow
                    };
                    eventBus.PublishAsync(mealPlanEvent);

                    // Mark notification as sent
                    await nutritionService.MarkMealPlanNotificationSentAsync(mealPlan.Id, cancellationToken);

                    // Generate workout plan
                    var workoutPlan = await nutritionService.GenerateDailyWorkoutPlanAsync(
                        profile.UserId, today, cancellationToken);

                    // Publish workout plan event
                    var workoutPlanEvent = new DailyWorkoutPlanGeneratedEvent
                    {
                        UserId = profile.UserId,
                        WorkoutPlanId = workoutPlan.Id,
                        Date = today,
                        WorkoutType = workoutPlan.WorkoutType,
                        DurationMinutes = workoutPlan.DurationMinutes,
                        EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
                        ExerciseCount = workoutPlan.Exercises.Count,
                        GeneratedAt = DateTime.UtcNow
                    };
                    eventBus.PublishAsync(workoutPlanEvent);

                    // Mark notification as sent
                    await nutritionService.MarkWorkoutPlanNotificationSentAsync(workoutPlan.Id, cancellationToken);

                    successCount++;
                    _logger.LogInformation(
                        "Successfully generated plans for UserId: {UserId}",
                        profile.UserId);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Error generating plans for UserId: {UserId}",
                        profile.UserId);
                }
            }

            _logger.LogInformation(
                "Daily nutrition plan generation completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateDailyPlansForAllUsersAsync");
            throw;
        }
    }
}
