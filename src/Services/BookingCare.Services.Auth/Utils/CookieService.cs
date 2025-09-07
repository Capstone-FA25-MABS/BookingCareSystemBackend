using BookingCare.Shared.Common.AppRouting;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Auth.Utils;

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

            var prefix = GetAppPrefix();
            var cookieTokenName = $"access_token_{prefix}_{userId}";
            var cookieRefreshTokenName = $"refresh_token_{prefix}";
            var cookieUserName = $"{prefix}_current_user";

            // Save access token
            var isHttps = httpContext.Request.IsHttps;
            httpContext.Response.Cookies.Append(cookieTokenName, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            // Save refresh token
            httpContext.Response.Cookies.Append(cookieRefreshTokenName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Save user ID
            httpContext.Response.Cookies.Append(cookieUserName, userId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
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

            var prefix = GetAppPrefix();
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

            var origin = GetOrigin();
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

    #region Helper Methods

    /// <summary>
    /// Get origin from HTTP context headers
    /// </summary>
    public string GetOrigin()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return string.Empty;

        return httpContext.Request.Headers["Origin"].FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Get app prefix based on origin
    /// </summary>
    public string GetAppPrefix()
    {
        var origin = GetOrigin();
        return AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
    }

    /// <summary>
    /// Get base URL for the current request
    /// </summary>
    public string GetBaseUrl()
    {
        var origin = GetOrigin();
        var appPrefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);
        return AppRoutingHelper.ResolveBaseUrl(appPrefix, _frontendOptions);
    }

    #endregion
}
