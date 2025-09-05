using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Common.Versioning;

/// <summary>
/// Extension methods for API versioning configuration
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Adds API versioning support to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
        {
            // Default API version
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;

            // Version reading strategy
            opt.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(), // /api/v1/users
                new QueryStringApiVersionReader("version"), // ?version=1.0
                new HeaderApiVersionReader("X-Version"), // X-Version: 1.0
                new MediaTypeApiVersionReader("version") // Accept: application/json;version=1.0
            );

            // Version format
            opt.ApiVersionSelector = new CurrentImplementationApiVersionSelector(opt);
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
            setup.AssumeDefaultVersionWhenUnspecified = true;
            setup.DefaultApiVersion = new ApiVersion(1, 0);
        });

        return services;
    }
}
