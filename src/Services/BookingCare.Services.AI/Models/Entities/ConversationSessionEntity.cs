namespace BookingCare.Services.AI.Models.Entities;

/// <summary>
/// Conversation type enum
/// </summary>
public enum ConversationType
{
    SymptomAnalysis,
    LabResultAnalysis,
    MedicalImageAnalysis
}

/// <summary>
/// Entity model for conversation sessions
/// </summary>
public class ConversationSessionEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// User ID (required - user must be authenticated)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Conversation title (extracted from first user message)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Type of conversation (symptom analysis, lab result, medical image)
    /// </summary>
    public ConversationType ConversationType { get; set; } = ConversationType.SymptomAnalysis;

    /// <summary>
    /// Number of files uploaded in this conversation (for file analysis types only)
    /// </summary>
    public int FileUploadCount { get; set; } = 0;

    /// <summary>
    /// Conversation history stored as JSON
    /// </summary>
    public string ConversationHistory { get; set; } = "[]";

    /// <summary>
    /// Created timestamp (UTC)
    /// Note: Set automatically by DbContext, do not set manually
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp (UTC)
    /// Note: Set automatically by DbContext, do not set manually
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary entity for conversation sessions (for listing)
/// </summary>
public class SessionSummaryEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public ConversationType ConversationType { get; set; } = ConversationType.SymptomAnalysis;
}