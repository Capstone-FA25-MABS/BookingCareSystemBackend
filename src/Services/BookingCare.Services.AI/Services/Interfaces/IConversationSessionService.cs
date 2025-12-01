using BookingCare.Services.AI.Models.DTOs.Requests;
using System.Diagnostics.CodeAnalysis;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing conversation sessions
/// </summary>
public interface IConversationSessionService
{
    /// <summary>
    /// Get or create a conversation session (requires authenticated user)
    /// </summary>
    Task<Guid> GetOrCreateSessionAsync(Guid? sessionId, Guid userId, LocationContext? location);

    /// <summary>
    /// Load conversation history for a session
    /// </summary>
    Task<List<ConversationMessage>> LoadConversationHistoryAsync(Guid sessionId);

    /// <summary>
    /// Save conversation history for a session
    /// </summary>
    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "Method captures all contextual fields needed to persist AI conversations; consolidating into a DTO would complicate call sites without improving clarity.")]
    Task SaveConversationHistoryAsync(
        Guid sessionId,
        string userMessage,
        string aiMessage,
        LocationContext? location = null,
        object? suggestions = null,
        Guid? userId = null,
        object? disease = null,
        int? questionCount = null,
        bool? analysisComplete = null);

    /// <summary>
    /// Get all conversation sessions for a user (requires authenticated user)
    /// </summary>
    Task<List<Models.Entities.SessionSummaryEntity>> GetUserSessionsAsync(Guid userId);

    /// <summary>
    /// Delete a conversation session (requires authenticated user and ownership verification)
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId);

    /// <summary>
    /// Check if a lab result has already been uploaded in this session
    /// </summary>
    Task<bool> CheckIfLabResultExistsAsync(Guid sessionId);
}
