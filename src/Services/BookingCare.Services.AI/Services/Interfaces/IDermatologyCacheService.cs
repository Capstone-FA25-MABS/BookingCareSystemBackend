using BookingCare.Services.AI.Models.Entities;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing dermatology disease cache
/// </summary>
public interface IDermatologyCacheService
{
    /// <summary>
    /// Find cached disease by English name (exact match)
    /// </summary>
    /// <param name="englishName">English disease name from AILabTools (e.g., "melanoma")</param>
    /// <returns>Cached disease entity or null if not found</returns>
    Task<DermatologyDiseaseCacheEntity?> FindByEnglishNameAsync(string englishName);

    /// <summary>
    /// Save disease translation and advice to cache
    /// </summary>
    Task SaveDiseaseAsync(
        string englishName,
        string vietnameseName,
        Dictionary<string, List<string>> adviceBySeverity,
        List<string>? reasons = null,
        string createdBy = "GROQ");

    /// <summary>
    /// Increment usage count for cache entry
    /// </summary>
    Task IncrementUsageAsync(Guid cacheId);

    /// <summary>
    /// Get cache statistics for monitoring
    /// </summary>
    Task<DermatologyCacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// Cache statistics model for dermatology analysis
/// </summary>
public class DermatologyCacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalUsage { get; set; }
    public double AverageUsagePerEntry { get; set; }
    public List<TopCachedDisease> TopDiseases { get; set; } = new();
}

/// <summary>
/// Top cached disease model
/// </summary>
public class TopCachedDisease
{
    public string EnglishName { get; set; } = string.Empty;
    public string VietnameseName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

