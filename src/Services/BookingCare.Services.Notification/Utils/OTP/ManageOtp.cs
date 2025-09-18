using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using System.Security.Cryptography;
using System.Text;

namespace BookingCare.Services.Notification.Utils.OTP;

/// <summary>
/// Data model for storing OTP hash in cache
/// </summary>
public class OtpData
{
    public string Hash { get; set; } = string.Empty;
}

/// <summary>
/// Data model for storing flag values in cache
/// </summary>
public class FlagData
{
    public string Value { get; set; } = string.Empty;
}

public class ManageOtp
{
    private readonly ICacheService _cacheService;

    public ManageOtp(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public static string GenerateNumericOtp(int length = 6)
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
        var otpData = new OtpData { Hash = Hash(otp) };
        await _cacheService.SetAsync(cacheKey, otpData, ttl);
    }

    public async Task<bool> VerifyOtpAsync(string key, string otp)
    {
        var cacheKey = GetCacheKey(key);
        var otpData = await _cacheService.GetAsync<OtpData>(cacheKey);

        if (otpData?.Hash == null)
        {
            return false;
        }

        var providedHash = Hash(otp);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(otpData.Hash),
            Encoding.UTF8.GetBytes(providedHash));

        if (isValid)
        {
            await _cacheService.RemoveAsync(cacheKey);
            return true;
        }

        return false;
    }

    // Removed legacy helpers; use namespaced keys with purpose + subject instead

    private static string GetCacheKey(string key) => CacheKeys.Format(CacheKeys.OtpByKey, key.ToLowerInvariant());

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
        var flagData = new FlagData { Value = "1" };
        await _cacheService.SetAsync(cacheKey, flagData, ttl);
    }

}

