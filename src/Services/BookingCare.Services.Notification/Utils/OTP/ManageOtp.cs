using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace BookingCare.Services.Notification.Utils.OTP;

public class ManageOtp
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly bool _useDistributed;
    private bool _forceMemoryFallback;

    public ManageOtp(IMemoryCache memoryCache, IDistributedCache? distributedCache = null, IConfiguration? configuration = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _useDistributed = configuration != null && configuration.GetSection("Redis").GetValue<bool>("Enabled") && distributedCache != null;
        _forceMemoryFallback = false;
    }

    public string GenerateNumericOtp(int length = 6)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append((b % 10).ToString());
        }
        return sb.ToString();
    }

    public async Task StoreOtpAsync(string key, string otp, TimeSpan ttl)
    {
        var cacheKey = GetCacheKey(key);
        var otpHash = Hash(otp);
        if (_useDistributed && _distributedCache != null && !_forceMemoryFallback)
        {
            try
            {
                // Dual-write: also cache in memory with shorter TTL to bridge transient issues
                await _distributedCache.SetStringAsync(cacheKey, otpHash, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
                var shortTtl = ttl < TimeSpan.FromMinutes(2) ? ttl : TimeSpan.FromMinutes(2);
                _memoryCache.Set(cacheKey, otpHash, shortTtl);
                return;
            }
            catch
            {
                // Fallback to memory if Redis is unavailable
                _forceMemoryFallback = true;
            }
        }
        _memoryCache.Set(cacheKey, otpHash, ttl);
    }

    public async Task<bool> VerifyOtpAsync(string key, string otp)
    {
        var cacheKey = GetCacheKey(key);
        string? storedHash = null;
        if (_useDistributed && _distributedCache != null && !_forceMemoryFallback)
        {
            try
            {
                storedHash = await _distributedCache.GetStringAsync(cacheKey);
                if (storedHash != null)
                {
                    var providedHash = Hash(otp);
                    var isValid = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(storedHash), Encoding.UTF8.GetBytes(providedHash));
                    if (isValid)
                    {
                        await _distributedCache.RemoveAsync(cacheKey);
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback to memory if Redis is unavailable
                _forceMemoryFallback = true;
            }
        }
        if (_memoryCache.TryGetValue<string>(cacheKey, out var memHash))
        {
            storedHash = memHash;
            var providedHash = Hash(otp);
            var isValid = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(storedHash!), Encoding.UTF8.GetBytes(providedHash));
            if (isValid)
            {
                _memoryCache.Remove(cacheKey);
                return true;
            }
        }
        return false;
    }

    // Removed legacy helpers; use namespaced keys with purpose + subject instead

    private static string GetCacheKey(string key) => $"otp:{key.ToLowerInvariant()}";

    private static string Hash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    // Flags (non-OTP) for verification handshakes
    public async Task SetFlagAsync(string key, TimeSpan ttl)
    {
        var cacheKey = GetCacheKey(key);

        if (_useDistributed && _distributedCache != null && !_forceMemoryFallback)
        {
            try
            {
                // Dual-write: also cache in memory with shorter TTL to bridge transient issues
                await _distributedCache.SetStringAsync(cacheKey, "1", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
                var shortTtl = ttl < TimeSpan.FromMinutes(2) ? ttl : TimeSpan.FromMinutes(2);
                _memoryCache.Set(cacheKey, "1", shortTtl);
                return;
            }
            catch
            {
                // Fallback to memory if Redis is unavailable
                _forceMemoryFallback = true;
            }
        }
        _memoryCache.Set(cacheKey, "1", ttl);
    }

    public async Task<bool> CheckAndConsumeFlagAsync(string key)
    {
        var cacheKey = GetCacheKey(key);
        if (_useDistributed && _distributedCache != null)
        {
            var val = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(val))
            {
                await _distributedCache.RemoveAsync(cacheKey);
                return true;
            }
            return false;
        }
        else
        {
            if (_memoryCache.TryGetValue<string>(cacheKey, out var val) && !string.IsNullOrEmpty(val))
            {
                _memoryCache.Remove(cacheKey);
                return true;
            }
            return false;
        }
    }

    // Fallback controls for BackgroundService
    public bool IsMemoryFallbackEnabled() => _forceMemoryFallback;
    public void DisableMemoryFallback() => _forceMemoryFallback = false;
}


