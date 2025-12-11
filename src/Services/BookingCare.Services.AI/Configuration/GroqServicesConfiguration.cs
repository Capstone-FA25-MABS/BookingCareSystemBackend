namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Container chứa cấu hình Groq cho tất cả các services
/// Container for all service-specific Groq configurations
/// </summary>
public class GroqServicesConfiguration
{
    /// <summary>
    /// Cấu hình cho Symptom Analysis Service
    /// </summary>
    public ServiceGroqConfiguration SymptomAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Dermatology Analysis Service
    /// </summary>
    public ServiceGroqConfiguration DermatologyAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Lab Result Analysis Service
    /// </summary>
    public ServiceGroqConfiguration LabResultAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Medical Summary Service
    /// </summary>
    public ServiceGroqConfiguration MedicalSummary { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Nutrition Service
    /// </summary>
    public ServiceGroqConfiguration NutritionService { get; set; } = new();
}

