using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing lab result abnormal indicator cache with context awareness
/// Implements 3-tier caching: Exact text match → Exact keywords match → Fuzzy match
/// </summary>
public class LabResultCacheService : ILabResultCacheService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<LabResultCacheService> _logger;

    public LabResultCacheService(
        AiDbContext dbContext,
        ILogger<LabResultCacheService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Tier 0: Find cached analysis with exact normalized text match (fastest, most accurate)
    /// </summary>
    public async Task<LabResultAbnormalIndicatorCacheEntity?> FindExactTextMatchAsync(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return null;

        var normalized = normalizedText.ToLowerInvariant().Trim();

        var result = await _dbContext.LabResultAbnormalIndicatorCache
            .Where(c => c.NormalizedText != null
                     && c.NormalizedText.ToLower() == normalized)
            .OrderByDescending(c => c.UsageCount)
            .ThenByDescending(c => c.SuccessRate)
            .FirstOrDefaultAsync();

        if (result != null)
        {
            _logger.LogInformation(
                "✅ Tier 0 Hit: Exact text match for lab result analysis");
        }

        return result;
    }

    public async Task<LabResultAbnormalIndicatorCacheEntity?> FindExactMatchAsync(string keywords)
    {
        var normalized = keywords.ToLowerInvariant();

        var result = await _dbContext.LabResultAbnormalIndicatorCache
            .Where(c => c.NormalizedKeywords == normalized)
            .OrderByDescending(c => c.UsageCount)
            .ThenByDescending(c => c.SuccessRate)
            .FirstOrDefaultAsync();

        if (result != null)
        {
            _logger.LogInformation(
                "✅ Tier 1 Hit: Exact keywords match for '{Keywords}'",
                keywords);
        }

        return result;
    }

    public async Task<LabResultAbnormalIndicatorCacheEntity?> FindFuzzyMatchAsync(
        string keywords,
        double threshold = 0.75)
    {
        var keywordList = keywords.ToLowerInvariant()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .ToList();

        // Get candidates - limit to top 100 by usage for performance
        var candidates = await _dbContext.LabResultAbnormalIndicatorCache
            .OrderByDescending(c => c.UsageCount)
            .Take(100)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            return null;
        }

        // Calculate Jaccard similarity for each candidate
        var matches = candidates
            .Select(c => new
            {
                Cache = c,
                Similarity = CalculateJaccardSimilarity(
                    keywordList,
                    c.NormalizedKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim())
                        .ToList())
            })
            .Where(m => m.Similarity >= threshold)
            .OrderByDescending(m => m.Similarity)
            .ThenByDescending(m => m.Cache.UsageCount)
            .ThenByDescending(m => m.Cache.SuccessRate)
            .FirstOrDefault();

        if (matches != null)
        {
            _logger.LogInformation(
                "✅ Tier 2 Hit: Fuzzy keywords match '{Input}' → '{Cached}' (similarity: {Similarity:P})",
                keywords,
                matches.Cache.NormalizedKeywords,
                matches.Similarity);
        }

        return matches?.Cache;
    }

    public async Task SaveAnalysisAsync(
        string normalizedKeywords,
        string? normalizedText,
        string abnormalIndicatorsJson,
        string normalIndicatorsJson,
        string specialtiesJson,
        string? disclaimer,
        string createdBy = "GROQ")
    {
        var entry = new LabResultAbnormalIndicatorCacheEntity
        {
            Id = Guid.NewGuid(),
            NormalizedKeywords = normalizedKeywords.ToLowerInvariant(),
            NormalizedText = normalizedText?.ToLowerInvariant().Trim(),
            AbnormalIndicatorsJson = abnormalIndicatorsJson,
            NormalIndicatorsJson = normalIndicatorsJson,
            SpecialtiesJson = specialtiesJson,
            Disclaimer = disclaimer,
            UsageCount = 0,
            SuccessRate = 0.0,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _dbContext.LabResultAbnormalIndicatorCache.Add(entry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "💾 Saved to cache: '{Keywords}' → {AbnormalCount} abnormal indicators",
            normalizedKeywords,
            abnormalIndicatorsJson.Length > 0 ? "with" : "no");
    }

    public async Task IncrementUsageAsync(Guid cacheId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE LabResultAbnormalIndicatorCache 
              SET UsageCount = UsageCount + 1,
                  LastUsedAt = GETUTCDATE()
              WHERE Id = {0}",
            cacheId);
    }

    public async Task<LabResultCacheStatistics> GetStatisticsAsync()
    {
        var stats = await _dbContext.LabResultAbnormalIndicatorCache
            .Select(c => new
            {
                TotalUsage = c.UsageCount
            })
            .ToListAsync();

        var topResults = await _dbContext.LabResultAbnormalIndicatorCache
            .OrderByDescending(c => c.UsageCount)
            .Take(10)
            .Select(c => new TopCachedLabResult
            {
                Keywords = c.NormalizedKeywords,
                UsageCount = c.UsageCount
            })
            .ToListAsync();

        var totalEntries = stats.Count;
        var totalUsage = stats.Sum(s => s.TotalUsage);

        return new LabResultCacheStatistics
        {
            TotalEntries = totalEntries,
            TotalUsage = totalUsage,
            AverageUsagePerEntry = totalEntries > 0 ? (double)totalUsage / totalEntries : 0,
            TopResults = topResults
        };
    }

    /// <summary>
    /// Calculate Jaccard similarity between two keyword sets
    /// Similarity = |intersection| / |union|
    /// </summary>
    private double CalculateJaccardSimilarity(List<string> set1, List<string> set2)
    {
        if (set1.Count == 0 && set2.Count == 0)
            return 1.0;

        if (set1.Count == 0 || set2.Count == 0)
            return 0.0;

        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }
}

