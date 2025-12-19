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

    /// <summary>
    /// Account ID (foreign key to Accounts table)
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(50, 300)]
    public decimal HeightCm { get; set; }

    [Required]
    [Range(20, 500)]
    public decimal WeightKg { get; set; }

    [Range(10, 100)]
    public decimal BMI { get; set; }

    public decimal BMR { get; set; } // Basal Metabolic Rate

    public decimal TDEE { get; set; } // Total Daily Energy Expenditure

    [Required]
    [MaxLength(50)]
    public string ActivityLevel { get; set; } = string.Empty; // "Sedentary", "Light", "Moderate", "Active", "VeryActive"

    [Required]
    [MaxLength(50)]
    public string HealthGoal { get; set; } = string.Empty; // "WeightLoss", "MuscleGain", "Maintenance", "HeartHealth"

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

    /// <summary>
    /// Streak count - số ngày hoàn thành liên tiếp
    /// </summary>
    public int StreakCount { get; set; } = 0;

    /// <summary>
    /// Last completed date - ngày hoàn thành gần nhất
    /// </summary>
    public DateTime? LastCompletedDate { get; set; }

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

    /// <summary>
    /// Account ID (foreign key to Accounts table)
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

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

    /// <summary>
    /// JSON array of completed meal indices (e.g., [0, 2] means meal 0 and 2 are completed)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CompletedItemsJson { get; set; }

    /// <summary>
    /// Is fully completed (all meals eaten)
    /// </summary>
    public bool IsFullyCompleted { get; set; } = false;

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

    /// <summary>
    /// Account ID (foreign key to Accounts table)
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

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

    /// <summary>
    /// JSON array of completed exercise indices (e.g., [0, 1] means exercise 0 and 1 are completed)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CompletedItemsJson { get; set; }

    /// <summary>
    /// Is fully completed (all exercises done)
    /// </summary>
    public bool IsFullyCompleted { get; set; } = false;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsNotificationSent { get; set; } = false;

    // Navigation property
    [ForeignKey(nameof(NutritionProfileId))]
    public virtual NutritionProfileEntity NutritionProfile { get; set; } = null!;
}
