namespace BookingCare.Services.AI.Models.Entities;

/// <summary>
/// Entity model for conversation sessions
/// </summary>
public class ConversationSessionEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// User ID if authenticated (nullable for anonymous users)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Province ID for location context
    /// </summary>
    public string? ProvinceId { get; set; }

    /// <summary>
    /// District ID for location context
    /// </summary>
    public string? DistrictId { get; set; }

    /// <summary>
    /// Conversation title (extracted from first user message)
    /// </summary>
    public string? Title { get; set; }

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
    public Guid? UserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}