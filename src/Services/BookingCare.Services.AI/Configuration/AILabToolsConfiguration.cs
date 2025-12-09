namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration for AILabTools Detect Skin Disease API
/// </summary>
public class AILabToolsConfiguration
{
    /// <summary>
    /// Base URL for AILabTools API (default: https://www.ailabapi.com)
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://www.ailabapi.com";

    /// <summary>
    /// API Key for authentication (deprecated - now stored in database)
    /// </summary>
    [Obsolete("API keys are now managed in database via IAILabToolsApiKeyService")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// HTTP request timeout in seconds (default: 30)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum image file size in bytes (default: 20MB)
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Maximum image resolution (default: 1280x1280)
    /// </summary>
    public int MaxImageResolution { get; set; } = 1280;
}
