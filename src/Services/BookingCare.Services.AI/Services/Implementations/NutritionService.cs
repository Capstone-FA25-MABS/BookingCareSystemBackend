using System.Text.Json;
using System.Text.RegularExpressions;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.User.Protos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service implementation for nutrition recommendations
/// </summary>
public class NutritionService : INutritionService
{
    private readonly AiDbContext _context;
    private readonly HealthMetricsCalculator _healthMetricsCalculator;
    private readonly GroqApiHelper _groqApiHelper;
    private readonly ServiceGroqConfiguration _nutritionConfig;
    private readonly UserService.UserServiceClient _userServiceClient;
    private readonly ILogger<NutritionService> _logger;

    public NutritionService(
        AiDbContext context,
        HealthMetricsCalculator healthMetricsCalculator,
        GroqApiHelper groqApiHelper,
        IOptions<GroqServicesConfiguration> groqServicesConfig,
        UserService.UserServiceClient userServiceClient,
        ILogger<NutritionService> logger)
    {
        _context = context;
        _healthMetricsCalculator = healthMetricsCalculator;
        _groqApiHelper = groqApiHelper;
        _nutritionConfig = groqServicesConfig.Value.NutritionService;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<NutritionProfileDto> CreateOrUpdateProfileAsync(
        Guid userId,
        CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating/updating nutrition profile for UserId: {UserId}", userId);

        // Fetch user data from User service to get Age and Gender
        var userRequest = new GetUserByAccountIdRequest { AccountId = userId.ToString() };
        var userResponse = await _userServiceClient.GetUserByAccountIdAsync(userRequest, cancellationToken: cancellationToken);

        if (userResponse == null || string.IsNullOrEmpty(userResponse.Id))
        {
            throw new InvalidOperationException($"User not found for UserId: {userId}");
        }

        // Calculate age from date of birth
        var dateOfBirth = DateTime.Parse(userResponse.DateOfBirth);
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (DateTime.UtcNow < dateOfBirth.AddYears(age)) age--;

        var gender = userResponse.Gender;

        // Calculate health metrics
        var metrics = _healthMetricsCalculator.CalculateMetrics(
            dto.HeightCm,
            dto.WeightKg,
            age,
            gender,
            dto.ActivityLevel,
            dto.HealthGoal);

        // Check if profile exists
        var existingProfile = await _context.NutritionProfiles
            .FirstOrDefaultAsync(p => p.AccountId == userId, cancellationToken);

        NutritionProfileEntity profile;

        if (existingProfile != null)
        {
            // Update existing profile
            existingProfile.HeightCm = dto.HeightCm;
            existingProfile.WeightKg = dto.WeightKg;
            existingProfile.BMI = metrics.BMI;
            existingProfile.BMR = metrics.BMR;
            existingProfile.TDEE = metrics.TDEE;
            existingProfile.ActivityLevel = dto.ActivityLevel;
            existingProfile.HealthGoal = dto.HealthGoal;
            existingProfile.TargetCalories = metrics.TargetCalories;
            existingProfile.TargetProteinG = metrics.TargetProteinG;
            existingProfile.TargetCarbsG = metrics.TargetCarbsG;
            existingProfile.TargetFatG = metrics.TargetFatG;
            existingProfile.HealthConditionsJson = dto.HealthConditions != null
                ? JsonSerializer.Serialize(dto.HealthConditions)
                : null;
            existingProfile.DietaryPreferencesJson = dto.DietaryPreferences != null
                ? JsonSerializer.Serialize(dto.DietaryPreferences)
                : null;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            profile = existingProfile;
        }
        else
        {
            // Create new profile
            profile = new NutritionProfileEntity
            {
                AccountId = userId,
                HeightCm = dto.HeightCm,
                WeightKg = dto.WeightKg,
                BMI = metrics.BMI,
                BMR = metrics.BMR,
                TDEE = metrics.TDEE,
                ActivityLevel = dto.ActivityLevel,
                HealthGoal = dto.HealthGoal,
                TargetCalories = metrics.TargetCalories,
                TargetProteinG = metrics.TargetProteinG,
                TargetCarbsG = metrics.TargetCarbsG,
                TargetFatG = metrics.TargetFatG,
                HealthConditionsJson = dto.HealthConditions != null
                    ? JsonSerializer.Serialize(dto.HealthConditions)
                    : null,
                DietaryPreferencesJson = dto.DietaryPreferences != null
                    ? JsonSerializer.Serialize(dto.DietaryPreferences)
                    : null
            };

            await _context.NutritionProfiles.AddAsync(profile, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully saved nutrition profile for UserId: {UserId}", userId);

        return await MapToDtoAsync(profile, cancellationToken);
    }

    public async Task<NutritionProfileDto?> GetProfileByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.NutritionProfiles
            .FirstOrDefaultAsync(p => p.AccountId == userId, cancellationToken);

        return profile != null ? await MapToDtoAsync(profile, cancellationToken) : null;
    }

    public async Task<HealthMetricsDto> CalculateHealthMetricsAsync(
        Guid userId,
        CreateNutritionProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        // Fetch user data from User service to get Age and Gender
        var userRequest = new GetUserByAccountIdRequest { AccountId = userId.ToString() };
        var userResponse = await _userServiceClient.GetUserByAccountIdAsync(userRequest, cancellationToken: cancellationToken);

        if (userResponse == null || string.IsNullOrEmpty(userResponse.Id))
        {
            throw new InvalidOperationException($"User not found for UserId: {userId}");
        }

        // Calculate age from date of birth
        var dateOfBirth = DateTime.Parse(userResponse.DateOfBirth);
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (DateTime.UtcNow < dateOfBirth.AddYears(age)) age--;

        var gender = userResponse.Gender;

        return _healthMetricsCalculator.CalculateMetrics(
            dto.HeightCm,
            dto.WeightKg,
            age,
            gender,
            dto.ActivityLevel,
            dto.HealthGoal);
    }

    public async Task<MealPlanDto> GenerateDailyMealPlanAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating meal plan for UserId: {UserId}, Date: {Date}", userId, date);

        // Get user's nutrition profile
        var profile = await GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            throw new InvalidOperationException($"Nutrition profile not found for UserId: {userId}");
        }

        // Check if meal plan already exists for this date
        var existingPlan = await _context.MealPlans
            .FirstOrDefaultAsync(m => m.AccountId == userId && m.Date.Date == date.Date, cancellationToken);

        if (existingPlan != null)
        {
            _logger.LogInformation("Meal plan already exists for UserId: {UserId}, Date: {Date}", userId, date);
            return MapMealPlanToDto(existingPlan);
        }

        // Get meal plans from last 7 days to avoid duplicates
        var last7DaysPlans = await GetLast7DaysMealPlansAsync(userId, date, cancellationToken);
        var usedMealNames = ExtractMealNamesFromPlans(last7DaysPlans);

        // Generate meal plan using Groq AI with exclusion list
        var mealPlan = await GenerateMealPlanWithGroqAsync(
            profile,
            profile.HealthConditions,
            profile.DietaryPreferences,
            usedMealNames,
            cancellationToken);

        // Save to database
        var entity = new MealPlanEntity
        {
            AccountId = userId,
            NutritionProfileId = profile.Id,
            Date = date.Date,
            TotalCalories = mealPlan.TotalCalories,
            TotalProteinG = mealPlan.TotalProteinG,
            TotalCarbsG = mealPlan.TotalCarbsG,
            TotalFatG = mealPlan.TotalFatG,
            MealsJson = JsonSerializer.Serialize(mealPlan.Meals),
            GeneratedAt = DateTime.UtcNow,
            IsNotificationSent = false
        };

        await _context.MealPlans.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        mealPlan.Id = entity.Id;

        _logger.LogInformation("Successfully generated and saved meal plan for UserId: {UserId}", userId);

        return mealPlan;
    }

