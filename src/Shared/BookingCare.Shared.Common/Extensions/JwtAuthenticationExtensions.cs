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
        var (secretKey, issuer, audience) = GetAndValidateJwtConfiguration(configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = requireHttpsMetadata;
            ConfigureJwtBearerOptions(options, secretKey, issuer, audience);
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
        var (secretKey, issuer, audience) = GetAndValidateJwtConfiguration(configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            ConfigureJwtBearerOptions(options, secretKey, issuer, audience);
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

    /// <summary>
    /// Get and validate JWT configuration from centralized source
    /// </summary>
    private static (string secretKey, string issuer, string audience) GetAndValidateJwtConfiguration(IConfiguration? configuration)
    {
        var (secretKey, issuer, audience, _, _) = configuration != null
            ? JwtConfiguration.GetConfiguration(configuration)
            : JwtConfiguration.GetConfiguration();

        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured. Please set JWT_SECRET_KEY environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(issuer))
            throw new InvalidOperationException("JWT Issuer is not configured. Please set JWT_ISSUER environment variable or configure in appsettings.json");

        if (string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT Audience is not configured. Please set JWT_AUDIENCE environment variable or configure in appsettings.json");

        return (secretKey, issuer, audience);
    }

    /// <summary>
    /// Configure JWT Bearer options with token validation and SignalR support
    /// </summary>
    private static void ConfigureJwtBearerOptions(JwtBearerOptions options, string secretKey, string issuer, string audience)
    {
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

        // Allow SignalR to receive token from query string or Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;

                // Check if this is a SignalR hub request
                if (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/noti-hubs"))
                {
                    // Try to get token from query string first (for WebSocket connections)
                    var accessToken = context.Request.Query["access_token"].ToString();

                    // If not in query string, try Authorization header (for negotiate/long polling)
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            accessToken = authHeader.Substring("Bearer ".Length).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }

                return Task.CompletedTask;
            }
        };
    }
}

