using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookingCare.Shared.Common.Configuration;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for JWT Authentication configuration
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Add JWT Authentication with centralized configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (optional, will use centralized config if not provided)</param>
    /// <param name="requireHttpsMetadata">Whether to require HTTPS metadata (default: false for development)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration? configuration = null, bool requireHttpsMetadata = false)
    {
        // Get JWT configuration from centralized source
        var (secretKey, issuer, audience, _, _) = configuration != null
            ? JwtConfiguration.GetConfiguration(configuration)
            : JwtConfiguration.GetConfiguration();

        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured. Please set JWT_SECRET_KEY environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(issuer))
            throw new InvalidOperationException("JWT Issuer is not configured. Please set JWT_ISSUER environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT Audience is not configured. Please set JWT_AUDIENCE environment variable or configure in appsettings.json");

        // Add Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = requireHttpsMetadata;

            // Configure token validation
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

        return services;
    }

    /// <summary>
    /// Add JWT Authentication with custom validation parameters
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (optional, will use centralized config if not provided)</param>
    /// <param name="configureOptions">Action to configure additional JWT options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration? configuration, Action<JwtBearerOptions> configureOptions)
    {
        // Get JWT configuration from centralized source
        var (secretKey, issuer, audience, _, _) = configuration != null
            ? JwtConfiguration.GetConfiguration(configuration)
            : JwtConfiguration.GetConfiguration();

        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured. Please set JWT_SECRET_KEY environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(issuer))
            throw new InvalidOperationException("JWT Issuer is not configured. Please set JWT_ISSUER environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT Audience is not configured. Please set JWT_AUDIENCE environment variable or configure in appsettings.json");

        // Add Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Set default validation parameters
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            // Apply custom configuration
            configureOptions(options);
        });

        return services;
    }

    /// <summary>
    /// Add JWT Authentication with environment-specific configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="environment">Environment name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var requireHttpsMetadata = !environment.IsDevelopment();
        return services.AddJwtAuthentication(configuration, requireHttpsMetadata);
    }
}

