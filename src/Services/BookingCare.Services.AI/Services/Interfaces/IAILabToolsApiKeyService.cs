namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for managing AILabTools API keys with rotation after 10 uses
/// </summary>
public interface IAILabToolsApiKeyService
{
    /// <summary>
    /// Get an active API key for use. Increments usage count automatically.
    /// If key reaches MaxUsageCount (10), it will be deleted and next key will be used.
    /// </summary>
    /// <returns>API key string or null if no active keys available</returns>
    Task<string?> GetApiKeyAsync();

    /// <summary>
    /// Increment usage count for a specific API key
    /// </summary>
    /// <param name="apiKeyId">ID of the API key entity</param>
    /// <returns>True if key was deleted (reached max usage), false otherwise</returns>
    Task<bool> IncrementUsageAsync(Guid apiKeyId);

    /// <summary>
    /// Add a new API key to the database
    /// </summary>
    /// <param name="apiKey">API key value</param>
    /// <param name="maxUsageCount">Maximum usage count before deletion (default: 10)</param>
    /// <param name="notes">Optional notes for this key</param>
    /// <returns>Created entity ID</returns>
    Task<Guid> AddApiKeyAsync(string apiKey, int maxUsageCount = 10, string? notes = null);

    /// <summary>
    /// Get all active API keys with their usage counts, ordered by priority (next to use first)
    /// </summary>
    Task<List<AILabToolsApiKeyInfo>> GetActiveKeysAsync();

    /// <summary>
    /// Get information about the next API key that will be used
    /// </summary>
    /// <returns>Information about the next key to use, or null if no keys available</returns>
    Task<AILabToolsApiKeyInfo?> GetNextKeyInfoAsync();

    /// <summary>
    /// Deactivate an API key (soft delete)
    /// </summary>
    Task DeactivateKeyAsync(Guid apiKeyId);
}

/// <summary>
/// API key information model
/// </summary>
public class AILabToolsApiKeyInfo
{
    public Guid Id { get; set; }
    public int UsageCount { get; set; }
    public int MaxUsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    /// <summary>
    /// Priority order: 1 = will be used next, 2 = second, etc.
    /// </summary>
    public int PriorityOrder { get; set; }
}


