namespace BookingCare.Services.AI.Models.DTOs.Responses;

/// <summary>
/// Response for lab result analysis
/// </summary>
public class LabResultAnalysisResponse
{
    /// <summary>
    /// Session ID for conversation persistence
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// URL of uploaded lab result image
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Text extracted from image via OCR
    /// </summary>
    public string ExtractedText { get; set; } = string.Empty;

    /// <summary>
    /// List of normal lab indicators
    /// </summary>
    public List<LabIndicator> NormalIndicators { get; set; } = new();

    /// <summary>
    /// List of abnormal lab indicators with explanations
    /// </summary>
    public List<AbnormalLabIndicator> AbnormalIndicators { get; set; } = new();

    /// <summary>
    /// Recommended doctors based on analysis
    /// </summary>
    public List<DoctorRecommendation> RecommendedDoctors { get; set; } = new();

    /// <summary>
    /// Recommended hospitals based on analysis
    /// </summary>
    public List<HospitalRecommendation> RecommendedHospitals { get; set; } = new();

    /// <summary>
    /// Medical disclaimer text
    /// </summary>
    public string Disclaimer { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of analysis
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Lab indicator (test result)
/// </summary>
public class LabIndicator
{
    /// <summary>
    /// Name of the indicator (e.g., "Hemoglobin", "WBC")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Measured value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement (e.g., "g/dL", "10^9/L")
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Reference range (normal range)
    /// </summary>
    public string ReferenceRange { get; set; } = string.Empty;
}

/// <summary>
/// Abnormal lab indicator with medical analysis
/// </summary>
public class AbnormalLabIndicator : LabIndicator
{
    /// <summary>
    /// Explanation of why this value is abnormal
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Medical advice for this abnormal value
    /// </summary>
    public string Advice { get; set; } = string.Empty;

    /// <summary>
    /// Possible diagnosis based on this indicator
    /// </summary>
    public string PossibleDiagnosis { get; set; } = string.Empty;

    /// <summary>
    /// Recommended medical specialties for this diagnosis
    /// </summary>
    public List<SpecialtyMatch> RecommendedSpecialties { get; set; } = new();
}
