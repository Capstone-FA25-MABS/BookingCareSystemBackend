using BookingCare.Shared.Common.Filters;
using BookingCare.Shared.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for configuring exception handling
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds exception handling services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        // Add the global exception filter
        services.AddScoped<GlobalExceptionFilter>();
        
        // Configure MVC options to include the global exception filter
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            options.Filters.Add<GlobalExceptionFilter>();
        });

        // Add custom validation behavior for consistent API responses
        services.AddCustomValidation();

        return services;
    }

    /// <summary>
    /// Configures the global exception handling middleware
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }
}

/// <summary>
/// Extension methods for HTTP context
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the correlation ID from the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The correlation ID</returns>
    public static string GetCorrelationId(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        return context.TraceIdentifier;
    }

    /// <summary>
    /// Gets the user ID from the HTTP context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The user ID or "Anonymous" if not authenticated</returns>
    public static string GetUserId(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        return context.User?.Identity?.Name ?? "Anonymous";
    }
}
