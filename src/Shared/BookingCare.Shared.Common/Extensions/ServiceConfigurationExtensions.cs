using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for complete service configuration
/// </summary>
public static class ServiceConfigurationExtensions
{
    /// <summary>
    /// Add complete JWT authentication and authorization setup for a service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="environment">Environment (for HTTPS metadata configuration)</param>
    /// <param name="useDynamicAuthorization">Whether to use dynamic authorization (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthAndAuthorization(this IServiceCollection services, IHostEnvironment environment, bool useDynamicAuthorization = true)
    {
        return services.AddJwtAuthAndAuthorization(configuration: null, environment, useDynamicAuthorization);
    }

    /// <summary>
    /// Add complete JWT authentication and authorization setup for a service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (optional, will use centralized config if not provided)</param>
    /// <param name="environment">Environment (optional, for HTTPS metadata configuration)</param>
    /// <param name="useDynamicAuthorization">Whether to use dynamic authorization (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthAndAuthorization(this IServiceCollection services, IConfiguration? configuration = null, IHostEnvironment? environment = null, bool useDynamicAuthorization = true)
    {
        // Add JWT Authentication using centralized configuration
        if (environment != null && configuration != null)
        {
            services.AddJwtAuthentication(configuration, environment);
        }
        else
        {
            var requireHttpsMetadata = environment?.IsDevelopment() == false;
            services.AddJwtAuthentication(configuration, requireHttpsMetadata);
        }

        // Add Authorization
        if (useDynamicAuthorization)
        {
            services.AddFullAuthorization();
        }
        else
        {
            services.AddStandardAuthorization();
        }

        // Add Frontend configuration for AutoToken middleware
        services.AddFrontendConfiguration(configuration);

        return services;
    }

    /// <summary>
    /// Add JWT authentication and authorization with custom JWT configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (optional, will use centralized config if not provided)</param>
    /// <param name="configureJwt">Action to configure JWT options</param>
    /// <param name="useDynamicAuthorization">Whether to use dynamic authorization (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthAndAuthorization(this IServiceCollection services, IConfiguration? configuration, Action<JwtBearerOptions> configureJwt, bool useDynamicAuthorization = true)
    {
        // Add JWT Authentication with custom configuration using centralized config
        services.AddJwtAuthentication(configuration, configureJwt);

        // Add Authorization
        if (useDynamicAuthorization)
        {
            services.AddFullAuthorization();
        }
        else
        {
            services.AddStandardAuthorization();
        }

        // Add Frontend configuration for AutoToken middleware
        services.AddFrontendConfiguration(configuration);

        return services;
    }
}

