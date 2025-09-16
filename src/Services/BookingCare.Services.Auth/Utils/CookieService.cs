using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Services;
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
    private readonly CookieEncryptionService _encryptionService;

    public CookieService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CookieService> logger,
        CookieEncryptionService encryptionService,
        IOptions<FrontendOptions>? frontendOptions = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _encryptionService = encryptionService;
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

            // Generate encrypted cookie names to prevent information leakage
            var cookieTokenName = GenerateEncryptedCookieName($"access_token_{prefix}_{userId}");
            var cookieRefreshTokenName = GenerateEncryptedCookieName($"refresh_token_{prefix}");
            var cookieUserName = GenerateEncryptedCookieName($"{prefix}_current_user");

            // Also create a registry cookie to track our authentication cookies for easier cleanup
            var cookieRegistryName = GenerateEncryptedCookieName($"bc_auth_registry_{prefix}");

            // Encrypt tokens before saving
            var encryptedAccessToken = _encryptionService.Encrypt(accessToken);
            var encryptedRefreshToken = _encryptionService.Encrypt(refreshToken);
            var encryptedUserId = _encryptionService.Encrypt(userId.ToString());

            // Create registry data with all our cookie names for this session
            var registryData = $"{cookieTokenName},{cookieRefreshTokenName},{cookieUserName}";
            var encryptedRegistryData = _encryptionService.Encrypt(registryData);

            // Save encrypted access token with encrypted name
            httpContext.Response.Cookies.Append(cookieTokenName, encryptedAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            // Save encrypted refresh token with encrypted name
            httpContext.Response.Cookies.Append(cookieRefreshTokenName, encryptedRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Save encrypted user ID with encrypted name
            httpContext.Response.Cookies.Append(cookieUserName, encryptedUserId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            // Save registry cookie for easier cleanup
            httpContext.Response.Cookies.Append(cookieRegistryName, encryptedRegistryData, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7) // Same as refresh token
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
            var cookieRefreshTokenName = GenerateEncryptedCookieName($"refresh_token_{prefix}");

            if (httpContext.Request.Cookies.TryGetValue(cookieRefreshTokenName, out var encryptedRefreshToken))
            {
                // Decrypt the refresh token before returning
                return _encryptionService.Decrypt(encryptedRefreshToken);
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

            var prefix = GetAppPrefix();
            var cookiesToRemove = new List<string>();

            // First, try to use the registry cookie for efficient cleanup
            var registryName = GenerateEncryptedCookieName($"bc_auth_registry_{prefix}");
            if (httpContext.Request.Cookies.TryGetValue(registryName, out var encryptedRegistry))
            {
                try
                {
                    var registryData = _encryptionService.Decrypt(encryptedRegistry);
                    if (!string.IsNullOrEmpty(registryData))
                    {
                        var cookieNames = registryData.Split(',');
                        cookiesToRemove.AddRange(cookieNames);
                        cookiesToRemove.Add(registryName); // Also remove the registry itself
                    }
                }
                catch
                {
                    // If registry decryption fails, fall back to manual detection
                    _logger.LogWarning("Failed to decrypt cookie registry, falling back to manual detection");
                }
            }

            // Fallback: If no registry or registry failed, scan all cookies
            if (cookiesToRemove.Count == 0)
            {
                foreach (var cookieKey in httpContext.Request.Cookies.Keys)
                {
                    try
                    {
                        // Try to find if this is one of our encrypted cookie names
                        if (IsAuthenticationCookie(cookieKey, prefix))
                        {
                            cookiesToRemove.Add(cookieKey);
                        }
                    }
                    catch
                    {
                        // Ignore errors for non-auth cookies
                    }
                }
            }

            // Remove each identified authentication cookie
            foreach (var key in cookiesToRemove.Distinct())
            {
                httpContext.Response.Cookies.Delete(key);
            }

            _logger.LogDebug("Authentication cookies cleared: {CookieCount} cookies removed", cookiesToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authentication cookies");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Generate encrypted cookie name from plain text name
    /// </summary>
    private string GenerateEncryptedCookieName(string plainName)
    {
        return _encryptionService.GenerateEncryptedCookieName(plainName);
    }

    /// <summary>
    /// Check if a cookie name corresponds to an authentication cookie
    /// </summary>
    private bool IsAuthenticationCookie(string cookieName, string prefix)
    {
        // First check if it's a valid BookingCare cookie format
        if (!_encryptionService.IsBookingCareCookie(cookieName))
        {
            return false;
        }

        // Check if the cookie name matches our known static patterns (refresh_token, current_user)
        if (_encryptionService.IsValidAuthCookieName(cookieName, prefix))
        {
            return true;
        }

        // For access tokens, check if it matches the pattern access_token_{prefix}_{userId}
        // We need to find the actual user ID from current_user cookie first
        return IsAccessTokenCookie(cookieName, prefix);
    }

    /// <summary>
    /// Check if a cookie name corresponds to an access token cookie
    /// </summary>
    private bool IsAccessTokenCookie(string cookieName, string prefix)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false;

            // Get user ID from current_user cookie
            var userCookieName = GenerateEncryptedCookieName($"{prefix}_current_user");
            if (!httpContext.Request.Cookies.TryGetValue(userCookieName, out var encryptedUserId))
            {
                return false;
            }

            var userId = _encryptionService.Decrypt(encryptedUserId);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // Check if this cookie name matches the access token pattern for this user
            var expectedAccessTokenName = GenerateEncryptedCookieName($"access_token_{prefix}_{userId}");
            return cookieName == expectedAccessTokenName;
        }
        catch
        {
            return false;
        }
    }

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
