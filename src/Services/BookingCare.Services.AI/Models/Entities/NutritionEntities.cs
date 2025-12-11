using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.AI.Models.Entities;

/// <summary>
/// Nutrition profile entity for storing user's health and nutrition information
/// </summary>
public class NutritionProfileEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [Range(50, 300)]
    public decimal HeightCm { get; set; }

    [Required]
    [Range(20, 500)]
    public decimal WeightKg { get; set; }

    [Range(10, 100)]
    public decimal BMI { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActivityLevel { get; set; } = string.Empty; // "Sedentary", "LightlyActive", "ModeratelyActive", "VeryActive", "ExtraActive"

    [Required]
    [MaxLength(50)]
    public string HealthGoal { get; set; } = string.Empty; // "WeightLoss", "MuscleGain", "Maintenance"

    public int TargetCalories { get; set; }
    public decimal TargetProteinG { get; set; }
    public decimal TargetCarbsG { get; set; }
    public decimal TargetFatG { get; set; }

    /// <summary>
    /// JSON array of health conditions (e.g., ["Diabetes", "Hypertension"])
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? HealthConditionsJson { get; set; }

    /// <summary>
    /// JSON object for dietary preferences
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? DietaryPreferencesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<MealPlanEntity> MealPlans { get; set; } = new List<MealPlanEntity>();
    public virtual ICollection<WorkoutPlanEntity> WorkoutPlans { get; set; } = new List<WorkoutPlanEntity>();
}

/// <summary>
/// Meal plan entity for daily meal recommendations
/// </summary>
public class MealPlanEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid NutritionProfileId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public int TotalCalories { get; set; }
    public decimal TotalProteinG { get; set; }
    public decimal TotalCarbsG { get; set; }
    public decimal TotalFatG { get; set; }

    /// <summary>
    /// JSON array of meals with recipes and nutrition info
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string MealsJson { get; set; } = "[]";

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsNotificationSent { get; set; } = false;

    // Navigation property
    [ForeignKey(nameof(NutritionProfileId))]
    public virtual NutritionProfileEntity NutritionProfile { get; set; } = null!;
}

/// <summary>
/// Workout plan entity for daily exercise recommendations
/// </summary>
public class WorkoutPlanEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid NutritionProfileId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(100)]
    public string WorkoutType { get; set; } = string.Empty; // "Cardio", "Strength", "Flexibility", "Mixed"

    public int DurationMinutes { get; set; }
    public int EstimatedCaloriesBurned { get; set; }

    /// <summary>
    /// JSON array of exercises with instructions
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ExercisesJson { get; set; } = "[]";

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsNotificationSent { get; set; } = false;

    // Navigation property
    [ForeignKey(nameof(NutritionProfileId))]
    public virtual NutritionProfileEntity NutritionProfile { get; set; } = null!;
}
