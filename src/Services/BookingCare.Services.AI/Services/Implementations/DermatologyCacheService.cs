using System.Text.Json;
using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing dermatology disease cache
/// </summary>
public class DermatologyCacheService : IDermatologyCacheService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<DermatologyCacheService> _logger;

    // JSON options with UTF-8 encoding (no Unicode escaping)
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserve UTF-8 characters
    };

    public DermatologyCacheService(
        AiDbContext dbContext,
        ILogger<DermatologyCacheService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DermatologyDiseaseCacheEntity?> FindByEnglishNameAsync(string englishName)
    {
        if (string.IsNullOrWhiteSpace(englishName))
            return null;

        var normalized = englishName.ToLowerInvariant().Trim();

        var result = await _dbContext.DermatologyDiseaseCache
            .Where(c => c.EnglishName.ToLower() == normalized)
            .OrderByDescending(c => c.UsageCount)
            .ThenByDescending(c => c.SuccessRate)
            .FirstOrDefaultAsync();

        if (result != null)
        {
            _logger.LogInformation(
                "✅ Cache Hit: Found cached disease '{English}' → '{Vietnamese}'",
                englishName,
                result.VietnameseName);
        }

        return result;
    }

    public async Task SaveDiseaseAsync(
        string englishName,
        string vietnameseName,
        Dictionary<string, List<string>> adviceBySeverity,
        List<string>? reasons = null,
        string createdBy = "GROQ")
    {
        // Check if already exists
        var normalized = englishName.ToLowerInvariant().Trim();
        var existing = await _dbContext.DermatologyDiseaseCache
            .Where(c => c.EnglishName.ToLower() == normalized)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            _logger.LogDebug(
                "⏭️ Skipping cache save: Disease already exists for '{English}'",
                englishName);
            return;
        }

        // Clean Vietnamese name - remove XML/HTML tags and unwanted text
        vietnameseName = TextHelper.CleanVietnameseName(vietnameseName);

        // Serialize advice by severity (with UTF-8 encoding)
        var adviceJson = JsonSerializer.Serialize(adviceBySeverity, JsonOptions);

        // Serialize reasons if provided
        string? reasonsJson = null;
        if (reasons != null && reasons.Count > 0)
        {
            reasonsJson = JsonSerializer.Serialize(reasons, JsonOptions);
        }

        var entry = new DermatologyDiseaseCacheEntity
        {
            Id = Guid.NewGuid(),
            EnglishName = englishName,
            VietnameseName = vietnameseName,
            ReasonsJson = reasonsJson,
            AdviceJson = adviceJson,
            UsageCount = 0,
            SuccessRate = 0.0,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _dbContext.DermatologyDiseaseCache.Add(entry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "💾 Saved to cache: '{English}' → '{Vietnamese}' with {SeverityCount} severity levels",
            englishName,
            vietnameseName,
            adviceBySeverity.Count);
    }


    public async Task IncrementUsageAsync(Guid cacheId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE DermatologyDiseaseCache 
              SET UsageCount = UsageCount + 1,
                  LastUsedAt = GETUTCDATE()
              WHERE Id = {0}",
            cacheId);
    }

    public async Task<DermatologyCacheStatistics> GetStatisticsAsync()
    {
        var stats = await _dbContext.DermatologyDiseaseCache
            .Select(c => new
            {
                TotalUsage = c.UsageCount
            })
            .ToListAsync();

        var topDiseases = await _dbContext.DermatologyDiseaseCache
            .OrderByDescending(c => c.UsageCount)
            .Take(10)
            .Select(c => new TopCachedDisease
            {
                EnglishName = c.EnglishName,
                VietnameseName = c.VietnameseName,
                UsageCount = c.UsageCount
            })
            .ToListAsync();

        var totalEntries = stats.Count;
        var totalUsage = stats.Sum(s => s.TotalUsage);

        return new DermatologyCacheStatistics
        {
            TotalEntries = totalEntries,
            TotalUsage = totalUsage,
            AverageUsagePerEntry = totalEntries > 0 ? (double)totalUsage / totalEntries : 0,
            TopDiseases = topDiseases
        };
    }
}

