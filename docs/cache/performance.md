# Performance Monitoring and Best Practices for Redis Caching

## Monitoring Cache Performance

### 1. Cache Metrics Service

```csharp
public class CacheMetricsService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheMetricsService> _logger;
    private static readonly ConcurrentDictionary<string, CacheMetrics> _metrics = new();

    public CacheMetricsService(ICacheService cacheService, ILogger<CacheMetricsService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<T?> GetWithMetricsAsync<T>(string key, string operation = "GET") where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _cacheService.GetAsync<T>(key);
        stopwatch.Stop();

        RecordMetrics(key, operation, result != null ? "HIT" : "MISS", stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    public async Task SetWithMetricsAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        await _cacheService.SetAsync(key, value, expiration);
        stopwatch.Stop();

        RecordMetrics(key, "SET", "SUCCESS", stopwatch.ElapsedMilliseconds);
    }

    private void RecordMetrics(string key, string operation, string result, long duration)
    {
        var keyPrefix = key.Split(':')[0];
        var metricsKey = $"{keyPrefix}:{operation}";
        
        _metrics.AddOrUpdate(metricsKey, 
            new CacheMetrics { Operation = operation, KeyPrefix = keyPrefix },
            (k, existing) =>
            {
                existing.TotalOperations++;
                existing.TotalDuration += duration;
                
                if (result == "HIT") existing.HitCount++;
                else if (result == "MISS") existing.MissCount++;
                
                existing.AverageDuration = existing.TotalDuration / existing.TotalOperations;
                existing.HitRatio = existing.TotalOperations > 0 
                    ? (double)existing.HitCount / (existing.HitCount + existing.MissCount) 
                    : 0;
                
                return existing;
            });

        if (duration > 100) // Log slow operations
        {
            _logger.LogWarning("Slow cache operation: {Operation} {Key} took {Duration}ms", 
                operation, key, duration);
        }
    }

    public CacheMetricsReport GetMetricsReport()
    {
        return new CacheMetricsReport
        {
            GeneratedAt = DateTime.UtcNow,
            Metrics = _metrics.Values.ToList()
        };
    }
}

public class CacheMetrics
{
    public string Operation { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public long TotalOperations { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRatio { get; set; }
    public long TotalDuration { get; set; }
    public double AverageDuration { get; set; }
}

public class CacheMetricsReport
{
    public DateTime GeneratedAt { get; set; }
    public List<CacheMetrics> Metrics { get; set; } = new();
    
    public double OverallHitRatio => Metrics.Sum(m => m.HitCount) / (double)Metrics.Sum(m => m.HitCount + m.MissCount);
    public double AverageResponseTime => Metrics.Average(m => m.AverageDuration);
}
```

### 2. Health Check for Redis

```csharp
public class RedisHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(ICacheService cacheService, ILogger<RedisHealthCheck> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "health:check";
            var testValue = new { Status = "OK", Timestamp = DateTime.UtcNow };
            
            // Test write
            var writeStopwatch = Stopwatch.StartNew();
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            writeStopwatch.Stop();
            
            // Test read
            var readStopwatch = Stopwatch.StartNew();
            var retrieved = await _cacheService.GetAsync<object>(testKey);
            readStopwatch.Stop();
            
            // Test delete
            await _cacheService.RemoveAsync(testKey);
            
            if (retrieved == null)
            {
                return HealthCheckResult.Unhealthy("Redis read operation failed");
            }
            
            var data = new Dictionary<string, object>
            {
                ["write_time_ms"] = writeStopwatch.ElapsedMilliseconds,
                ["read_time_ms"] = readStopwatch.ElapsedMilliseconds
            };
            
            return writeStopwatch.ElapsedMilliseconds < 100 && readStopwatch.ElapsedMilliseconds < 100
                ? HealthCheckResult.Healthy("Redis is healthy", data)
                : HealthCheckResult.Degraded("Redis is slow", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "infrastructure" });
```

### 3. Cache Performance Interceptor

```csharp
public class CachePerformanceInterceptor
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CachePerformanceInterceptor> _logger;

    public CachePerformanceInterceptor(RequestDelegate next, ILogger<CachePerformanceInterceptor> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var cacheStats = new CacheOperationStats();
            context.Items["CacheStats"] = cacheStats;
            
            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            stopwatch.Stop();
            
            LogCachePerformance(context, cacheStats, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            await _next(context);
        }
    }

    private void LogCachePerformance(HttpContext context, CacheOperationStats stats, long totalDuration)
    {
        if (stats.OperationCount > 0)
        {
            _logger.LogInformation(
                "Cache Performance - Path: {Path}, Operations: {Count}, Hits: {Hits}, Misses: {Misses}, " +
                "Hit Ratio: {HitRatio:P2}, Cache Time: {CacheTime}ms, Total Time: {TotalTime}ms",
                context.Request.Path,
                stats.OperationCount,
                stats.HitCount,
                stats.MissCount,
                stats.HitRatio,
                stats.TotalCacheTime,
                totalDuration);
        }
    }
}

public class CacheOperationStats
{
    public int OperationCount { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public long TotalCacheTime { get; set; }
    
    public double HitRatio => OperationCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
}
```

