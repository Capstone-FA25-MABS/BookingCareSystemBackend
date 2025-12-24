using BookingCare.Services.AI.Models.Entities;

namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// Request to create a new conversation session
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// Type of conversation (0 = SymptomAnalysis, 1 = LabResultAnalysis, 2 = MedicalImageAnalysis)
    /// </summary>
    public ConversationType ConversationType { get; set; } = ConversationType.SymptomAnalysis;

    /// <summary>
    /// Initial message (optional, used for title generation)
    /// </summary>
    public string? InitialMessage { get; set; }
}

/// <summary>
/// Response after creating a session
/// </summary>
public class CreateSessionResponse
{
    public Guid SessionId { get; set; }
    public ConversationType ConversationType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Session summary for listing
/// </summary>
public class SessionSummaryDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public ConversationType ConversationType { get; set; }
}
