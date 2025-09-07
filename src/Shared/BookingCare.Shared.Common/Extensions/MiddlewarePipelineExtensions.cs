using Microsoft.AspNetCore.Builder;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for middleware pipeline configuration
/// </summary>
public static class MiddlewarePipelineExtensions
{
    /// <summary>
    /// Configure standard authentication and authorization middleware pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseStandardAuthPipeline(this IApplicationBuilder app)
    {
        app.UseAutoToken(); // Custom middleware to attach token from cookies
        app.UseRouting(); // Must be before UseAuthentication and UseAuthorization
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

