using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing AILabTools API keys with rotation after 10 uses
/// </summary>
public class AILabToolsApiKeyService : IAILabToolsApiKeyService
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<AILabToolsApiKeyService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public AILabToolsApiKeyService(
        AiDbContext dbContext,
        ILogger<AILabToolsApiKeyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetApiKeyAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Tìm key có UsageCount thấp nhất, nếu bằng nhau thì lấy key tạo sớm nhất
            var apiKeyEntity = await _dbContext.AILabToolsApiKeys
                .Where(k => k.IsActive && k.UsageCount < k.MaxUsageCount)
                .OrderBy(k => k.UsageCount) // Key có usage thấp nhất trước
                .ThenBy(k => k.CreatedAt) // Nếu bằng nhau thì lấy key tạo sớm nhất
                .FirstOrDefaultAsync();

            if (apiKeyEntity == null)
            {
                _logger.LogWarning("No active AILabTools API keys available");
                return null;
            }

            // Increment usage count
            apiKeyEntity.UsageCount++;
            apiKeyEntity.LastUsedAt = DateTime.UtcNow;

            // Check if key has reached max usage
            if (apiKeyEntity.UsageCount >= apiKeyEntity.MaxUsageCount)
            {
                _logger.LogInformation(
                    "AILabTools API key {KeyId} has reached max usage ({UsageCount}/{MaxUsageCount}). Deleting key.",
                    apiKeyEntity.Id,
                    apiKeyEntity.UsageCount,
                    apiKeyEntity.MaxUsageCount);

                // Delete the key
                _dbContext.AILabToolsApiKeys.Remove(apiKeyEntity);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug(
                "Using AILabTools API key {KeyId} (Usage: {UsageCount}/{MaxUsageCount})",
                apiKeyEntity.Id,
                apiKeyEntity.UsageCount,
                apiKeyEntity.MaxUsageCount);

            return apiKeyEntity.ApiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AILabTools API key");
            throw new AILabToolsApiKeyException("Failed to get AILabTools API key.", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IncrementUsageAsync(Guid apiKeyId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var apiKeyEntity = await _dbContext.AILabToolsApiKeys
                .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.IsActive);

            if (apiKeyEntity == null)
            {
                _logger.LogWarning("AILabTools API key {KeyId} not found or inactive", apiKeyId);
                return false;
            }

            apiKeyEntity.UsageCount++;
            apiKeyEntity.LastUsedAt = DateTime.UtcNow;

            bool shouldDelete = apiKeyEntity.UsageCount >= apiKeyEntity.MaxUsageCount;

            if (shouldDelete)
            {
                _logger.LogInformation(
                    "AILabTools API key {KeyId} has reached max usage ({UsageCount}/{MaxUsageCount}). Deleting key.",
                    apiKeyEntity.Id,
                    apiKeyEntity.UsageCount,
                    apiKeyEntity.MaxUsageCount);

                _dbContext.AILabToolsApiKeys.Remove(apiKeyEntity);
            }

            await _dbContext.SaveChangesAsync();

            return shouldDelete;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing usage for AILabTools API key {KeyId}", apiKeyId);
            throw new AILabToolsApiKeyException(
                $"Failed to increment usage for AILabTools API key {apiKeyId}.", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Guid> AddApiKeyAsync(string apiKey, int maxUsageCount = 10, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        var entity = new AILabToolsApiKeyEntity
        {
            Id = Guid.NewGuid(),
            ApiKey = apiKey,
            MaxUsageCount = maxUsageCount,
            UsageCount = 0,
            IsActive = true,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AILabToolsApiKeys.Add(entity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Added new AILabTools API key {KeyId} with max usage {MaxUsageCount}",
            entity.Id,
            maxUsageCount);

        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<List<AILabToolsApiKeyInfo>> GetActiveKeysAsync()
    {
        var keys = await _dbContext.AILabToolsApiKeys
            .Where(k => k.IsActive && k.UsageCount < k.MaxUsageCount)
            .Select(k => new AILabToolsApiKeyInfo
            {
                Id = k.Id,
                UsageCount = k.UsageCount,
                MaxUsageCount = k.MaxUsageCount,
                LastUsedAt = k.LastUsedAt,
                CreatedAt = k.CreatedAt,
                Notes = k.Notes
            })
            .OrderBy(k => k.UsageCount)
            .ThenBy(k => k.CreatedAt)
            .ToListAsync();

        // Gán thứ tự ưu tiên (1 = sẽ dùng tiếp theo)
        for (int i = 0; i < keys.Count; i++)
        {
            keys[i].PriorityOrder = i + 1;
        }

        return keys;
    }

    /// <inheritdoc />
    public async Task<AILabToolsApiKeyInfo?> GetNextKeyInfoAsync()
    {
        var nextKey = await _dbContext.AILabToolsApiKeys
            .Where(k => k.IsActive && k.UsageCount < k.MaxUsageCount)
            .OrderBy(k => k.UsageCount)
            .ThenBy(k => k.CreatedAt)
            .Select(k => new AILabToolsApiKeyInfo
            {
                Id = k.Id,
                UsageCount = k.UsageCount,
                MaxUsageCount = k.MaxUsageCount,
                LastUsedAt = k.LastUsedAt,
                CreatedAt = k.CreatedAt,
                Notes = k.Notes,
                PriorityOrder = 1
            })
            .FirstOrDefaultAsync();

        return nextKey;
    }

    /// <inheritdoc />
    public async Task DeactivateKeyAsync(Guid apiKeyId)
    {
        var apiKeyEntity = await _dbContext.AILabToolsApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId);

        if (apiKeyEntity == null)
        {
            _logger.LogWarning("AILabTools API key {KeyId} not found", apiKeyId);
            return;
        }

        apiKeyEntity.IsActive = false;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deactivated AILabTools API key {KeyId}", apiKeyId);
    }
}


