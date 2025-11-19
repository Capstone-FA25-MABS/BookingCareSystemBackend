using BookingCare.Services.AI.Models.DTOs.Requests;

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
    /// Load conversation history for a session (verifies ownership)
    /// </summary>
    Task<List<ConversationMessage>> LoadConversationHistoryAsync(Guid sessionId, Guid userId);

    /// <summary>
    /// Save conversation history for a session
    /// </summary>
    Task SaveConversationHistoryAsync(
        Guid sessionId,
        Guid userId,
        string userMessage,
        string aiMessage,
        LocationContext? location = null,
        object? suggestions = null);

    /// <summary>
    /// Get all conversation sessions for a user (requires authenticated user)
    /// </summary>
    Task<List<Models.Entities.SessionSummaryEntity>> GetUserSessionsAsync(Guid userId);

    /// <summary>
    /// Delete a conversation session (requires authenticated user and ownership verification)
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId);
}
