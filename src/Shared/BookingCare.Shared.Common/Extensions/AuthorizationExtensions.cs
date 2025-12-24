using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using BookingCare.Shared.Common.Authorization;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for Authorization configuration
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Add standard role-based authorization policies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStandardAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Role:Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("Role:Clinic", policy => policy.RequireRole("Clinic"));
            options.AddPolicy("Role:Doctor", policy => policy.RequireRole("Doctor"));
            options.AddPolicy("Role:Patient", policy => policy.RequireRole("Patient"));
            options.AddPolicy("Role:Admin,Role:Clinic", policy => policy.RequireRole("Admin", "Clinic"));
            options.AddPolicy("Role:Doctor,Role:Patient", policy => policy.RequireRole("Doctor", "Patient"));
            // Add other standard role policies as needed
        });
        return services;
    }

    /// <summary>
    /// Add dynamic authorization with policy provider and handlers
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDynamicAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(); // Required for authorization services
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleHandler>();
        services.AddSingleton<IAuthorizationHandler, MultipleRolesHandler>();
        return services;
    }

    /// <summary>
    /// Add both standard and dynamic authorization
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFullAuthorization(this IServiceCollection services)
    {
        services.AddStandardAuthorization();
        services.AddDynamicAuthorization();
        return services;
    }
}

