namespace BookingCare.Services.AI.Models.DTOs.Responses;

/// <summary>
/// Response model for dermatology image analysis
/// </summary>
public class DermatologyAnalysisResponse
{
    /// <summary>
    /// Session ID for tracking conversation history
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// CloudFront URL of the uploaded image
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Primary skin condition diagnosis
    /// </summary>
    public SkinConditionDiagnosis? Diagnosis { get; set; }

    /// <summary>
    /// Malignancy risk assessment
    /// </summary>
    public MalignancyAssessment? MalignancyRisk { get; set; }

    /// <summary>
    /// General medical advice based on diagnosis
    /// </summary>
    public List<string> GeneralAdvice { get; set; } = new();

    /// <summary>
    /// Whether biopsy is recommended
    /// </summary>
    public bool BiopsyRecommended { get; set; }

    /// <summary>
    /// Reason for biopsy recommendation
    /// </summary>
    public string? BiopsyReason { get; set; }

    /// <summary>
    /// Recommended dermatologists
    /// </summary>
    public List<DoctorRecommendation> RecommendedDoctors { get; set; } = new();

    /// <summary>
    /// Recommended hospitals with dermatology department
    /// </summary>
    public List<HospitalRecommendation> RecommendedHospitals { get; set; } = new();

    /// <summary>
    /// Medical disclaimer
    /// </summary>
    public string Disclaimer { get; set; } = string.Empty;

    /// <summary>
    /// Detailed medical conclusion about the condition (500-800 words)
    /// Includes: description, causes, symptoms, treatment, prognosis
    /// </summary>
    public string? DetailedConclusion { get; set; }

    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Skin condition diagnosis information
/// </summary>
public class SkinConditionDiagnosis
{
    /// <summary>
    /// Name of the skin condition (e.g., "Melanoma", "Basal Cell Carcinoma")
    /// </summary>
    public string ConditionName { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Detailed description of the condition
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Severity level (e.g., "Mild", "Moderate", "Severe")
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// ICD-11 code if available
    /// </summary>
    public string? IcdCode { get; set; }
}

/// <summary>
/// Malignancy risk assessment
/// </summary>
public class MalignancyAssessment
{
    /// <summary>
    /// Suspicion level for malignancy (0-1, where 1 is highest suspicion)
    /// </summary>
    public double SuspicionLevel { get; set; }

    /// <summary>
    /// Risk category: "Low", "Medium", "High"
    /// </summary>
    public string RiskCategory { get; set; } = string.Empty;

    /// <summary>
    /// List of risk factors identified
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Recommended urgency for medical consultation
    /// </summary>
    public string? UrgencyLevel { get; set; }
}
