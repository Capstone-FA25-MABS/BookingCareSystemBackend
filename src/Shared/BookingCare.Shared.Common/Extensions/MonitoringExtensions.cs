using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extensions for configuring monitoring, metrics, and distributed tracing
/// </summary>
public static class MonitoringExtensions
{
    private static readonly ActivitySource ActivitySource = new("BookingCare");
    private static readonly Meter Meter = new("BookingCare", "1.0.0");

    // Custom metrics
    public static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "bookingcare_requests_total",
        "Total number of requests");

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "bookingcare_request_duration_seconds",
        "Request duration in seconds");

    public static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
        "bookingcare_errors_total",
        "Total number of errors");

    /// <summary>
    /// Adds monitoring services including OpenTelemetry, Prometheus metrics, and Jaeger tracing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">The name of the service for telemetry</param>
    /// <param name="serviceVersion">The version of the service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBookingCareMonitoring(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        // Add OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("environment", "development"),
                    new KeyValuePair<string, object>("service.namespace", "BookingCare")
                }))
            .WithTracing(tracing => tracing
                .AddSource("BookingCare")
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) =>
                    {
                        // Don't trace health checks and metrics endpoints
                        var path = httpContext.Request.Path.Value?.ToLower();
                        return path != "/health" && 
                               path != "/metrics" && 
                               !path?.StartsWith("/swagger") == true;
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.RecordException = true;
                })
                .AddJaegerExporter(options =>
                {
                    options.AgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST") ?? "localhost";
                    options.AgentPort = int.Parse(Environment.GetEnvironmentVariable("JAEGER_AGENT_PORT") ?? "6831");
                }))
            .WithMetrics(metrics => metrics
                .AddMeter("BookingCare")
                .AddMeter(serviceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());

        // Add Prometheus metrics
        services.AddSingleton(ActivitySource);
        services.AddSingleton(Meter);

        return services;
    }

    /// <summary>
    /// Configures monitoring middleware and endpoints
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseBookingCareMonitoring(this IApplicationBuilder app)
    {
        // Add Prometheus metrics endpoint
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetrics(); // /metrics endpoint for Prometheus
        });

        // Add custom metrics middleware
        app.UseMiddleware<MetricsMiddleware>();

        return app;
    }

    /// <summary>
    /// Creates a new activity for distributed tracing
    /// </summary>
    /// <param name="name">The name of the activity</param>
    /// <returns>The created activity</returns>
    public static Activity? StartActivity(string name)
    {
        return ActivitySource.StartActivity(name);
    }

    /// <summary>
    /// Records a custom metric for request count
    /// </summary>
    /// <param name="endpoint">The endpoint that was called</param>
    /// <param name="method">The HTTP method</param>
    /// <param name="statusCode">The HTTP status code</param>
    public static void RecordRequest(string endpoint, string method, int statusCode)
    {
        RequestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint),
                              new KeyValuePair<string, object?>("method", method),
                              new KeyValuePair<string, object?>("status_code", statusCode));
    }

    /// <summary>
    /// Records a custom metric for request duration
    /// </summary>
    /// <param name="endpoint">The endpoint that was called</param>
    /// <param name="method">The HTTP method</param>
    /// <param name="duration">The duration in seconds</param>
    public static void RecordRequestDuration(string endpoint, string method, double duration)
    {
        RequestDuration.Record(duration, new KeyValuePair<string, object?>("endpoint", endpoint),
                                        new KeyValuePair<string, object?>("method", method));
    }

    /// <summary>
    /// Records a custom metric for errors
    /// </summary>
    /// <param name="service">The service where the error occurred</param>
    /// <param name="errorType">The type of error</param>
    public static void RecordError(string service, string errorType)
    {
        ErrorCounter.Add(1, new KeyValuePair<string, object?>("service", service),
                           new KeyValuePair<string, object?>("error_type", errorType));
    }
}

/// <summary>
/// Middleware for collecting custom metrics
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;

            // Record metrics
            MonitoringExtensions.RecordRequest(endpoint, method, statusCode);
            MonitoringExtensions.RecordRequestDuration(endpoint, method, duration);

            // Record errors for 4xx and 5xx status codes
            if (statusCode >= 400)
            {
                var errorType = statusCode >= 500 ? "server_error" : "client_error";
                MonitoringExtensions.RecordError("api", errorType);
            }

            _logger.LogInformation(
                "Request {Method} {Path} completed with {StatusCode} in {Duration}ms",
                method, endpoint, statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
