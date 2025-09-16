using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Configuration;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for Frontend configuration
/// </summary>
public static class FrontendConfigurationExtensions
{
    /// <summary>
    /// Add Frontend configuration using centralized settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (optional, will use centralized config if not provided)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFrontendConfiguration(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Create FrontendOptions from centralized configuration
        var frontendOptions = FrontendConfiguration.CreateFrontendOptions(configuration);

        // Register as singleton
        services.AddSingleton(frontendOptions);

        // Also register as IOptions for backward compatibility
        services.Configure<FrontendOptions>(options =>
        {
            options.Client = frontendOptions.Client;
            options.Admin = frontendOptions.Admin;
            options.Default = frontendOptions.Default;
            options.HostMap = frontendOptions.HostMap;
        });

        return services;
    }

    /// <summary>
    /// Add Frontend configuration with custom options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure FrontendOptions</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFrontendConfiguration(this IServiceCollection services, Action<FrontendOptions> configureOptions)
    {
        // Create default options and apply custom configuration
        var frontendOptions = FrontendConfiguration.CreateFrontendOptions();
        configureOptions(frontendOptions);

        // Register as singleton
        services.AddSingleton(frontendOptions);

        // Also register as IOptions for backward compatibility
        services.Configure<FrontendOptions>(options =>
        {
            options.Client = frontendOptions.Client;
            options.Admin = frontendOptions.Admin;
            options.Default = frontendOptions.Default;
            options.HostMap = frontendOptions.HostMap;
        });

        return services;
    }
}
