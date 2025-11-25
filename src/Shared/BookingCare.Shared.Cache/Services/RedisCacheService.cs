using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BookingCare.Shared.Cache.Services;

/// <summary>
/// Redis implementation of ICacheService
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _database = connectionMultiplexer.GetDatabase(_options.Database);
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping get operation for key: {Key}", key);
            return null;
        }

        try
        {
            var cacheKey = GetCacheKey(key);
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", cacheKey);
                return null;
            }

            var deserializedValue = JsonConvert.DeserializeObject<T>(cachedValue);
            _logger.LogDebug("Cache hit for key: {Key}", cacheKey);
            return deserializedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping set operation for key: {Key}", key);
            return;
        }

        try
        {
            var cacheKey = GetCacheKey(key);
            var serializedValue = JsonConvert.SerializeObject(value);

            var distributedCacheOptions = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                distributedCacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                distributedCacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.DefaultExpirationInMinutes));
            }

            await _distributedCache.SetStringAsync(cacheKey, serializedValue, distributedCacheOptions);
            _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}",
                cacheKey, expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationInMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping remove operation for key: {Key}", key);
            return;
        }

        try
        {
            var cacheKey = GetCacheKey(key);
            await _distributedCache.RemoveAsync(cacheKey);
            _logger.LogDebug("Cache removed for key: {Key}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping remove by pattern operation for pattern: {Pattern}", pattern);
            return;
        }

        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            var cachePattern = GetCacheKey(pattern);

            var keys = server.Keys(_options.Database, cachePattern).ToArray();

            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }

            _logger.LogDebug("Cache removed for pattern: {Pattern}", cachePattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache values for pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            var cacheKey = GetCacheKey(key);
            var exists = await _database.KeyExistsAsync(cacheKey);
            _logger.LogDebug("Cache existence check for key: {Key}, exists: {Exists}", cacheKey, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
    {
        var cachedValue = await GetAsync<T>(key);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();

        if (value != null)
        {
            await SetAsync(key, value, expiration);
        }

        return value;
    }

    public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping get keys by pattern operation for pattern: {Pattern}", pattern);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            var cachePattern = GetCacheKey(pattern);

            var keys = server.Keys(_options.Database, cachePattern).Select(k => k.ToString()).ToList();

            _logger.LogDebug("Found {Count} keys matching pattern: {Pattern}", keys.Count, cachePattern);
            return Task.FromResult<IEnumerable<string>>(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys for pattern: {Pattern}", pattern);
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    private string GetCacheKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }
}