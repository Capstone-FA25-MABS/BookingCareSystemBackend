namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// DTO for creating or updating nutrition profile
/// </summary>
public class CreateNutritionProfileDto
{
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public string ActivityLevel { get; set; } = string.Empty;
    public string HealthGoal { get; set; } = string.Empty;
    public List<string>? HealthConditions { get; set; }
    public DietaryPreferencesDto? DietaryPreferences { get; set; }
}

/// <summary>
/// DTO for dietary preferences
/// </summary>
public class DietaryPreferencesDto
{
    public string? DietType { get; set; } // "Vegetarian", "Vegan", "Keto", "Paleo", etc.
    public List<string>? Allergies { get; set; }
    public List<string>? Dislikes { get; set; }
    public List<string>? PreferredCuisines { get; set; }
}

/// <summary>
/// DTO for nutrition profile response
/// </summary>
public class NutritionProfileDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public decimal BMI { get; set; }
    public decimal BMR { get; set; }
    public decimal TDEE { get; set; }
    public string ActivityLevel { get; set; } = string.Empty;
    public string HealthGoal { get; set; } = string.Empty;
    public int TargetCalories { get; set; }
    public decimal TargetProteinG { get; set; }
    public decimal TargetCarbsG { get; set; }
    public decimal TargetFatG { get; set; }
    public List<string>? HealthConditions { get; set; }
    public DietaryPreferencesDto? DietaryPreferences { get; set; }
    public int StreakCount { get; set; }
    public DateTime? LastCompletedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for meal plan response
/// </summary>
public class MealPlanDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime Date { get; set; }
    public int TotalCalories { get; set; }
    public decimal TotalProteinG { get; set; }
    public decimal TotalCarbsG { get; set; }
    public decimal TotalFatG { get; set; }
    public List<MealDto> Meals { get; set; } = new();
    public List<int>? CompletedItems { get; set; } // Indices of completed meals
    public bool IsFullyCompleted { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for individual meal
/// </summary>
public class MealDto
{
    public string MealType { get; set; } = string.Empty; // "Breakfast", "Lunch", "Dinner", "Snack"
    public string MealTime { get; set; } = string.Empty; // "08:00", "12:30", "19:00", etc.
    public RecipeDto Recipe { get; set; } = new();
}

/// <summary>
/// DTO for recipe - Enhanced with UI display info
/// </summary>
public class RecipeDto
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionVi { get; set; } // Short description for UI
    public string? DescriptionEn { get; set; }
    public string? ImageUrl { get; set; } // Image URL for meal display
    public string? BenefitsVi { get; set; } // Health benefits in Vietnamese
    public string? BenefitsEn { get; set; } // Health benefits in English
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public NutritionInfoDto Nutrition { get; set; } = new();
    public List<string>? MainIngredients { get; set; } // Top 3-5 main ingredients for display
}

// REMOVED: IngredientDto - not needed anymore

/// <summary>
/// DTO for nutrition information
/// </summary>
public class NutritionInfoDto
{
    public int Calories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public decimal? FiberG { get; set; }
}

/// <summary>
/// DTO for workout plan response
/// </summary>
public class WorkoutPlanDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime Date { get; set; }
    public string WorkoutType { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int EstimatedCaloriesBurned { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
    public List<int>? CompletedItems { get; set; } // Indices of completed exercises
    public bool IsFullyCompleted { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for exercise - Enhanced with UI display info
/// </summary>
public class ExerciseDto
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionVi { get; set; } // Short description about the exercise
    public string? DescriptionEn { get; set; }
    public string? ImageUrl { get; set; } // Image/GIF URL for exercise demonstration
    public string? VideoUrl { get; set; } // Optional video tutorial URL
    public int DurationMinutes { get; set; }
    public int Sets { get; set; } // Number of sets
    public int Reps { get; set; } // Repetitions per set
    public string? Intensity { get; set; } // "Low", "Medium", "High"
    public int? CaloriesBurned { get; set; }
    public string? TargetMuscles { get; set; } // e.g., "Chest, Triceps, Shoulders"
    public List<string>? Instructions { get; set; } // Step-by-step instructions
}

/// <summary>
/// DTO for health metrics calculation
/// </summary>
public class HealthMetricsDto
{
    public decimal BMI { get; set; }
    public decimal BMR { get; set; } // Basal Metabolic Rate
    public decimal TDEE { get; set; } // Total Daily Energy Expenditure
    public int TargetCalories { get; set; }
    public decimal TargetProteinG { get; set; }
    public decimal TargetCarbsG { get; set; }
    public decimal TargetFatG { get; set; }
    public string BMICategory { get; set; } = string.Empty; // "Underweight", "Normal", "Overweight", "Obese"
}

/// <summary>
/// DTO for daily plan (meal + workout combined)
/// </summary>
public class DailyPlanDto
{
    public DateTime Date { get; set; }
    public MealPlanDto? MealPlan { get; set; }
    public WorkoutPlanDto? WorkoutPlan { get; set; }
    public HydrationPlanDto? HydrationPlan { get; set; }
    public decimal CompletionPercentage { get; set; }
    public int TotalCaloriesConsumed { get; set; }
    public int TotalCaloriesBurned { get; set; }
}

/// <summary>
/// DTO for hydration plan
/// </summary>
public class HydrationPlanDto
{
    public decimal TargetWaterLiters { get; set; } // e.g., 2.5L
    public int RecommendedGlasses { get; set; } // e.g., 8 glasses
    public decimal GlassSizeMl { get; set; } // e.g., 250ml per glass
    public List<HydrationReminderDto> Reminders { get; set; } = new();
    public decimal CurrentIntakeLiters { get; set; } // Tracked intake
    public int CompletedGlasses { get; set; }
}

/// <summary>
/// DTO for hydration reminder
/// </summary>
public class HydrationReminderDto
{
    public string Time { get; set; } = string.Empty; // "08:00", "10:00", etc.
    public string Message { get; set; } = string.Empty; // "Uống nước sau khi thức dậy"
    public decimal AmountMl { get; set; } // Amount to drink
}

/// <summary>
/// DTO for progress statistics
/// </summary>
public class ProgressStatsDto
{
    public int StreakCount { get; set; }
    public DateTime? LastCompletedDate { get; set; }
    public List<DailyCompletionDto> WeeklyCompletion { get; set; } = new();
    public decimal AverageCompletionRate { get; set; }
    public int TotalDaysCompleted { get; set; }
}

/// <summary>
/// DTO for daily completion
/// </summary>
public class DailyCompletionDto
{
    public DateTime Date { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool IsFullyCompleted { get; set; }
}

/// <summary>
/// DTO for marking item as completed
/// </summary>
public class CompleteItemDto
{
    public int ItemIndex { get; set; }
}
