namespace BookingCare.Services.AI.Models.DTOs.Responses;

/// <summary>
/// Response model for symptom analysis
/// </summary>
public class SymptomAnalysisResponse
{
    /// <summary>
    /// Session ID for tracking conversation
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// AI's response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Possible diseases identified
    /// </summary>
    public List<DiseaseMatch> PossibleDiseases { get; set; } = new();

    /// <summary>
    /// Follow-up questions to ask user (if analysis incomplete)
    /// </summary>
    public List<FollowUpQuestion> NextQuestions { get; set; } = new();

    /// <summary>
    /// Recommended specialties for consultation
    /// </summary>
    public List<SpecialtyMatch> RecommendedSpecialties { get; set; } = new();

    /// <summary>
    /// Recommended doctors (if specialty is determined)
    /// </summary>
    public List<DoctorRecommendation> RecommendedDoctors { get; set; } = new();

    /// <summary>
    /// Recommended hospitals (if applicable)
    /// </summary>
    public List<HospitalRecommendation> RecommendedHospitals { get; set; } = new();

    /// <summary>
    /// General health advice
    /// </summary>
    public List<string> GeneralAdvice { get; set; } = new();

    /// <summary>
    /// Whether analysis is complete (enough info to make recommendations)
    /// </summary>
    public bool AnalysisComplete { get; set; }

    /// <summary>
    /// Whether this requires immediate medical attention
    /// </summary>
    public bool RequiresImmediateAttention { get; set; }

    /// <summary>
    /// Medical disclaimer
    /// </summary>
    public string Disclaimer { get; set; } = "Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.";

    /// <summary>
    /// Timestamp of analysis
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of questions asked in current round (1-3)
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// Current consultation round (1 or 2)
    /// </summary>
    public int CurrentRound { get; set; }

    /// <summary>
    /// Maximum questions per round (always 3)
    /// </summary>
    public int MaxQuestions { get; set; } = 3;

    /// <summary>
    /// Disease conclusion (only available when AnalysisComplete = true)
    /// </summary>
    public DiseaseConclusion? Disease { get; set; }

    /// <summary>
    /// Whether user can request more questions (true when round 1 && confidence < 90%)
    /// </summary>
    public bool CanRequestMoreQuestions { get; set; }

    /// <summary>
    /// Whether this response came from cache (for monitoring/debugging)
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Cache tier used (1 = exact match, 2 = fuzzy match, null = not from cache)
    /// </summary>
    public int? CacheTier { get; set; }
}

/// <summary>
/// Disease conclusion with confidence and reasoning
/// </summary>
public class DiseaseConclusion
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0-1
    public List<string> Reasons { get; set; } = new();
}

/// <summary>
/// Possible disease match
/// </summary>
public class DiseaseMatch
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0-1
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Follow-up question
/// </summary>
public class FollowUpQuestion
{
    public string Question { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty; // Why we're asking
    public string Priority { get; set; } = "MEDIUM"; // HIGH, MEDIUM, LOW
}

/// <summary>
/// Specialty matching result
/// </summary>
public class SpecialtyMatch
{
    public Guid? SpecialtyId { get; set; } // Can be null if not determined yet
    public string SpecialtyName { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0-1
    public string Urgency { get; set; } = "NORMAL"; // EMERGENCY, URGENT, NORMAL, ROUTINE
    public List<string> Reasons { get; set; } = new(); // Why this specialty matches
}

/// <summary>
/// Doctor recommendation
/// </summary>
public class DoctorRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int YearOfExperience { get; set; }
    public string? ServiceTypeName { get; set; }
    public string? Price { get; set; }
    public double RecommendationScore { get; set; } // 0-1, weighted score
    public string? AvatarUrl { get; set; } // Doctor avatar image URL
    public List<DoctorServiceOption> ServiceOptions { get; set; } = new();
}

public class DoctorServiceOption
{
    public string? ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string? Price { get; set; }
}

/// <summary>
/// Hospital recommendation
/// </summary>
public class HospitalRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<string> SpecialtyNames { get; set; } = new();
    public double RecommendationScore { get; set; } // 0-1, weighted score
    public string? ImageUrl { get; set; } // Hospital image/avatar URL
}

/// <summary>
/// Summary of a conversation session
/// </summary>
public class SessionSummary
{
    public Guid SessionId { get; set; }
    public Guid? UserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}


