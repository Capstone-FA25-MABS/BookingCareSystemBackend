using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookingCare.Services.Notification.Utils.OTP;

namespace BookingCare.Services.Notification.Services;

public class RedisHealthBackgroundService : BackgroundService
{
    private readonly ILogger<RedisHealthBackgroundService> _logger;
    private readonly IDistributedCache? _distributedCache;
    private readonly ManageOtp _otpManager;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public RedisHealthBackgroundService(
        ILogger<RedisHealthBackgroundService> logger,
        ManageOtp otpManager,
        IDistributedCache? distributedCache = null)
    {
        _logger = logger;
        _otpManager = otpManager;
        _distributedCache = distributedCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Only attempt if currently in memory fallback and distributed cache is available
                if (_distributedCache != null && _otpManager.IsMemoryFallbackEnabled())
                {
                    var probeKey = $"otp:probe:{Guid.NewGuid()}";
                    await _distributedCache.SetStringAsync(probeKey, "1", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                    }, stoppingToken);
                    var val = await _distributedCache.GetStringAsync(probeKey, stoppingToken);
                    if (val == "1")
                    {
                        _logger.LogInformation("Redis reconnected. Disabling memory fallback for OTP manager.");
                        _otpManager.DisableMemoryFallback();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Redis probe failed; remaining in memory fallback mode.");
            }

            try { await Task.Delay(_interval, stoppingToken); } catch { }
        }
    }
}


