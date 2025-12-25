using Microsoft.AspNetCore.Http;
using BookingCare.Shared.Common.Interfaces;
using System.Diagnostics;

namespace BookingCare.Shared.Common.Middleware;

/// <summary>
/// Middleware to automatically track HTTP request metrics
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBookingCareMetrics _metrics;

    public MetricsMiddleware(RequestDelegate next, IBookingCareMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        
        try
        {
            await _next(context);
            
            stopwatch.Stop();
            
            // Record successful request
            var statusCode = context.Response.StatusCode;
            var result = statusCode < 400 ? "success" : "error";
            
            _metrics.IncrementOperation($"http_{method.ToLower()}", result);
            _metrics.RecordDuration($"http_{method.ToLower()}", stopwatch.Elapsed.TotalSeconds);
            
            // Record specific endpoint metrics
            var endpoint = GetEndpointName(path);
            _metrics.IncrementOperation($"endpoint_{endpoint}", result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record error
            _metrics.IncrementError(ex.GetType().Name, $"http_{method.ToLower()}");
            _metrics.RecordDuration($"http_{method.ToLower()}", stopwatch.Elapsed.TotalSeconds);
            
            throw;
        }
    }

    private static string GetEndpointName(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return "root";
            
        // Extract meaningful endpoint name
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length == 0)
            return "root";
            
        // Take first segment as endpoint name
        var endpoint = segments[0].ToLowerInvariant();
        
        // Common API patterns
        return endpoint switch
        {
            "api" when segments.Length > 1 => segments[1].ToLowerInvariant(),
            "health" => "health_check",
            "metrics" => "metrics",
            _ => endpoint
        };
    }
}