## Best Practices for Performance

### 1. Optimal Expiration Times

```csharp
public static class CacheExpirationStrategy
{
    // User data - moderate frequency of change
    public static TimeSpan UserData => TimeSpan.FromMinutes(30);
    
    // Configuration data - rarely changes
    public static TimeSpan ConfigurationData => TimeSpan.FromHours(6);
    
    // Session data - security sensitive
    public static TimeSpan SessionData => TimeSpan.FromMinutes(20);
    
    // Real-time data - changes frequently
    public static TimeSpan RealTimeData => TimeSpan.FromSeconds(30);
    
    // Static reference data - very rarely changes
    public static TimeSpan ReferenceData => TimeSpan.FromHours(24);
    
    // Search results - moderate freshness needed
    public static TimeSpan SearchResults => TimeSpan.FromMinutes(15);
    
    // Get expiration based on data type and access pattern
    public static TimeSpan GetExpiration(string dataType, AccessPattern pattern)
    {
        return dataType.ToLower() switch
        {
            "user" => pattern == AccessPattern.Frequent ? TimeSpan.FromMinutes(45) : TimeSpan.FromMinutes(30),
            "doctor" => pattern == AccessPattern.Frequent ? TimeSpan.FromHours(2) : TimeSpan.FromMinutes(60),
            "clinic" => pattern == AccessPattern.Frequent ? TimeSpan.FromHours(4) : TimeSpan.FromHours(2),
            "appointment" => TimeSpan.FromMinutes(15), // Always short for appointments
            "configuration" => TimeSpan.FromHours(6),
            _ => TimeSpan.FromMinutes(30)
        };
    }
}

public enum AccessPattern
{
    Rare,
    Normal,
    Frequent,
    VeryFrequent
}
```

### 2. Batch Operations for Better Performance

```csharp
public class BatchCacheOperations
{
    private readonly ICacheService _cacheService;
    
    public async Task<Dictionary<int, User?>> GetMultipleUsersAsync(IEnumerable<int> userIds)
    {
        var results = new Dictionary<int, User?>();
        var cacheMisses = new List<int>();
        
        // Parallel cache lookups
        var cacheTasks = userIds.Select(async id =>
        {
            var key = CacheKeys.Format(CacheKeys.UserById, id);
            var user = await _cacheService.GetAsync<User>(key);
            return new { Id = id, User = user };
        });
        
        var cacheResults = await Task.WhenAll(cacheTasks);
        
        foreach (var result in cacheResults)
        {
            if (result.User != null)
            {
                results[result.Id] = result.User;
            }
            else
            {
                cacheMisses.Add(result.Id);
            }
        }
        
        // Batch load cache misses from database
        if (cacheMisses.Any())
        {
            var dbUsers = await LoadUsersFromDatabaseAsync(cacheMisses);
            
            // Cache the loaded users
            var cacheUpdateTasks = dbUsers.Select(user =>
            {
                var key = CacheKeys.Format(CacheKeys.UserById, user.Id);
                return _cacheService.SetAsync(key, user, CacheExpirationStrategy.UserData);
            });
            
            await Task.WhenAll(cacheUpdateTasks);
            
            // Add to results
            foreach (var user in dbUsers)
            {
                results[user.Id] = user;
            }
        }
        
        return results;
    }
    
    private async Task<List<User>> LoadUsersFromDatabaseAsync(List<int> userIds)
    {
        // Implement batch database loading
        // This is much more efficient than individual database calls
        await Task.Delay(50); // Simulate database call
        return userIds.Select(id => new User { Id = id, Email = $"user{id}@example.com" }).ToList();
    }
}
```

### 3. Memory-Efficient Caching

