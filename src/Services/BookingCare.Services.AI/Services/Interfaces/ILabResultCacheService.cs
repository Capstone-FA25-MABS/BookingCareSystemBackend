using BookingCare.Services.AI.Models.Entities;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing lab result abnormal indicator cache
/// </summary>
public interface ILabResultCacheService
{
    /// <summary>
    /// Find cached analysis with exact normalized text match (Tier 0 - fastest, most accurate)
    /// </summary>
    /// <param name="normalizedText">Normalized extracted text with stop words removed</param>
    /// <returns>Cached analysis entity or null if not found</returns>
    Task<LabResultAbnormalIndicatorCacheEntity?> FindExactTextMatchAsync(string normalizedText);

    /// <summary>
    /// Find cached analysis with exact keyword match (Tier 1)
    /// </summary>
    /// <param name="keywords">Normalized keywords (e.g., "wbc,hemoglobin,glucose")</param>
    /// <returns>Cached analysis entity or null if not found</returns>
    Task<LabResultAbnormalIndicatorCacheEntity?> FindExactMatchAsync(string keywords);

    /// <summary>
    /// Find cached analysis with fuzzy keyword match (Jaccard similarity)
    /// </summary>
    /// <param name="keywords">Normalized keywords</param>
    /// <param name="threshold">Similarity threshold (0.0-1.0, default 0.75)</param>
    /// <returns>Cached analysis entity or null if no match above threshold</returns>
    Task<LabResultAbnormalIndicatorCacheEntity?> FindFuzzyMatchAsync(
        string keywords,
        double threshold = 0.75);

    /// <summary>
    /// Save analysis to cache
    /// </summary>
    Task SaveAnalysisAsync(
        string normalizedKeywords,
        string? normalizedText,
        string abnormalIndicatorsJson,
        string normalIndicatorsJson,
        string specialtiesJson,
        string? disclaimer,
        string createdBy = "GROQ");

    /// <summary>
    /// Increment usage count for cache entry
    /// </summary>
    Task IncrementUsageAsync(Guid cacheId);

    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    Task<LabResultCacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// Cache statistics model for lab result analysis
/// </summary>
public class LabResultCacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalUsage { get; set; }
    public double AverageUsagePerEntry { get; set; }
    public List<TopCachedLabResult> TopResults { get; set; } = new();
}

/// <summary>
/// Top cached lab result model
/// </summary>
public class TopCachedLabResult
{
    public string Keywords { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

