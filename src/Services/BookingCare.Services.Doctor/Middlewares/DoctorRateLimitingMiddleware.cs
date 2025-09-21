using System.Collections.Concurrent;

namespace BookingCare.Services.Doctor.Middlewares;

public class DoctorRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DoctorRateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore;
    private readonly int _maxRequestsPerMinute;
    private readonly int _maxRequestsPerHour;

    public DoctorRateLimitingMiddleware(RequestDelegate next, ILogger<DoctorRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _rateLimitStore = new ConcurrentDictionary<string, RateLimitInfo>();
        _maxRequestsPerMinute = 100; // 100 requests per minute per IP
        _maxRequestsPerHour = 1000;  // 1000 requests per hour per IP
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var endpoint = context.Request.Path.Value;

        // Skip rate limiting for health checks and swagger
        if (ShouldSkipRateLimiting(endpoint))
        {
            await _next(context);
            return;
        }

        if (!IsRateLimitExceeded(clientIp, endpoint))
        {
            // Update rate limit counters
            UpdateRateLimit(clientIp, endpoint);
            await _next(context);
        }
        else
        {
            // Rate limit exceeded
            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp} on endpoint: {Endpoint}", clientIp, endpoint);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please try again later.",
                retryAfter = GetRetryAfterSeconds(clientIp)
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool ShouldSkipRateLimiting(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return true;

        return endpoint.Contains("/health") ||
               endpoint.Contains("/swagger") ||
               endpoint.Contains("/favicon.ico");
    }

    private bool IsRateLimitExceeded(string clientIp, string? endpoint)
    {
        var key = $"{clientIp}:{endpoint}";

        if (!_rateLimitStore.TryGetValue(key, out var rateLimitInfo))
        {
            return false; // No previous requests, allow
        }

        var now = DateTime.UtcNow;

        // Check minute limit
        if (rateLimitInfo.MinuteRequests.Count > 0)
        {
            // Remove requests older than 1 minute
            rateLimitInfo.MinuteRequests.RemoveAll(time => now - time > TimeSpan.FromMinutes(1));

            if (rateLimitInfo.MinuteRequests.Count >= _maxRequestsPerMinute)
            {
                return true;
            }
        }

        // Check hour limit
        if (rateLimitInfo.HourRequests.Count > 0)
        {
            // Remove requests older than 1 hour
            rateLimitInfo.HourRequests.RemoveAll(time => now - time > TimeSpan.FromHours(1));

            if (rateLimitInfo.HourRequests.Count >= _maxRequestsPerHour)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateRateLimit(string clientIp, string? endpoint)
    {
        var key = $"{clientIp}:{endpoint}";
        var now = DateTime.UtcNow;

        _rateLimitStore.AddOrUpdate(key,
            new RateLimitInfo
            {
                MinuteRequests = new List<DateTime> { now },
                HourRequests = new List<DateTime> { now }
            },
            (existingKey, existingInfo) =>
            {
                existingInfo.MinuteRequests.Add(now);
                existingInfo.HourRequests.Add(now);
                return existingInfo;
            });
    }

    private int GetRetryAfterSeconds(string _)
    {
        // Return retry after time based on rate limit type
        // For simplicity, return 60 seconds
        return 60;
    }

    // Cleanup old entries periodically
    public void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _rateLimitStore)
        {
            var rateLimitInfo = kvp.Value;

            // Remove old minute requests
            rateLimitInfo.MinuteRequests.RemoveAll(time => now - time > TimeSpan.FromMinutes(1));

            // Remove old hour requests
            rateLimitInfo.HourRequests.RemoveAll(time => now - time > TimeSpan.FromHours(1));

            // If no requests left, mark for removal
            if (rateLimitInfo.MinuteRequests.Count == 0 && rateLimitInfo.HourRequests.Count == 0)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        // Remove empty entries
        foreach (var key in keysToRemove)
        {
            _rateLimitStore.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public List<DateTime> MinuteRequests { get; set; } = new();
        public List<DateTime> HourRequests { get; set; } = new();
    }
}
