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
    /// Add Frontend configuration using default settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFrontendConfiguration(this IServiceCollection services)
    {
        // Create FrontendOptions from environment variables or defaults
        var frontendOptions = FrontendConfiguration.CreateFrontendOptions();

        // Register as singleton for direct injection
        services.AddSingleton(frontendOptions);

        // Also register as IOptions<FrontendOptions> for backward compatibility
        // Must copy values explicitly, not just assign references
        services.Configure<FrontendOptions>(options =>
        {
            options.Client.BaseUrl = frontendOptions.Client.BaseUrl;
            options.Admin.BaseUrl = frontendOptions.Admin.BaseUrl;
            options.Default.BaseUrl = frontendOptions.Default.BaseUrl;
            options.HostMap = new Dictionary<string, string>(frontendOptions.HostMap);
        });

        return services;
    }
}
