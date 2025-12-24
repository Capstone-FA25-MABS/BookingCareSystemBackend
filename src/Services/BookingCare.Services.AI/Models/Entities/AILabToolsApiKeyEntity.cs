namespace BookingCare.Services.AI.Models.Entities;

/// <summary>
/// Entity model for AILabTools API keys management
/// Stores API keys with usage count for rotation after 10 uses
/// </summary>
public class AILabToolsApiKeyEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// API Key value (encrypted or plain text based on security requirements)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this API key has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Maximum usage count before key is deleted (default: 10)
    /// </summary>
    public int MaxUsageCount { get; set; } = 10;

    /// <summary>
    /// Whether this key is currently active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last used timestamp (UTC)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Notes or description for this API key
    /// </summary>
    public string? Notes { get; set; }
}


