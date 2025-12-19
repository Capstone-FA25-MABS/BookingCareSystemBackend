using System.Text.Json;
using System.Text.RegularExpressions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.Extensions.Caching.Memory;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing nutrition conversation flow
/// </summary>
public class NutritionConversationService : INutritionConversationService
{
    private readonly IMemoryCache _cache;
    private readonly INutritionService _nutritionService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<NutritionConversationService> _logger;
    private const int TotalSteps = 6;
    private const int CacheExpirationMinutes = 30;

    public NutritionConversationService(
        IMemoryCache cache,
        INutritionService nutritionService,
        IEventBus eventBus,
        ILogger<NutritionConversationService> logger)
    {
        _cache = cache;
        _nutritionService = nutritionService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<NutritionConversationResponse> StartConversationAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting nutrition conversation for UserId: {UserId}, SessionId: {SessionId}",
            userId, sessionId);

        // Initialize conversation state
        var state = new NutritionConversationState
        {
            CurrentStep = 1
        };

        // Save state to cache
        var cacheKey = GetCacheKey(sessionId);
        _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return new NutritionConversationResponse
        {
            SessionId = sessionId,
            Question = GetQuestionForStep(1),
            CurrentStep = 1,
            TotalSteps = TotalSteps,
            IsComplete = false
        };
    }

    public async Task<NutritionConversationResponse> ProcessAnswerAsync(
        Guid userId,
        string sessionId,
        string answer,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing answer for UserId: {UserId}, SessionId: {SessionId}, Answer: {Answer}",
            userId, sessionId, answer);

        // Get conversation state from cache
        var cacheKey = GetCacheKey(sessionId);
        if (!_cache.TryGetValue<NutritionConversationState>(cacheKey, out var state) || state == null)
        {
            throw new InvalidOperationException("Conversation session not found or expired. Please start a new conversation.");
        }

        // Parse and validate answer
        var (isValid, errorMessage) = await ParseAndValidateAnswerAsync(state.CurrentStep, answer, state);

        if (!isValid)
        {
            // Return same question with error message
            return new NutritionConversationResponse
            {
                SessionId = sessionId,
                Question = $"{errorMessage} {GetQuestionForStep(state.CurrentStep)}",
                CurrentStep = state.CurrentStep,
                TotalSteps = TotalSteps,
                IsComplete = false
            };
        }

        // Move to next step
        state.CurrentStep++;

        // Check if conversation is complete
        if (state.CurrentStep > TotalSteps)
        {
            // Create nutrition profile
            var profile = await CreateNutritionProfileAsync(userId, state, cancellationToken);

            // Generate meal and workout plans
            var today = DateTime.UtcNow.Date;
            var mealPlan = await _nutritionService.GenerateDailyMealPlanAsync(userId, today, cancellationToken);
            var workoutPlan = await _nutritionService.GenerateDailyWorkoutPlanAsync(userId, today, cancellationToken);

            // Publish notification events
            PublishNotificationEvents(userId, mealPlan, workoutPlan);

            // Clear cache
            _cache.Remove(cacheKey);

            _logger.LogInformation("Nutrition conversation completed for UserId: {UserId}", userId);

            return new NutritionConversationResponse
            {
                SessionId = sessionId,
                Question = string.Empty,
                CurrentStep = TotalSteps,
                TotalSteps = TotalSteps,
                IsComplete = true,
                Profile = profile,
                MealPlan = mealPlan,
                WorkoutPlan = workoutPlan
            };
        }

        // Save updated state
        _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return new NutritionConversationResponse
        {
            SessionId = sessionId,
            Question = GetQuestionForStep(state.CurrentStep),
            CurrentStep = state.CurrentStep,
            TotalSteps = TotalSteps,
            IsComplete = false
        };
    }

    private string GetCacheKey(string sessionId) => $"nutrition_conversation:{sessionId}";

    private string GetQuestionForStep(int step)
    {
        return step switch
        {
            1 => "Xin chào! Tôi sẽ giúp bạn tạo kế hoạch dinh dưỡng cá nhân. Chiều cao của bạn là bao nhiêu? (cm)",
            2 => "Cảm ơn! Cân nặng hiện tại của bạn là bao nhiêu? (kg)",
            3 => "Mức độ hoạt động của bạn? Bạn có thể chọn: Ít vận động (ngồi nhiều), Nhẹ (tập 1-3 ngày/tuần), Vừa phải (tập 3-5 ngày/tuần), Nhiều (tập 6-7 ngày/tuần), hoặc Rất nhiều (vận động viên)",
            4 => "Mục tiêu sức khỏe của bạn? Bạn có thể chọn: Giảm cân, Tăng cơ, hoặc Duy trì cân nặng",
            5 => "Bạn có tình trạng sức khỏe đặc biệt nào không? (ví dụ: tiểu đường, huyết áp cao, dị ứng thực phẩm...) Nếu không có, vui lòng nhập 'Không'",
            6 => "Bạn có chế độ ăn đặc biệt không? (ví dụ: chay, keto, ăn chay trứng sữa, low-carb...) Nếu không có, vui lòng nhập 'Không'",
            _ => throw new ArgumentException($"Invalid step: {step}")
        };
    }

    private async Task<(bool isValid, string errorMessage)> ParseAndValidateAnswerAsync(
        int step,
        string answer,
        NutritionConversationState state)
    {
        var trimmedAnswer = answer.Trim();

        try
        {
            switch (step)
            {
                case 1: // Height
                    if (!decimal.TryParse(trimmedAnswer, out var height) || height < 100 || height > 250)
                    {
                        return (false, "Xin lỗi, chiều cao bạn nhập chưa hợp lệ. Vui lòng nhập số từ 100-250 cm.");
                    }
                    state.HeightCm = height;
                    return (true, string.Empty);

                case 2: // Weight
                    if (!decimal.TryParse(trimmedAnswer, out var weight) || weight < 30 || weight > 300)
                    {
                        return (false, "Xin lỗi, cân nặng bạn nhập chưa hợp lệ. Vui lòng nhập số từ 30-300 kg.");
                    }
                    state.WeightKg = weight;
                    return (true, string.Empty);

                case 3: // Activity Level
                    var activityLevel = ParseActivityLevel(trimmedAnswer);
                    if (activityLevel == null)
                    {
                        return (false, "Xin lỗi, tôi chưa hiểu mức độ hoạt động của bạn. Bạn có thể chọn: Ít vận động, Nhẹ, Vừa phải, Nhiều, hoặc Rất nhiều.");
                    }
                    state.ActivityLevel = activityLevel;
                    return (true, string.Empty);

                case 4: // Health Goal
                    var healthGoal = ParseHealthGoal(trimmedAnswer);
                    if (healthGoal == null)
                    {
                        return (false, "Xin lỗi, tôi chưa hiểu mục tiêu của bạn. Bạn có thể chọn: Giảm cân, Tăng cơ, hoặc Duy trì cân nặng.");
                    }
                    state.HealthGoal = healthGoal;
                    return (true, string.Empty);

                case 5: // Health Conditions
                    state.HealthConditions = ParseHealthConditions(trimmedAnswer);
                    return (true, string.Empty);

                case 6: // Dietary Preferences
                    state.DietaryPreferences = ParseDietaryPreferences(trimmedAnswer);
                    return (true, string.Empty);

                default:
                    return (false, "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing answer for step {Step}", step);
            return (false, "Xin lỗi, có lỗi xảy ra khi xử lý câu trả lời của bạn. Vui lòng thử lại.");
        }
    }

    private string? ParseActivityLevel(string answer)
    {
        var trimmed = answer.Trim().ToLower();

        // Support both number and text input
        return trimmed switch
        {
            "1" or "ít vận động" or "it van dong" or "sedentary" or "ngồi nhiều" or "ngoi nhieu" => "Sedentary",
            "2" or "nhẹ" or "nhe" or "lightly active" or "lightly" or "tập 1-3" or "tap 1-3" => "LightlyActive",
            "3" or "vừa phải" or "vua phai" or "moderately active" or "moderate" or "tập 3-5" or "tap 3-5" => "ModeratelyActive",
            "4" or "nhiều" or "nhieu" or "very active" or "very" or "tập 6-7" or "tap 6-7" => "VeryActive",
            "5" or "rất nhiều" or "rat nhieu" or "extra active" or "extra" or "vận động viên" or "van dong vien" => "ExtraActive",
            _ => null
        };
    }

    private string? ParseHealthGoal(string answer)
    {
        var trimmed = answer.Trim().ToLower();

        // Support both number and text input
        return trimmed switch
        {
            "1" or "giảm cân" or "giam can" or "weight loss" or "lose weight" or "giảm" or "giam" => "WeightLoss",
            "2" or "tăng cơ" or "tang co" or "muscle gain" or "gain muscle" or "tăng" or "tang" => "MuscleGain",
            "3" or "duy trì" or "duy tri" or "maintenance" or "maintain" or "giữ" or "giu" => "Maintenance",
            _ => null
        };
    }

    private List<string> ParseHealthConditions(string answer)
    {
        var trimmed = answer.Trim().ToLower();
        if (trimmed == "không" || trimmed == "khong" || string.IsNullOrWhiteSpace(trimmed))
        {
            return new List<string>();
        }

        return answer.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private DietaryPreferencesDto ParseDietaryPreferences(string answer)
    {
        var trimmed = answer.Trim().ToLower();
        var preferences = new DietaryPreferencesDto
        {
            DietType = null,
            Allergies = new List<string>()
        };

        if (trimmed == "không" || trimmed == "khong" || string.IsNullOrWhiteSpace(trimmed))
        {
            return preferences;
        }

        // Parse dietary preferences from answer and set DietType
        if (Regex.IsMatch(trimmed, @"\bvegan\b"))
        {
            preferences.DietType = "Vegan";
        }
        else if (Regex.IsMatch(trimmed, @"\bchay\b"))
        {
            preferences.DietType = "Vegetarian";
        }
        else if (Regex.IsMatch(trimmed, @"\bketo\b"))
        {
            preferences.DietType = "Keto";
        }
        else if (Regex.IsMatch(trimmed, @"\blow.?carb\b"))
        {
            preferences.DietType = "LowCarb";
        }
        else if (Regex.IsMatch(trimmed, @"\bgluten.?free\b"))
        {
            preferences.DietType = "GlutenFree";
        }
        else if (Regex.IsMatch(trimmed, @"\bdairy.?free\b"))
        {
            preferences.DietType = "DairyFree";
        }

        return preferences;
    }

    private async Task<NutritionProfileDto> CreateNutritionProfileAsync(
        Guid userId,
        NutritionConversationState state,
        CancellationToken cancellationToken)
    {
        var createDto = new CreateNutritionProfileDto
        {
            HeightCm = state.HeightCm!.Value,
            WeightKg = state.WeightKg!.Value,
            ActivityLevel = state.ActivityLevel!,
            HealthGoal = state.HealthGoal!,
            HealthConditions = state.HealthConditions,
            DietaryPreferences = state.DietaryPreferences
        };

        return await _nutritionService.CreateOrUpdateProfileAsync(userId, createDto, cancellationToken);
    }

    private void PublishNotificationEvents(Guid userId, MealPlanDto mealPlan, WorkoutPlanDto workoutPlan)
    {
        // Publish meal plan event
        var mealPlanEvent = new DailyMealPlanGeneratedEvent
        {
            UserId = userId,
            MealPlanId = mealPlan.Id,
            Date = mealPlan.Date,
            TotalCalories = mealPlan.TotalCalories,
            TotalProteinG = mealPlan.TotalProteinG,
            TotalCarbsG = mealPlan.TotalCarbsG,
            TotalFatG = mealPlan.TotalFatG,
            MealCount = mealPlan.Meals.Count,
            GeneratedAt = DateTime.UtcNow
        };
        _eventBus.PublishAsync(mealPlanEvent);

        // Publish workout plan event
        var workoutPlanEvent = new DailyWorkoutPlanGeneratedEvent
        {
            UserId = userId,
            WorkoutPlanId = workoutPlan.Id,
            Date = workoutPlan.Date,
            WorkoutType = workoutPlan.WorkoutType,
            DurationMinutes = workoutPlan.DurationMinutes,
            EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
            ExerciseCount = workoutPlan.Exercises.Count,
            GeneratedAt = DateTime.UtcNow
        };
        _eventBus.PublishAsync(workoutPlanEvent);

        _logger.LogInformation("Published nutrition notification events for UserId: {UserId}", userId);
    }
}
