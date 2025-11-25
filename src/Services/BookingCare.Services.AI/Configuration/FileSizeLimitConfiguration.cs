using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration helper for file size limits with safety validation
/// </summary>
public static class FileSizeLimitConfiguration
{
    // Safe limits: Minimum 1MB, Maximum 500MB to prevent DoS attacks
    public const long MinFileSizeBytes = 1_048_576; // 1 MB
    public const long MaxFileSizeBytes = 524_288_000; // 500 MB
    public const long DefaultFileSizeBytes = 100_000_000; // 100 MB

    /// <summary>
    /// Reads and validates max file size from configuration
    /// </summary>
    /// <param name="configuration">IConfiguration instance</param>
    /// <param name="logger">ILogger instance for logging warnings</param>
    /// <param name="configKey">Configuration key path (e.g., "Voice:MaxFileSizeBytes" or "CallRecordings:MaxFileSizeBytes")</param>
    /// <returns>Validated max file size in bytes</returns>
    public static long GetMaxFileSizeBytes(
        IConfiguration configuration,
        ILogger logger,
        string configKey
    )
    {
        var configuredSize = configuration.GetValue<long?>(configKey);
        if (configuredSize.HasValue)
        {
            // Validate configured value is within safe bounds
            if (configuredSize.Value < MinFileSizeBytes || configuredSize.Value > MaxFileSizeBytes)
            {
                logger.LogWarning(
                    "Configured {ConfigKey} ({ConfiguredSize}) is outside safe bounds ({Min}-{Max}). Using default: {Default}",
                    configKey,
                    configuredSize.Value,
                    MinFileSizeBytes,
                    MaxFileSizeBytes,
                    DefaultFileSizeBytes
                );
                return DefaultFileSizeBytes;
            }

            return configuredSize.Value;
        }

        return DefaultFileSizeBytes;
    }
}
