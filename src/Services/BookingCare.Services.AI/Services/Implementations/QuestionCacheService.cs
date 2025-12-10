using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing symptom question cache with context awareness
/// Implements 3-tier caching: Exact message match → Exact keywords match → Fuzzy match
/// </summary>
public class QuestionCacheService : IQuestionCacheService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<QuestionCacheService> _logger;

    public QuestionCacheService(
        AiDbContext dbContext,
        ILogger<QuestionCacheService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Tier 0: Find cached question with exact normalized message match (fastest, most accurate)
    /// </summary>
    public async Task<SymptomQuestionCacheEntity?> FindExactMessageMatchAsync(
        string normalizedMessage,
        int questionNumber)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return null;

        var normalized = normalizedMessage.ToLowerInvariant().Trim();

        var result = await _dbContext.SymptomQuestionCache
            .Where(q => q.NormalizedMessage != null
                     && q.NormalizedMessage.ToLower() == normalized
                     && q.QuestionNumber == questionNumber)
            .OrderByDescending(q => q.UsageCount)
            .ThenByDescending(q => q.SuccessRate)
            .FirstOrDefaultAsync();

        if (result != null)
        {
            _logger.LogInformation(
                "✅ Tier 0 Hit: Exact message match for '{Message}' Q{Number}",
                normalizedMessage,
                questionNumber);
        }

        return result;
    }

    public async Task<SymptomQuestionCacheEntity?> FindExactMatchAsync(
        string keywords,
        int questionNumber)
    {
        var normalized = keywords.ToLowerInvariant();

        var result = await _dbContext.SymptomQuestionCache
            .Where(q => q.NormalizedKeywords == normalized
                     && q.QuestionNumber == questionNumber)
            .OrderByDescending(q => q.UsageCount)
            .ThenByDescending(q => q.SuccessRate)
            .FirstOrDefaultAsync();

        if (result != null)
        {
            _logger.LogInformation(
                "✅ Tier 1 Hit: Exact keywords match for '{Keywords}' Q{Number}",
                keywords,
                questionNumber);
        }

        return result;
    }

    public async Task<SymptomQuestionCacheEntity?> FindFuzzyMatchAsync(
        string keywords,
        int questionNumber,
        double threshold = 0.75)
    {
        var keywordList = CacheHelper.ParseKeywords(keywords);

        // Get candidates for this question number
        // Limit to top 100 by usage for performance
        var candidates = await _dbContext.SymptomQuestionCache
            .Where(q => q.QuestionNumber == questionNumber)
            .OrderByDescending(q => q.UsageCount)
            .Take(100)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            return null;
        }

        var bestMatch = CacheHelper.FindBestFuzzyMatch(
            candidates,
            c => c.NormalizedKeywords,
            keywordList,
            threshold,
            c => c.UsageCount,
            c => c.SuccessRate);

        if (bestMatch != null)
        {
            CacheHelper.CalculateAndLogSimilarity(
                keywordList,
                bestMatch.NormalizedKeywords,
                keywords,
                _logger,
                questionNumber);
        }

        return bestMatch;
    }

    public async Task SaveQuestionAsync(
        string initialSymptom,
        string conversationContext,
        string normalizedKeywords,
        string? normalizedMessage,
        int questionNumber,
        string question,
        string? purpose = null,
        string? priority = null,
        string createdBy = "GEMINI")
    {
        var entry = new SymptomQuestionCacheEntity
        {
            Id = Guid.NewGuid(),
            InitialSymptom = initialSymptom,
            ConversationContext = conversationContext,
            NormalizedKeywords = normalizedKeywords.ToLowerInvariant(),
            NormalizedMessage = normalizedMessage?.ToLowerInvariant().Trim(),
            QuestionNumber = questionNumber,
            Question = question,
            Purpose = purpose,
            Priority = priority,
            UsageCount = 0,
            SuccessRate = 0.0,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _dbContext.SymptomQuestionCache.Add(entry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "💾 Saved to cache: '{Keywords}' Q{Number} → '{Question}'",
            normalizedKeywords,
            questionNumber,
            question.Length > 50 ? question.Substring(0, 50) + "..." : question);
    }

    public async Task IncrementUsageAsync(Guid cacheId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE SymptomQuestionCache 
              SET UsageCount = UsageCount + 1,
                  LastUsedAt = GETUTCDATE()
              WHERE Id = {0}",
            cacheId);
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var stats = await _dbContext.SymptomQuestionCache
            .GroupBy(q => q.QuestionNumber)
            .Select(g => new
            {
                QuestionNumber = g.Key,
                Count = g.Count(),
                TotalUsage = g.Sum(q => q.UsageCount)
            })
            .ToListAsync();

        var topQuestions = await _dbContext.SymptomQuestionCache
            .OrderByDescending(q => q.UsageCount)
            .Take(10)
            .Select(q => new TopCachedQuestion
            {
                Keywords = q.NormalizedKeywords,
                QuestionNumber = q.QuestionNumber,
                Question = q.Question,
                UsageCount = q.UsageCount
            })
            .ToListAsync();

        var totalEntries = stats.Sum(s => s.Count);
        var totalUsage = stats.Sum(s => s.TotalUsage);

        return new CacheStatistics
        {
            TotalEntries = totalEntries,
            TotalUsage = totalUsage,
            EntriesByQuestion = stats.ToDictionary(s => s.QuestionNumber, s => s.Count),
            AverageUsagePerEntry = totalEntries > 0 ? (double)totalUsage / totalEntries : 0,
            TopQuestions = topQuestions
        };
    }

}