```csharp
public class MemoryEfficientCacheService
{
    private readonly ICacheService _cacheService;
    
    // Cache only essential data, not full objects
    public async Task CacheUserSummaryAsync(User user)
    {
        var summary = new UserSummary
        {
            Id = user.Id,
            Name = user.FirstName + " " + user.LastName,
            Email = user.Email,
            IsActive = user.IsActive
        };
        
        var key = CacheKeys.Format("user:summary:{0}", user.Id);
        await _cacheService.SetAsync(key, summary, TimeSpan.FromMinutes(60));
    }
    
    // Use compression for large objects
    public async Task CacheLargeDataAsync<T>(string key, T data, TimeSpan expiration) where T : class
    {
        var json = JsonConvert.SerializeObject(data);
        var compressed = CompressString(json);
        
        if (compressed.Length < json.Length * 0.7) // Only cache if compression saves 30%
        {
            await _cacheService.SetAsync($"{key}:compressed", compressed, expiration);
        }
        else
        {
            await _cacheService.SetAsync(key, data, expiration);
        }
    }
    
    private byte[] CompressString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }
}

public class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

### 4. Cache Warming Strategy

```csharp
public class CacheWarmingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmingService> _logger;

    public CacheWarmingService(IServiceProvider serviceProvider, ILogger<CacheWarmingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WarmCacheAsync();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Warm every hour
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warming");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Retry in 10 minutes
            }
        }
    }

    private async Task WarmCacheAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        
        _logger.LogInformation("Starting cache warming");
        
        // Warm popular user data
        var popularUserIds = await userRepository.GetPopularUserIdsAsync();
        var warmingTasks = popularUserIds.Select(async userId =>
        {
            try
            {
                var user = await userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    var key = CacheKeys.Format(CacheKeys.UserById, userId);
                    await cacheService.SetAsync(key, user, CacheExpirationStrategy.UserData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm cache for user {UserId}", userId);
            }
        });
        
        await Task.WhenAll(warmingTasks);
        
        _logger.LogInformation("Cache warming completed for {Count} users", popularUserIds.Count);
    }
}

// Register in Program.cs
builder.Services.AddHostedService<CacheWarmingService>();
```

### 5. Monitoring Dashboard Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
public class CacheMonitoringController : ControllerBase
{
    private readonly CacheMetricsService _metricsService;
    private readonly IConnectionMultiplexer _redis;

    public CacheMonitoringController(CacheMetricsService metricsService, IConnectionMultiplexer redis)
    {
        _metricsService = metricsService;
        _redis = redis;
    }

    [HttpGet("metrics")]
    public IActionResult GetCacheMetrics()
    {
        var report = _metricsService.GetMetricsReport();
        return Ok(report);
    }

    [HttpGet("redis-info")]
    public async Task<IActionResult> GetRedisInfo()
    {
        var database = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        
        var info = await server.InfoAsync();
        var keyCount = await database.ExecuteAsync("DBSIZE");
        
        return Ok(new
        {
            RedisInfo = info.ToStringDictionary(),
            KeyCount = keyCount,
            Memory = info.FirstOrDefault(x => x.Key == "Memory")?.Value,
            Clients = info.FirstOrDefault(x => x.Key == "Clients")?.Value
        });
    }

    [HttpGet("cache-keys/{pattern}")]
    public async Task<IActionResult> GetCacheKeys(string pattern = "*")
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = new List<string>();
        
        await foreach (var key in server.ScanAsync(pattern: pattern, pageSize: 100))
        {
            keys.Add(key);
            if (keys.Count >= 100) break; // Limit results
        }
        
        return Ok(new { Pattern = pattern, Keys = keys, Count = keys.Count });
    }

    [HttpDelete("cache-keys/{pattern}")]
    public async Task<IActionResult> ClearCachePattern(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var database = _redis.GetDatabase();
        var deletedCount = 0;
        
        await foreach (var key in server.ScanAsync(pattern: pattern))
        {
            await database.KeyDeleteAsync(key);
            deletedCount++;
        }
        
        return Ok(new { Pattern = pattern, DeletedCount = deletedCount });
    }
}
```

## Performance Monitoring Checklist

### Daily Monitoring
- [ ] Check cache hit ratio (target: >80%)
- [ ] Monitor average response times
- [ ] Check Redis memory usage
- [ ] Review slow cache operations

### Weekly Monitoring  
- [ ] Analyze cache key distribution
- [ ] Review expiration strategies
- [ ] Check for cache hotspots
- [ ] Monitor background warming jobs

### Monthly Monitoring
- [ ] Performance trend analysis
- [ ] Cache strategy optimization
- [ ] Capacity planning review
- [ ] Cost optimization review

## Alerts and Thresholds

```csharp
public class CacheAlertService
{
    public class CacheThresholds
    {
        public const double MinHitRatio = 0.70; // 70%
        public const double MaxResponseTime = 100; // milliseconds
        public const double MaxMemoryUsage = 0.85; // 85% of max memory
        public const int MaxSlowOperations = 10; // per minute
    }
    
    // Implement alerting logic based on these thresholds
}
```

This comprehensive monitoring setup will help you maintain optimal cache performance and quickly identify issues before they impact your users.
