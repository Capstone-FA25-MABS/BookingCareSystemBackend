using BookingCare.Services.AI.Models.DTOs.Requests;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for extracting keywords from conversation history with context awareness
/// </summary>
public interface IContextKeywordExtractor
{
    /// <summary>
    /// Extract keywords from conversation history including context from previous answers
    /// </summary>
    /// <param name="currentMessage">Current user message</param>
    /// <param name="history">Full conversation history</param>
    /// <returns>Normalized keywords string (lowercase, sorted, comma-separated)</returns>
    string ExtractKeywordsWithContext(
        string currentMessage,
        List<ConversationMessage> history);
    
    /// <summary>
    /// Extract only initial symptom keywords from first user message
    /// </summary>
    /// <param name="message">User message</param>
    /// <returns>Normalized symptom keywords</returns>
    string ExtractInitialSymptom(string message);
    
    /// <summary>
    /// Extract context keywords from user answers (location, intensity, etc.)
    /// </summary>
    /// <param name="history">Conversation history</param>
    /// <returns>List of context keywords</returns>
    List<string> ExtractContextFromAnswers(List<ConversationMessage> history);
    
    /// <summary>
    /// Normalize message for exact matching: remove stop words, lowercase, normalize whitespace
    /// Used for Tier 0 exact message matching in cache
    /// </summary>
    /// <param name="message">Original user message</param>
    /// <returns>Normalized message with stop words removed</returns>
    string NormalizeMessage(string message);
}
