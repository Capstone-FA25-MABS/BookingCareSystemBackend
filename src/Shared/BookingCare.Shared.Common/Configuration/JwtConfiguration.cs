namespace BookingCare.Shared.Common.Configuration;

/// <summary>
/// Centralized JWT configuration for all services
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// Default JWT Issuer for all services (non-sensitive)
    /// </summary>
    public const string DefaultIssuer = "BookingCare.Auth";

    /// <summary>
    /// Default JWT Audience for all services (non-sensitive)
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
    /// Get JWT configuration from environment variables
    /// SECURITY: JWT_SECRET_KEY or JWT_KEY must be set in environment variables
    /// Supports both formats: JWT_KEY and Jwt__SecretKey (ASP.NET Core convention)
    /// </summary>
    /// <returns>JWT configuration values</returns>
    /// <exception cref="InvalidOperationException">Thrown when JWT_SECRET_KEY or JWT_KEY is not set</exception>
    public static (string SecretKey, string Issuer, string Audience, int AccessTokenExpirationMinutes, int RefreshTokenExpirationDays) GetConfiguration()
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                       ?? Environment.GetEnvironmentVariable("JWT_KEY")
                       ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")  // ASP.NET Core format
                       ?? Environment.GetEnvironmentVariable("Jwt__Key");       // ASP.NET Core format

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT Secret Key is not configured. Please set JWT_SECRET_KEY, JWT_KEY, Jwt__SecretKey, or Jwt__Key environment variable. " +
                "For security reasons, hard-coded secrets are not allowed in production.");
        }

        return (
            SecretKey: secretKey,
            Issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? Environment.GetEnvironmentVariable("Jwt__Issuer") ?? DefaultIssuer,
            Audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? Environment.GetEnvironmentVariable("Jwt__Audience") ?? DefaultAudience,
            AccessTokenExpirationMinutes: int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES") ?? Environment.GetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes"), out var accessExp) ? accessExp : DefaultAccessTokenExpirationMinutes,
            RefreshTokenExpirationDays: int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS") ?? Environment.GetEnvironmentVariable("Jwt__RefreshTokenExpirationDays"), out var refreshExp) ? refreshExp : DefaultRefreshTokenExpirationDays
        );
    }

    /// <summary>
    /// Get JWT configuration from IConfiguration with fallback to environment variables
    /// SECURITY: Environment variables take PRIORITY over appsettings.json
    /// Supports both formats: JWT_KEY and Jwt__SecretKey (ASP.NET Core convention)
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>JWT configuration values</returns>
    /// <exception cref="InvalidOperationException">Thrown when JWT Secret Key is not configured</exception>
    public static (string SecretKey, string Issuer, string Audience, int AccessTokenExpirationMinutes, int RefreshTokenExpirationDays) GetConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // PRIORITY ORDER: Environment Variables > appsettings.json
        // This ensures docker-compose env vars override hard-coded values
        // Supports both JWT_KEY and Jwt__SecretKey (ASP.NET Core double-underscore convention)
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                       ?? Environment.GetEnvironmentVariable("JWT_KEY")
                       ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")  // ASP.NET Core format
                       ?? Environment.GetEnvironmentVariable("Jwt__Key")        // ASP.NET Core format
                       ?? configuration["Jwt:SecretKey"]
                       ?? configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT Secret Key is not configured. Please set JWT_SECRET_KEY, JWT_KEY, Jwt__SecretKey, or Jwt__Key environment variable, " +
                "or configure Jwt:SecretKey / Jwt:Key in appsettings.json. " +
                "For security reasons, hard-coded secrets are not recommended in production.");
        }

        return (
            SecretKey: secretKey,
            Issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? Environment.GetEnvironmentVariable("Jwt__Issuer") ?? configuration["Jwt:Issuer"] ?? DefaultIssuer,
            Audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? Environment.GetEnvironmentVariable("Jwt__Audience") ?? configuration["Jwt:Audience"] ?? DefaultAudience,
            AccessTokenExpirationMinutes: int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES") ?? Environment.GetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes") ?? configuration["Jwt:AccessTokenExpirationMinutes"], out var accessExp) ? accessExp : DefaultAccessTokenExpirationMinutes,
            RefreshTokenExpirationDays: int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS") ?? Environment.GetEnvironmentVariable("Jwt__RefreshTokenExpirationDays") ?? configuration["Jwt:RefreshTokenExpirationDays"], out var refreshExp) ? refreshExp : DefaultRefreshTokenExpirationDays
        );
    }
}
