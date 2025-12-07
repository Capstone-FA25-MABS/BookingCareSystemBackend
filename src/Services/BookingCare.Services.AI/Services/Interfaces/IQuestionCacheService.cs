using BookingCare.Services.AI.Models.Entities;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing symptom question cache with conversation context
/// </summary>
public interface IQuestionCacheService
{
    /// <summary>
    /// Find cached question with exact normalized message match (Tier 0 - fastest, most accurate)
    /// </summary>
    /// <param name="normalizedMessage">Normalized message with stop words removed (e.g., "bị đau răng")</param>
    /// <param name="questionNumber">Question number (1, 2, or 3)</param>
    /// <returns>Cached question entity or null if not found</returns>
    Task<SymptomQuestionCacheEntity?> FindExactMessageMatchAsync(
        string normalizedMessage,
        int questionNumber);
    
    /// <summary>
    /// Find cached question with exact keyword match (Tier 1)
    /// </summary>
    /// <param name="keywords">Normalized keywords (e.g., "đau đầu,trước trán")</param>
    /// <param name="questionNumber">Question number (1, 2, or 3)</param>
    /// <returns>Cached question entity or null if not found</returns>
    Task<SymptomQuestionCacheEntity?> FindExactMatchAsync(
        string keywords,
        int questionNumber);
    
    /// <summary>
    /// Find cached question with fuzzy keyword match (Jaccard similarity)
    /// </summary>
    /// <param name="keywords">Normalized keywords</param>
    /// <param name="questionNumber">Question number (1, 2, or 3)</param>
    /// <param name="threshold">Similarity threshold (0.0-1.0, default 0.75)</param>
    /// <returns>Cached question entity or null if no match above threshold</returns>
    Task<SymptomQuestionCacheEntity?> FindFuzzyMatchAsync(
        string keywords,
        int questionNumber,
        double threshold = 0.75);
    
    /// <summary>
    /// Save question to cache
    /// </summary>
    Task SaveQuestionAsync(
        string initialSymptom,
        string conversationContext,
        string normalizedKeywords,
        string? normalizedMessage,
        int questionNumber,
        string question,
        string? purpose = null,
        string? priority = null,
        string createdBy = "GEMINI");
    
    /// <summary>
    /// Increment usage count for cache entry
    /// </summary>
    Task IncrementUsageAsync(Guid cacheId);
    
    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalUsage { get; set; }
    public Dictionary<int, int> EntriesByQuestion { get; set; } = new();
    public double AverageUsagePerEntry { get; set; }
    public List<TopCachedQuestion> TopQuestions { get; set; } = new();
}

/// <summary>
/// Top cached question model
/// </summary>
public class TopCachedQuestion
{
    public string Keywords { get; set; } = string.Empty;
    public int QuestionNumber { get; set; }
    public string Question { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}
