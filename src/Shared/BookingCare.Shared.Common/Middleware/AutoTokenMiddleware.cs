using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BookingCare.Shared.Common.AppRouting;
using Microsoft.Extensions.Options;
using BookingCare.Shared.Common.Configuration;

namespace BookingCare.Shared.Common.Middleware;

/// <summary>
/// Middleware to automatically attach access token from cookies to Authorization header
/// This allows seamless token management without manual header attachment
/// </summary>
public class AutoTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AutoTokenMiddleware> _logger;
    private readonly FrontendOptions _frontendOptions;

    public AutoTokenMiddleware(RequestDelegate next, ILogger<AutoTokenMiddleware> logger, IOptions<FrontendOptions>? frontendOptions = null)
    {
        _next = next;
        _logger = logger;
        _frontendOptions = frontendOptions?.Value ?? FrontendConfiguration.CreateFrontendOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Check if Authorization header is already present
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                // Try to get token from cookies and attach to Authorization header
                var token = GetTokenFromCookies(context);
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers.Add("Authorization", $"Bearer {token}");
                    _logger.LogDebug("AutoTokenMiddleware: Token attached from cookies");
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AutoTokenMiddleware");
            throw;
        }
    }

    private string GetTokenFromCookies(HttpContext context)
    {
        try
        {
            var cookies = context.Request.Cookies;
            var origin = context.Request.Headers["Origin"].ToString();

            // Determine prefix based on origin
            var prefix = AppRoutingHelper.GetAppPrefix(origin, _frontendOptions);

            // Get user ID from cookie
            var userKey = $"{prefix}_current_user";
            if (!cookies.TryGetValue(userKey, out var userId) || string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            // Get access token from cookie
            var tokenKey = $"access_token_{prefix}_{userId}";
            if (cookies.TryGetValue(tokenKey, out var token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from cookies");
            return string.Empty;
        }
    }
}
