using Prometheus;
using Prometheus.SystemMetrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Middleware;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for configuring Prometheus metrics in BookingCare services
/// </summary>
public static class PrometheusMetricsExtensions
{
    /// <summary>
    /// Add Prometheus metrics collection to the service container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Name of the service for metrics labeling</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBookingCareMetrics(
        this IServiceCollection services, 
        string serviceName)
    {
        // Add system metrics
        services.AddSystemMetrics();
        
        // Register metrics service
        services.AddSingleton<IBookingCareMetrics>(provider => 
            new BookingCareMetrics(serviceName));
            
        return services;
    }

    /// <summary>
    /// Configure Prometheus metrics middleware
    /// </summary>
    /// <param name="app">Web application</param>
    /// <param name="enableMetrics">Whether metrics are enabled (from environment)</param>
    /// <returns>Web application for chaining</returns>
    public static WebApplication UseBookingCareMetrics(
        this WebApplication app, 
        bool enableMetrics = true)
    {
        if (!enableMetrics) return app;

        // Add HTTP metrics middleware
        app.UseHttpMetrics();

        // Expose metrics endpoint
        app.MapMetrics("/metrics");

        return app;
    }

    /// <summary>
    /// Check if metrics are enabled from environment variables
    /// </summary>
    public static bool IsMetricsEnabled()
    {
        return Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS")?.ToLowerInvariant() == "true";
    }
}