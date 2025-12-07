namespace BookingCare.Services.AI.Models.Entities;

/// <summary>
/// Entity model for symptom question cache with conversation context
/// Stores cached questions based on initial symptom + conversation context
/// </summary>
public class SymptomQuestionCacheEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Initial symptom from first user message (e.g., "đau đầu")
    /// </summary>
    public string InitialSymptom { get; set; } = string.Empty;
    
    /// <summary>
    /// Conversation context from previous answers (e.g., "trước trán")
    /// </summary>
    public string? ConversationContext { get; set; }
    
    /// <summary>
    /// Normalized cache key: lowercase, sorted (e.g., "đau đầu,trước trán")
    /// Used for exact matching
    /// </summary>
    public string NormalizedKeywords { get; set; } = string.Empty;
    
    /// <summary>
    /// Normalized message: original message with stop words removed, lowercase
    /// Used for Tier 0 exact message matching (e.g., "bị đau răng" from "tôi bị đau răng")
    /// </summary>
    public string? NormalizedMessage { get; set; }
    
    /// <summary>
    /// Question number in workflow (1, 2, or 3)
    /// </summary>
    public int QuestionNumber { get; set; }
    
    /// <summary>
    /// Cached question text
    /// </summary>
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// Purpose/reason for asking this question
    /// </summary>
    public string? Purpose { get; set; }
    
    /// <summary>
    /// Priority level (HIGH, MEDIUM, LOW)
    /// </summary>
    public string? Priority { get; set; }
    
    /// <summary>
    /// Number of times this cache entry has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;
    
    /// <summary>
    /// Success rate based on user feedback (0.0 - 1.0)
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;
    
    /// <summary>
    /// Created timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last used timestamp (UTC)
    /// </summary>
    public DateTime LastUsedAt { get; set; }
    
    /// <summary>
    /// Source of this cache entry (GEMINI_PRO, GEMINI_FLASH, MANUAL)
    /// </summary>
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Entity model for conversation context keywords
/// Stores keyword patterns for extracting context from user answers
/// </summary>
public class ConversationContextKeywordEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Canonical keyword (e.g., "trước trán")
    /// </summary>
    public string Keyword { get; set; } = string.Empty;
    
    /// <summary>
    /// Category of keyword (LOCATION, SYMPTOM, INTENSITY)
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Synonyms stored as JSON array (e.g., ["trước trán", "trán", "vùng trán"])
    /// </summary>
    public string? Synonyms { get; set; }
    
    /// <summary>
    /// Created timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
