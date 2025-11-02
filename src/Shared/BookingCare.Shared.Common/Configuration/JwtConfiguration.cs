namespace BookingCare.Shared.Common.Configuration;

/// <summary>
/// Centralized JWT configuration for all services
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// Default JWT Secret Key for all services
    /// In production, this should be stored in environment variables or secure key vault
    /// </summary>
    public const string DefaultSecretKey = "***";

    /// <summary>
    /// Default JWT Issuer for all services
    /// </summary>
    public const string DefaultIssuer = "BookingCare.Auth";

    /// <summary>
    /// Default JWT Audience for all services
    /// </summary>
    public const string DefaultAudience = "BookingCare.API";

    /// <summary>
    /// Default access token expiration in minutes
    /// </summary>
    public const int DefaultAccessTokenExpirationMinutes = 60;

    /// <summary>
    /// Default refresh token expiration in days
    /// </summary>
    public const int DefaultRefreshTokenExpirationDays = 7;

    /// <summary>
    /// Get JWT configuration from environment variables or use defaults
    /// </summary>
    /// <returns>JWT configuration values</returns>
    public static (string SecretKey, string Issuer, string Audience, int AccessTokenExpirationMinutes, int RefreshTokenExpirationDays) GetConfiguration()
    {
        return (
            SecretKey: Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? DefaultSecretKey,
            Issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? DefaultIssuer,
            Audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? DefaultAudience,
            AccessTokenExpirationMinutes: int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var accessExp) ? accessExp : DefaultAccessTokenExpirationMinutes,
            RefreshTokenExpirationDays: int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS"), out var refreshExp) ? refreshExp : DefaultRefreshTokenExpirationDays
        );
    }

    /// <summary>
    /// Get JWT configuration from IConfiguration with fallback to defaults
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>JWT configuration values</returns>
    public static (string SecretKey, string Issuer, string Audience, int AccessTokenExpirationMinutes, int RefreshTokenExpirationDays) GetConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return (
            SecretKey: configuration["Jwt:SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? DefaultSecretKey,
            Issuer: configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? DefaultIssuer,
            Audience: configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? DefaultAudience,
            AccessTokenExpirationMinutes: int.TryParse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var accessExp) ? accessExp : DefaultAccessTokenExpirationMinutes,
            RefreshTokenExpirationDays: int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"] ?? Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS"), out var refreshExp) ? refreshExp : DefaultRefreshTokenExpirationDays
        );
    }
}