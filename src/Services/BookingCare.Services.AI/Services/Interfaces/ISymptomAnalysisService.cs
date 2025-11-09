using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Main service interface for symptom analysis
/// </summary>
public interface ISymptomAnalysisService
{
    /// <summary>
    /// Analyze user's symptoms and provide recommendations
    /// </summary>
    /// <param name="request">Symptom analysis request</param>
    /// <returns>Analysis response with recommendations</returns>
    Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request);

    /// <summary>
    /// Get conversation history for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>List of conversation messages</returns>
    Task<List<Models.DTOs.Requests.ConversationMessage>> GetConversationHistoryAsync(Guid sessionId);

    /// <summary>
    /// Get all conversation sessions for a user
    /// </summary>
    /// <param name="userId">User ID (optional)</param>
    /// <returns>List of session summaries</returns>
    Task<List<Models.DTOs.Responses.SessionSummary>> GetUserSessionsAsync(Guid? userId);

    /// <summary>
    /// Delete a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId);
}


