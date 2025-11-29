using BookingCare.Shared.Cache.Abstractions;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Cache.Services;

/// <summary>
/// No-operation cache service for when caching is disabled
/// </summary>
public class NoCacheService : ICacheService
{
    private readonly ILogger<NoCacheService> _logger;

    public NoCacheService(ILogger<NoCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _logger.LogDebug("Cache is disabled. GetAsync called for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        _logger.LogDebug("Cache is disabled. SetAsync called for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _logger.LogDebug("Cache is disabled. RemoveAsync called for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogDebug("Cache is disabled. RemoveByPatternAsync called for pattern: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        _logger.LogDebug("Cache is disabled. ExistsAsync called for key: {Key}", key);
        return Task.FromResult(false);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
    {
        _logger.LogDebug("Cache is disabled. GetOrSetAsync called for key: {Key}, executing factory", key);
        return await factory();
    }

    public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        _logger.LogDebug("Cache is disabled. GetKeysByPatternAsync called for pattern: {Pattern}", pattern);
        return Task.FromResult(Enumerable.Empty<string>());
    }
}