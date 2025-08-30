using BookingCare.Shared.Common.CircuitBreaker;
using BookingCare.Shared.Common.Exceptions;
using System.Diagnostics;

namespace BookingCare.Services.Discount.Middlewares;

public class CircuitBreakerMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CircuitBreakerMonitoringMiddleware> _logger;

    public CircuitBreakerMonitoringMiddleware(
        RequestDelegate next,
        ILogger<CircuitBreakerMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogWarning("Circuit breaker triggered for {Method} {Path}: {Message}", 
                method, path, ex.Message);
            
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "SERVICE_UNAVAILABLE",
                message = "Service is temporarily unavailable due to circuit breaker",
                details = ex.Message,
                timestamp = DateTime.UtcNow,
                path = path
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("Operation timeout for {Method} {Path}: {Message}", 
                method, path, ex.Message);
            
            context.Response.StatusCode = 408;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "REQUEST_TIMEOUT",
                message = "Request timed out",
                details = ex.Message,
                timestamp = DateTime.UtcNow,
                path = path
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
        finally
        {
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 5000) // Log slow requests
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms", 
                    method, path, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
