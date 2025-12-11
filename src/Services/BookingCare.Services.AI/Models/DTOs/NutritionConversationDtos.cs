namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// Request to start nutrition conversation
/// </summary>
public class StartNutritionRequest
{
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Request to answer nutrition question
/// </summary>
public class AnswerNutritionRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Response for nutrition conversation
/// </summary>
public class NutritionConversationResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public bool IsComplete { get; set; }
    public NutritionProfileDto? Profile { get; set; }
    public MealPlanDto? MealPlan { get; set; }
    public WorkoutPlanDto? WorkoutPlan { get; set; }
}

/// <summary>
/// Conversation state for nutrition profile creation
/// </summary>
public class NutritionConversationState
{
    public int CurrentStep { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? ActivityLevel { get; set; }
    public string? HealthGoal { get; set; }
    public List<string>? HealthConditions { get; set; }
    public DietaryPreferencesDto? DietaryPreferences { get; set; }
}