    public async Task<MealPlanDto?> GetMealPlanByDateAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.MealPlans
            .FirstOrDefaultAsync(m => m.AccountId == userId && m.Date.Date == date.Date, cancellationToken);

        return entity != null ? MapMealPlanToDto(entity) : null;
    }

    public async Task<WorkoutPlanDto> GenerateDailyWorkoutPlanAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating workout plan for UserId: {UserId}, Date: {Date}", userId, date);

        // Get user's nutrition profile
        var profile = await GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            throw new InvalidOperationException($"Nutrition profile not found for UserId: {userId}");
        }

        // Check if workout plan already exists for this date
        var existingPlan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(w => w.AccountId == userId && w.Date.Date == date.Date, cancellationToken);

        if (existingPlan != null)
        {
            _logger.LogInformation("Workout plan already exists for UserId: {UserId}, Date: {Date}", userId, date);
            return MapWorkoutPlanToDto(existingPlan);
        }

        // Get workout history to determine current week and day in program
        var workoutHistory = await GetWorkoutHistoryAsync(userId, date, cancellationToken);
        var currentWeek = CalculateCurrentWeek(workoutHistory, date);
        var dayOfWeek = (int)date.DayOfWeek; // 0=Sunday, 1=Monday, etc.

        // Generate workout plan using Groq AI with program context
        var workoutPlan = await GenerateWorkoutPlanWithGroqAsync(
            profile,
            profile.HealthConditions,
            currentWeek,
            dayOfWeek,
            cancellationToken);

        // Save to database
        var entity = new WorkoutPlanEntity
        {
            AccountId = userId,
            NutritionProfileId = profile.Id,
            Date = date.Date,
            WorkoutType = workoutPlan.WorkoutType,
            DurationMinutes = workoutPlan.DurationMinutes,
            EstimatedCaloriesBurned = workoutPlan.EstimatedCaloriesBurned,
            ExercisesJson = JsonSerializer.Serialize(workoutPlan.Exercises),
            GeneratedAt = DateTime.UtcNow,
            IsNotificationSent = false
        };

        await _context.WorkoutPlans.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        workoutPlan.Id = entity.Id;

        _logger.LogInformation("Successfully generated and saved workout plan for UserId: {UserId}", userId);

        return workoutPlan;
    }

    public async Task<WorkoutPlanDto?> GetWorkoutPlanByDateAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkoutPlans
            .FirstOrDefaultAsync(w => w.AccountId == userId && w.Date.Date == date.Date, cancellationToken);

        return entity != null ? MapWorkoutPlanToDto(entity) : null;
    }

    public async Task<List<NutritionProfileDto>> GetAllActiveProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = await _context.NutritionProfiles
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<NutritionProfileDto>();
        foreach (var profile in profiles)
        {
            result.Add(await MapToDtoAsync(profile, cancellationToken));
        }

        return result;
    }

    public async Task MarkMealPlanNotificationSentAsync(
        Guid mealPlanId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.MealPlans.FindAsync(new object[] { mealPlanId }, cancellationToken);
        if (plan != null)
        {
            plan.IsNotificationSent = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkWorkoutPlanNotificationSentAsync(
        Guid workoutPlanId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.WorkoutPlans.FindAsync(new object[] { workoutPlanId }, cancellationToken);
        if (plan != null)
        {
            plan.IsNotificationSent = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #region Groq AI Generation Methods

    private async Task<MealPlanDto> GenerateMealPlanWithGroqAsync(
        NutritionProfileDto profile,
        List<string>? healthConditions,
        DietaryPreferencesDto? preferences,
        HashSet<string> excludedMealNames,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildMealPlanPrompt(profile, healthConditions, preferences, excludedMealNames);

        try
        {
            _logger.LogInformation(
                "Generating meal plan for UserId: {UserId}, Goal: {Goal}",
                profile.AccountId, profile.HealthGoal);

            var response = await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _nutritionConfig,
                temperature: 0.7,
                maxTokens: 4000,
                cancellationToken);

            _logger.LogDebug("Groq API raw response length: {Length}", response.Length);

            // Extract JSON from response
            var jsonContent = ExtractJsonFromText(response);

            _logger.LogDebug("Extracted JSON content length: {Length}", jsonContent.Length);

            // Parse JSON response
            var mealPlan = JsonSerializer.Deserialize<MealPlanDto>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (mealPlan == null || mealPlan.Meals == null || mealPlan.Meals.Count == 0)
            {
                throw new InvalidOperationException("Failed to parse meal plan from Groq response");
            }

            // Set metadata
            mealPlan.AccountId = profile.AccountId;
            mealPlan.Date = DateTime.UtcNow.Date;
            mealPlan.GeneratedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Successfully generated meal plan with {MealCount} meals, {TotalCalories} kcal",
                mealPlan.Meals.Count, mealPlan.TotalCalories);

            return mealPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate meal plan for UserId: {UserId}", profile.AccountId);
            throw;
        }
    }

    private async Task<WorkoutPlanDto> GenerateWorkoutPlanWithGroqAsync(
        NutritionProfileDto profile,
        List<string>? healthConditions,
        int currentWeek,
        int dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildWorkoutPlanPrompt(profile, healthConditions, currentWeek, dayOfWeek);

        try
        {
            _logger.LogInformation(
                "Generating workout plan for UserId: {UserId}, Goal: {Goal}",
                profile.AccountId, profile.HealthGoal);

            var response = await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _nutritionConfig,
                temperature: 0.7,
                maxTokens: 2000,
                cancellationToken);

            // Extract JSON from response
            var jsonContent = ExtractJsonFromText(response);

            // Parse JSON response
            var workoutPlan = JsonSerializer.Deserialize<WorkoutPlanDto>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (workoutPlan == null || workoutPlan.Exercises == null || workoutPlan.Exercises.Count == 0)
            {
                throw new InvalidOperationException("Failed to parse workout plan from Groq response");
            }

            // Set metadata
            workoutPlan.AccountId = profile.AccountId;
            workoutPlan.Date = DateTime.UtcNow.Date;
            workoutPlan.GeneratedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Successfully generated workout plan with {ExerciseCount} exercises, {Duration} minutes",
                workoutPlan.Exercises.Count, workoutPlan.DurationMinutes);

            return workoutPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate workout plan for UserId: {UserId}", profile.AccountId);
            throw;
        }
    }

    #endregion

    #region Prompt Building Methods

    private string BuildMealPlanPrompt(
        NutritionProfileDto profile,
        List<string>? healthConditions,
        DietaryPreferencesDto? preferences,
        HashSet<string> excludedMealNames)
    {
        var healthConditionsStr = healthConditions != null && healthConditions.Count > 0
            ? string.Join(", ", healthConditions)
            : "Không có";

        var allergiesStr = preferences?.Allergies != null && preferences.Allergies.Count > 0
            ? string.Join(", ", preferences.Allergies)
            : "Không có";

        var dislikesStr = preferences?.Dislikes != null && preferences.Dislikes.Count > 0
            ? string.Join(", ", preferences.Dislikes)
            : "Không có";

        var cuisinesStr = preferences?.PreferredCuisines != null && preferences.PreferredCuisines.Count > 0
            ? string.Join(", ", preferences.PreferredCuisines)
            : "Món Việt Nam";

        var excludedMealsStr = excludedMealNames.Count > 0
            ? string.Join(", ", excludedMealNames)
            : "Không có";

        return $@"Bạn là chuyên gia dinh dưỡng Việt Nam. Tạo thực đơn NGẮN GỌN hàng ngày cho người dùng.

THÔNG TIN NGƯỜI DÙNG:
- Tuổi: {profile.Age}, Giới tính: {profile.Gender}
- Chiều cao: {profile.HeightCm}cm, Cân nặng: {profile.WeightKg}kg, BMI: {profile.BMI}
- Mục tiêu sức khỏe: {profile.HealthGoal}
- Mức độ vận động: {profile.ActivityLevel}

TÌNH TRẠNG SỨC KHỎE: {healthConditionsStr}

MÓN ĂN ĐÃ DÙNG TRONG 7 NGÀY QUA (TRÁNH TRÙNG): {excludedMealsStr}

SỞ THÍCH ĂN UỐNG:
- Chế độ ăn: {preferences?.DietType ?? "Không có"}
- Dị ứng: {allergiesStr}
- Không thích: {dislikesStr}
- Món ăn ưa thích: {cuisinesStr}

CHỈ TIÊU DINH DƯỠNG HÀNG NGÀY:
- Calories: {profile.TargetCalories} kcal
- Protein: {profile.TargetProteinG}g
- Carbs: {profile.TargetCarbsG}g
- Fat: {profile.TargetFatG}g

YÊU CẦU:
1. Tạo 3 bữa chính (Bữa sáng, Bữa trưa, Bữa tối) và 2 bữa phụ (Bữa phụ sáng, Bữa phụ chiều)
2. Sử dụng món ăn Việt Nam phổ biến và dễ chế biến
3. CHỈ cần tên món ăn, thời gian nấu và dinh dưỡng - KHÔNG cần mô tả, nguyên liệu, hướng dẫn
4. TRÁNH các món đã dùng trong 7 ngày qua
5. Tổng dinh dưỡng trong ngày phải khớp với chỉ tiêu (±50 kcal)
6. Xem xét tình trạng sức khỏe và dị ứng

⚠️ BẮT BUỘC: Trả về CHỈ JSON format NGẮN GỌN sau, KHÔNG có text khác:
{{
  ""totalCalories"": {profile.TargetCalories},
  ""totalProteinG"": {profile.TargetProteinG},
  ""totalCarbsG"": {profile.TargetCarbsG},
  ""totalFatG"": {profile.TargetFatG},
  ""meals"": [
    {{
      ""mealType"": ""Bữa sáng"",
      ""recipe"": {{
        ""nameVi"": ""Phở gà"",
        ""nameEn"": ""Chicken Pho"",
        ""prepTimeMinutes"": 15,
        ""cookTimeMinutes"": 30,
        ""servings"": 1,
        ""nutrition"": {{
          ""calories"": 450,
          ""proteinG"": 28,
          ""carbsG"": 65,
          ""fatG"": 8,
          ""fiberG"": 3
        }}
      }}
    }},
    {{
      ""mealType"": ""Bữa phụ sáng"",
      ""recipe"": {{
        ""nameVi"": ""Sữa chua Hy Lạp với hạt chia"",
        ""nameEn"": ""Greek Yogurt with Chia Seeds"",
        ""prepTimeMinutes"": 5,
        ""cookTimeMinutes"": 0,
        ""servings"": 1,
        ""nutrition"": {{
          ""calories"": 150,
          ""proteinG"": 12,
          ""carbsG"": 15,
          ""fatG"": 5,
          ""fiberG"": 4
        }}
      }}
    }}
  ]
}}";
    }

    private string BuildWorkoutPlanPrompt(
        NutritionProfileDto profile,
        List<string>? healthConditions,
        int currentWeek,
        int dayOfWeek)
    {
        var healthConditionsStr = healthConditions != null && healthConditions.Count > 0
            ? string.Join(", ", healthConditions)
            : "Không có";

        var bmiCategory = profile.BMI switch
        {
            < 18.5m => "Thiếu cân",
            >= 18.5m and < 25m => "Bình thường",
            >= 25m and < 30m => "Thừa cân",
            _ => "Béo phì"
        };

        var dayName = dayOfWeek switch
        {
            0 => "Chủ nhật (Nghỉ ngơi/Phục hồi)",
            1 => "Thứ 2 (Ngực + Vai)",
            2 => "Thứ 3 (Lưng + Tay sau)",
            3 => "Thứ 4 (Chân + Mông)",
            4 => "Thứ 5 (Vai + Tay trước)",
            5 => "Thứ 6 (Cardio + Core)",
            6 => "Thứ 7 (Full Body hoặc Yoga)",
            _ => "Không xác định"
        };

        return $@"Bạn là huấn luyện viên thể hình chuyên nghiệp. Tạo kế hoạch tập luyện THEO CHƯƠNG TRÌNH CÓ HỆ THỐNG.

THÔNG TIN NGƯỜI DÙNG:
- Tuổi: {profile.Age}, Giới tính: {profile.Gender}
- Chiều cao: {profile.HeightCm}cm, Cân nặng: {profile.WeightKg}kg
- BMI: {profile.BMI} ({bmiCategory})
- Mục tiêu: {profile.HealthGoal}
- Mức độ vận động: {profile.ActivityLevel}

TÌNH TRẠNG SỨC KHỎE: {healthConditionsStr}

CHƯƠNG TRÌNH TẬP LUYỆN:
- Tuần hiện tại: Tuần {currentWeek} (chu kỳ 4 tuần)
- Ngày trong tuần: {dayName}

YÊU CẦU:
1. Tạo kế hoạch tập THEO LỊCH TRÌNH CỐ ĐỊNH theo ngày trong tuần
2. Mỗi ngày tập nhóm cơ khác nhau (split training)
3. Chủ nhật là ngày nghỉ ngơi hoặc tập nhẹ (yoga, stretching)
4. Bài tập phù hợp tập tại nhà hoặc phòng gym
5. CHỈ cần tên bài tập, số sets, reps, thời gian - KHÔNG cần hướng dẫn chi tiết
6. Thời gian tập: 30-60 phút
7. Tăng cường độ theo tuần (tuần 1: nhẹ, tuần 4: nặng)

⚠️ BẮT BUỘC: Trả về CHỈ JSON format NGẮN GỌN sau, KHÔNG có text khác:
{{
  ""workoutType"": ""Cardio"",
  ""durationMinutes"": 30,
  ""estimatedCaloriesBurned"": 250,
  ""exercises"": [
    {{
      ""nameVi"": ""Hít đất"",
      ""nameEn"": ""Push-ups"",
      ""durationMinutes"": 5,
      ""sets"": 3,
      ""reps"": 15,
      ""intensity"": ""Medium"",
      ""caloriesBurned"": 30
    }},
    {{
      ""nameVi"": ""Nâng tạ vai"",
      ""nameEn"": ""Shoulder Press"",
      ""durationMinutes"": 8,
      ""sets"": 4,
      ""reps"": 12,
      ""intensity"": ""Medium"",
      ""caloriesBurned"": 50
    }}
  ]
}}";
    }

    #endregion

    #region Meal Plan Diversity Methods

    /// <summary>
    /// Get meal plans from last 7 days to avoid duplicates
    /// </summary>
    private async Task<List<MealPlanEntity>> GetLast7DaysMealPlansAsync(
        Guid userId,
        DateTime currentDate,
        CancellationToken cancellationToken)
    {
        var startDate = currentDate.AddDays(-7);

        return await _context.MealPlans
            .Where(m => m.AccountId == userId && m.Date >= startDate && m.Date < currentDate)
            .OrderByDescending(m => m.Date)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Extract meal names from plans to create exclusion list
    /// </summary>
    private HashSet<string> ExtractMealNamesFromPlans(List<MealPlanEntity> plans)
    {
        var mealNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plan in plans)
        {
            try
            {
                var meals = JsonSerializer.Deserialize<List<MealDto>>(plan.MealsJson);
                if (meals != null)
                {
                    foreach (var meal in meals)
                    {
                        if (!string.IsNullOrEmpty(meal.Recipe?.NameVi))
                        {
                            mealNames.Add(meal.Recipe.NameVi);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse meals from plan {PlanId}", plan.Id);
            }
        }

        return mealNames;
    }

    #endregion

    #region Workout Program Methods

    /// <summary>
    /// Get workout history to determine program progression
    /// </summary>
    private async Task<List<WorkoutPlanEntity>> GetWorkoutHistoryAsync(
        Guid userId,
        DateTime currentDate,
        CancellationToken cancellationToken)
    {
        // Get last 30 days of workout history
        var startDate = currentDate.AddDays(-30);

        return await _context.WorkoutPlans
            .Where(w => w.AccountId == userId && w.Date >= startDate && w.Date < currentDate)
            .OrderBy(w => w.Date)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Calculate current week in 4-week training cycle
    /// </summary>
    private int CalculateCurrentWeek(List<WorkoutPlanEntity> history, DateTime currentDate)
    {
        if (history.Count == 0)
        {
            // New user - start at week 1
            return 1;
        }

        // Get first workout date
        var firstWorkoutDate = history.First().Date;

        // Calculate weeks since first workout
        var daysSinceStart = (currentDate - firstWorkoutDate).Days;
        var weeksSinceStart = daysSinceStart / 7;

        // 4-week cycle (1, 2, 3, 4, 1, 2, 3, 4, ...)
        return (weeksSinceStart % 4) + 1;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extract JSON from text (handles cases where Groq adds extra text or thinking tags)
    /// </summary>
    private string ExtractJsonFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Remove <think> tags if present
        if (text.Contains("</think>"))
        {
            var thinkEndIndex = text.IndexOf("</think>");
            if (thinkEndIndex >= 0)
            {
                text = text.Substring(thinkEndIndex + 8).Trim();
            }
        }

        // Remove markdown code blocks if present
        if (text.TrimStart().StartsWith("```json"))
        {
            text = text.Substring(text.IndexOf("```json") + 7);
        }
        else if (text.TrimStart().StartsWith("```"))
        {
            text = text.Substring(text.IndexOf("```") + 3);
        }

        if (text.TrimEnd().EndsWith("```"))
        {
            text = text.Substring(0, text.LastIndexOf("```"));
        }

        text = text.Trim();

        // Try to find JSON object in the text
        int startIndex = text.IndexOf('{');
        if (startIndex < 0)
            return text;

        int endIndex = text.LastIndexOf('}');
        if (endIndex <= startIndex)
            return text;

        return text.Substring(startIndex, endIndex - startIndex + 1);
    }

    /// <summary>
    /// Simple cleanup: remove XML/HTML tags only
    /// </summary>
    private string CleanXmlTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Remove XML/HTML tags
        var cleaned = Regex.Replace(
            text,
            @"</?[^>]+>",
            "",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));

        return cleaned.Trim();
    }

    #endregion

    #region Private Mapping Methods

    private async Task<NutritionProfileDto> MapToDtoAsync(
        NutritionProfileEntity entity,
        CancellationToken cancellationToken = default)
    {
        // Fetch user data to get Age and Gender
        var userRequest = new GetUserByAccountIdRequest { AccountId = entity.AccountId.ToString() };
        var userResponse = await _userServiceClient.GetUserByAccountIdAsync(userRequest, cancellationToken: cancellationToken);

        int age = 0;
        string gender = "Unknown";

        if (userResponse != null && !string.IsNullOrEmpty(userResponse.DateOfBirth))
        {
            var dateOfBirth = DateTime.Parse(userResponse.DateOfBirth);
            age = DateTime.UtcNow.Year - dateOfBirth.Year;
            if (DateTime.UtcNow < dateOfBirth.AddYears(age)) age--;
            gender = userResponse.Gender ?? "Unknown";
        }

        return new NutritionProfileDto
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            Age = age,
            Gender = gender,
            HeightCm = entity.HeightCm,
            WeightKg = entity.WeightKg,
            BMI = entity.BMI,
            BMR = entity.BMR,
            TDEE = entity.TDEE,
            ActivityLevel = entity.ActivityLevel,
            HealthGoal = entity.HealthGoal,
            TargetCalories = entity.TargetCalories,
            TargetProteinG = entity.TargetProteinG,
            TargetCarbsG = entity.TargetCarbsG,
            TargetFatG = entity.TargetFatG,
            HealthConditions = !string.IsNullOrEmpty(entity.HealthConditionsJson)
                ? JsonSerializer.Deserialize<List<string>>(entity.HealthConditionsJson)
                : null,
            DietaryPreferences = !string.IsNullOrEmpty(entity.DietaryPreferencesJson)
                ? JsonSerializer.Deserialize<DietaryPreferencesDto>(entity.DietaryPreferencesJson)
                : null,
            StreakCount = entity.StreakCount,
            LastCompletedDate = entity.LastCompletedDate,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private MealPlanDto MapMealPlanToDto(MealPlanEntity entity)
    {
        return new MealPlanDto
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            Date = entity.Date,
            TotalCalories = entity.TotalCalories,
            TotalProteinG = entity.TotalProteinG,
            TotalCarbsG = entity.TotalCarbsG,
            TotalFatG = entity.TotalFatG,
            Meals = JsonSerializer.Deserialize<List<MealDto>>(entity.MealsJson) ?? new List<MealDto>(),
            CompletedItems = !string.IsNullOrEmpty(entity.CompletedItemsJson)
                ? JsonSerializer.Deserialize<List<int>>(entity.CompletedItemsJson)
                : new List<int>(),
            IsFullyCompleted = entity.IsFullyCompleted,
            GeneratedAt = entity.GeneratedAt
        };
    }

    private WorkoutPlanDto MapWorkoutPlanToDto(WorkoutPlanEntity entity)
    {
        return new WorkoutPlanDto
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            Date = entity.Date,
            WorkoutType = entity.WorkoutType,
            DurationMinutes = entity.DurationMinutes,
            EstimatedCaloriesBurned = entity.EstimatedCaloriesBurned,
            Exercises = JsonSerializer.Deserialize<List<ExerciseDto>>(entity.ExercisesJson) ?? new List<ExerciseDto>(),
            CompletedItems = !string.IsNullOrEmpty(entity.CompletedItemsJson)
                ? JsonSerializer.Deserialize<List<int>>(entity.CompletedItemsJson)
                : new List<int>(),
            IsFullyCompleted = entity.IsFullyCompleted,
            GeneratedAt = entity.GeneratedAt
        };
    }

    #endregion

    #region New Methods for Plan Completion and Progress

    public async Task<DailyPlanDto> GetDailyPlanAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var mealPlan = await GetMealPlanByDateAsync(userId, date, cancellationToken);
        var workoutPlan = await GetWorkoutPlanByDateAsync(userId, date, cancellationToken);

        var completionPercentage = CalculateCompletionPercentage(mealPlan, workoutPlan);

        return new DailyPlanDto
        {
            Date = date,
            MealPlan = mealPlan,
            WorkoutPlan = workoutPlan,
            CompletionPercentage = completionPercentage,
            TotalCaloriesConsumed = mealPlan?.TotalCalories ?? 0,
            TotalCaloriesBurned = workoutPlan?.EstimatedCaloriesBurned ?? 0
        };
    }

    public async Task<MealPlanDto> CompleteMealAsync(
        Guid mealPlanId,
        int mealIndex,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.MealPlans.FindAsync(new object[] { mealPlanId }, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Meal plan not found: {mealPlanId}");
        }

        // Parse completed items
        var completedItems = !string.IsNullOrEmpty(entity.CompletedItemsJson)
            ? JsonSerializer.Deserialize<List<int>>(entity.CompletedItemsJson) ?? new List<int>()
            : new List<int>();

        // Add meal index if not already completed
        if (!completedItems.Contains(mealIndex))
        {
            completedItems.Add(mealIndex);
            entity.CompletedItemsJson = JsonSerializer.Serialize(completedItems);

            // Check if all meals are completed
            var meals = JsonSerializer.Deserialize<List<MealDto>>(entity.MealsJson) ?? new List<MealDto>();
            entity.IsFullyCompleted = completedItems.Count >= meals.Count;

            await _context.SaveChangesAsync(cancellationToken);

            // Update streak if fully completed
            if (entity.IsFullyCompleted)
            {
                await UpdateStreakAsync(entity.AccountId, entity.Date, cancellationToken);
            }
        }

        return MapMealPlanToDto(entity);
    }

    public async Task<WorkoutPlanDto> CompleteExerciseAsync(
        Guid workoutPlanId,
        int exerciseIndex,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkoutPlans.FindAsync(new object[] { workoutPlanId }, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Workout plan not found: {workoutPlanId}");
        }

        // Parse completed items
        var completedItems = !string.IsNullOrEmpty(entity.CompletedItemsJson)
            ? JsonSerializer.Deserialize<List<int>>(entity.CompletedItemsJson) ?? new List<int>()
            : new List<int>();

        // Add exercise index if not already completed
        if (!completedItems.Contains(exerciseIndex))
        {
            completedItems.Add(exerciseIndex);
            entity.CompletedItemsJson = JsonSerializer.Serialize(completedItems);

            // Check if all exercises are completed
            var exercises = JsonSerializer.Deserialize<List<ExerciseDto>>(entity.ExercisesJson) ?? new List<ExerciseDto>();
            entity.IsFullyCompleted = completedItems.Count >= exercises.Count;

            await _context.SaveChangesAsync(cancellationToken);

            // Update streak if fully completed
            if (entity.IsFullyCompleted)
            {
                await UpdateStreakAsync(entity.AccountId, entity.Date, cancellationToken);
            }
        }

        return MapWorkoutPlanToDto(entity);
    }

    public async Task<ProgressStatsDto> GetProgressStatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.NutritionProfiles
            .FirstOrDefaultAsync(p => p.AccountId == userId, cancellationToken);

        if (profile == null)
        {
            throw new InvalidOperationException($"Nutrition profile not found for UserId: {userId}");
        }

        // Get last 7 days completion
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-6);

        var mealPlans = await _context.MealPlans
            .Where(m => m.AccountId == userId && m.Date >= weekAgo && m.Date <= today)
            .ToListAsync(cancellationToken);

        var workoutPlans = await _context.WorkoutPlans
            .Where(w => w.AccountId == userId && w.Date >= weekAgo && w.Date <= today)
            .ToListAsync(cancellationToken);

        var weeklyCompletion = new List<DailyCompletionDto>();
        var totalDaysCompleted = 0;
        var totalCompletionRate = 0m;

        for (var date = weekAgo; date <= today; date = date.AddDays(1))
        {
            var mealPlan = mealPlans.FirstOrDefault(m => m.Date.Date == date);
            var workoutPlan = workoutPlans.FirstOrDefault(w => w.Date.Date == date);

            var completionPercentage = CalculateCompletionPercentage(
                mealPlan != null ? MapMealPlanToDto(mealPlan) : null,
                workoutPlan != null ? MapWorkoutPlanToDto(workoutPlan) : null);

            var isFullyCompleted = completionPercentage >= 80; // 80% threshold

            weeklyCompletion.Add(new DailyCompletionDto
            {
                Date = date,
                CompletionPercentage = completionPercentage,
                IsFullyCompleted = isFullyCompleted
            });

            if (isFullyCompleted)
            {
                totalDaysCompleted++;
            }

            totalCompletionRate += completionPercentage;
        }

        return new ProgressStatsDto
        {
            StreakCount = profile.StreakCount,
            LastCompletedDate = profile.LastCompletedDate,
            WeeklyCompletion = weeklyCompletion,
            AverageCompletionRate = weeklyCompletion.Count > 0 ? totalCompletionRate / weeklyCompletion.Count : 0,
            TotalDaysCompleted = totalDaysCompleted
        };
    }

    private async Task UpdateStreakAsync(Guid userId, DateTime completedDate, CancellationToken cancellationToken)
    {
        var profile = await _context.NutritionProfiles
            .FirstOrDefaultAsync(p => p.AccountId == userId, cancellationToken);

        if (profile == null) return;

        // Check if both meal and workout are completed for this date
        var mealPlan = await _context.MealPlans
            .FirstOrDefaultAsync(m => m.AccountId == userId && m.Date.Date == completedDate.Date, cancellationToken);
        var workoutPlan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(w => w.AccountId == userId && w.Date.Date == completedDate.Date, cancellationToken);

        if (mealPlan?.IsFullyCompleted == true && workoutPlan?.IsFullyCompleted == true)
        {
            // Both completed - update streak
            if (profile.LastCompletedDate.HasValue)
            {
                var daysDiff = (completedDate.Date - profile.LastCompletedDate.Value.Date).Days;
                if (daysDiff == 1)
                {
                    // Consecutive day - increment streak
                    profile.StreakCount++;
                }
                else if (daysDiff > 1)
                {
                    // Streak broken - reset to 1
                    profile.StreakCount = 1;
                }
                // If daysDiff == 0, same day - don't change streak
            }
            else
            {
                // First completion
                profile.StreakCount = 1;
            }

            profile.LastCompletedDate = completedDate.Date;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private decimal CalculateCompletionPercentage(MealPlanDto? mealPlan, WorkoutPlanDto? workoutPlan)
    {
        if (mealPlan == null && workoutPlan == null)
        {
            return 0;
        }

        var totalItems = 0;
        var completedItems = 0;

        if (mealPlan != null)
        {
            totalItems += mealPlan.Meals.Count;
            completedItems += mealPlan.CompletedItems?.Count ?? 0;
        }

        if (workoutPlan != null)
        {
            totalItems += workoutPlan.Exercises.Count;
            completedItems += workoutPlan.CompletedItems?.Count ?? 0;
        }

        return totalItems > 0 ? (decimal)completedItems / totalItems * 100 : 0;
    }

    #endregion
}
