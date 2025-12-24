using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service interface for symptom analysis and medical recommendations
/// </summary>
public interface ISymptomAnalysisService
{
    /// <summary>
    /// Analyze symptoms and provide recommendations
    /// Uses 3-question workflow: asks clarifying questions until 3 questions are asked,
    /// then provides final diagnosis with disease, advice, specialty, and doctor/hospital recommendations
    /// </summary>
    /// <param name="request">Symptom analysis request with user message and context</param>
    /// <returns>Analysis response with questions or final recommendations</returns>
    Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request);

    /// <summary>
    /// Get conversation history for a specific session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>List of conversation messages</returns>
    Task<List<ConversationMessage>> GetConversationHistoryAsync(Guid sessionId);

    /// <summary>
    /// Get all conversation sessions for a user 
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of session summaries</returns>
    Task<List<SessionSummary>> GetUserSessionsAsync(Guid userId);

    /// <summary>
    /// Delete a conversation session (with ownership check)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID for ownership verification</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId);
}
