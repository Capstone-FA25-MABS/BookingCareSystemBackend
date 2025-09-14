namespace BookingCare.Shared.Cache.Options;

/// <summary>
/// Configuration options for Redis cache
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";
    
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";
    
    /// <summary>
    /// Default expiration time for cached items (in minutes)
    /// </summary>
    public int DefaultExpirationInMinutes { get; set; } = 60;
    
    /// <summary>
    /// Key prefix for all cache keys to avoid conflicts
    /// </summary>
    public string KeyPrefix { get; set; } = "BookingCare:";
    
    /// <summary>
    /// Database number for Redis (0-15)
    /// </summary>
    public int Database { get; set; } = 0;
    
    /// <summary>
    /// Enable/disable caching globally
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Retry count for failed operations
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectTimeout { get; set; } = 5;
    
    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 5;
}
