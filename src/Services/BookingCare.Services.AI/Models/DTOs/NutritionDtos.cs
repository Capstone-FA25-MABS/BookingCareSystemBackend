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
    public Guid UserId { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public decimal BMI { get; set; }
    public string ActivityLevel { get; set; } = string.Empty;
    public string HealthGoal { get; set; } = string.Empty;
    public int TargetCalories { get; set; }
    public decimal TargetProteinG { get; set; }
    public decimal TargetCarbsG { get; set; }
    public decimal TargetFatG { get; set; }
    public List<string>? HealthConditions { get; set; }
    public DietaryPreferencesDto? DietaryPreferences { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for meal plan response
/// </summary>
public class MealPlanDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public int TotalCalories { get; set; }
    public decimal TotalProteinG { get; set; }
    public decimal TotalCarbsG { get; set; }
    public decimal TotalFatG { get; set; }
    public List<MealDto> Meals { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for individual meal
/// </summary>
public class MealDto
{
    public string MealType { get; set; } = string.Empty; // "Breakfast", "Lunch", "Dinner", "Snack"
    public RecipeDto Recipe { get; set; } = new();
}

/// <summary>
/// DTO for recipe - SIMPLIFIED (removed unnecessary details)
/// </summary>
public class RecipeDto
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    // Removed: DescriptionVi, DescriptionEn, Ingredients, InstructionsVi, InstructionsEn
    // Keep only essential info
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public NutritionInfoDto Nutrition { get; set; } = new();
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
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public string WorkoutType { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int EstimatedCaloriesBurned { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for exercise - SIMPLIFIED
/// </summary>
public class ExerciseDto
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Sets { get; set; } // Number of sets
    public int Reps { get; set; } // Repetitions per set
    public string? Intensity { get; set; } // "Low", "Medium", "High"
    public int? CaloriesBurned { get; set; }
    // Removed: InstructionsVi, InstructionsEn - too detailed
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
