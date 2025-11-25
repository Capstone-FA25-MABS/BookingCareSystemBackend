namespace BookingCare.Shared.Cache.Abstractions;

/// <summary>
/// Interface for caching operations in the BookingCare system
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value by key
    /// </summary>
    /// <typeparam name="T">Type of cached object</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set cache value with expiration
    /// </summary>
    /// <typeparam name="T">Type of object to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Expiration time (optional)</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Remove cached value by key
    /// </summary>
    /// <param name="key">Cache key</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove multiple cached values by pattern
    /// </summary>
    /// <param name="pattern">Pattern to match keys (e.g., "user:*")</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Get or set cached value using a factory function
    /// </summary>
    /// <typeparam name="T">Type of cached object</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Function to generate value if not cached</param>
    /// <param name="expiration">Expiration time (optional)</param>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Get all keys matching a pattern
    /// </summary>
    /// <param name="pattern">Pattern to match keys (e.g., "user:*")</param>
    /// <returns>List of matching keys</returns>
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);
}