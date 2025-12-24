namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Container chứa cấu hình Gemini cho tất cả các services
/// Container for all service-specific Gemini configurations
/// </summary>
public class GeminiServicesConfiguration
{
    /// <summary>
    /// Cấu hình cho Symptom Analysis Service
    /// </summary>
    public ServiceGeminiConfiguration SymptomAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Dermatology Analysis Service
    /// </summary>
    public ServiceGeminiConfiguration DermatologyAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Lab Result Analysis Service
    /// </summary>
    public ServiceGeminiConfiguration LabResultAnalysis { get; set; } = new();

    /// <summary>
    /// Cấu hình cho Medical Summary Service
    /// </summary>
    public ServiceGeminiConfiguration MedicalSummary { get; set; } = new();
}
