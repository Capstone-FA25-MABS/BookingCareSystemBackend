using Microsoft.AspNetCore.Http;
using BookingCare.Shared.Common.AppRouting;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service for managing authentication cookies
/// </summary>
public class CookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieService> _logger;
    private readonly FrontendOptions _frontendOptions;

    public CookieService(IHttpContextAccessor httpContextAccessor, ILogger<CookieService> logger, IOptions<FrontendOptions>? frontendOptions = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _frontendOptions = frontendOptions?.Value ?? new FrontendOptions();
    }

    /// <summary>
    /// Save tokens in cookies
    /// </summary>
    public void SaveTokensInCookies(Guid userId, string accessToken, string refreshToken)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var origin = httpContext.Request.Headers["Origin"].ToString();
            var prefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
            var cookieTokenName = $"access_token_{prefix}_{userId}";
            var cookieRefreshTokenName = $"refresh_token_{prefix}";
            var cookieUserName = $"{prefix}_current_user";

            // Save access token
            httpContext.Response.Cookies.Append(cookieTokenName, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            // Save refresh token
            httpContext.Response.Cookies.Append(cookieRefreshTokenName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Save user ID
            httpContext.Response.Cookies.Append(cookieUserName, userId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            _logger.LogDebug("Tokens saved in cookies for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens in cookies for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Get refresh token from cookies
    /// </summary>
    public string GetRefreshTokenFromCookies()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return string.Empty;

            var origin = httpContext.Request.Headers["Origin"].ToString();
            var prefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
            var cookieRefreshTokenName = $"refresh_token_{prefix}";

            if (httpContext.Request.Cookies.TryGetValue(cookieRefreshTokenName, out var refreshToken))
            {
                return refreshToken;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh token from cookies");
            return string.Empty;
        }
    }

    /// <summary>
    /// Clear all authentication cookies
    /// </summary>
    public void ClearAuthenticationCookies()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var origin = httpContext.Request.Headers["Origin"].ToString();
            var prefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);

            // Get all cookies to remove
            var cookiesToRemove = httpContext.Request.Cookies.Keys
                .Where(key => key.StartsWith($"access_token_{prefix}_") || 
                             key.StartsWith($"{prefix}_current_user") || 
                             key.StartsWith($"refresh_token_{prefix}"))
                .ToList();

            // Remove each cookie
            foreach (var key in cookiesToRemove)
            {
                httpContext.Response.Cookies.Delete(key);
            }

            _logger.LogDebug("Authentication cookies cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authentication cookies");
        }
    }

    // Prefix resolution moved to AppRoutingHelper
}
