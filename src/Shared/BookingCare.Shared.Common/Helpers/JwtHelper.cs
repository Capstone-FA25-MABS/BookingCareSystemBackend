using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BookingCare.Shared.Common.Helpers;

/// <summary>
/// Helper class for JWT token operations
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// Extracts the AccountId from JWT token claims in HttpContext
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>The AccountId if found and valid, null otherwise</returns>
    public static Guid? GetAccountIdFromClaims(HttpContext httpContext)
    {
        // Try to get AccountId from custom claim first, then fallback to NameIdentifier
        var accountIdClaim = httpContext.User.FindFirst("AccountId") ??
                            httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

        if (accountIdClaim != null && Guid.TryParse(accountIdClaim.Value, out var accountId))
        {
            return accountId;
        }

        return null;
    }

    /// <summary>
    /// Extracts the AccountId from JWT token claims and throws UnauthorizedAccessException if not found
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>The AccountId</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when AccountId is not found or invalid</exception>
    public static Guid GetAccountIdFromClaimsOrThrow(HttpContext httpContext)
    {
        var accountId = GetAccountIdFromClaims(httpContext);

        if (accountId == null)
        {
            throw new UnauthorizedAccessException("Invalid or missing account ID in token");
        }

        return accountId.Value;
    }

    /// <summary>
    /// Gets the user email from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>The user email if found, null otherwise</returns>
    public static string? GetEmailFromClaims(HttpContext httpContext)
    {
        return httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user role from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>The user role if found, null otherwise</returns>
    public static string? GetRoleFromClaims(HttpContext httpContext)
    {
        return httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets all user roles from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>List of user roles</returns>
    public static List<string> GetUserRoles(HttpContext httpContext)
    {
        return httpContext.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <param name="role">The role to check for</param>
    /// <returns>True if the user has the specified role, false otherwise</returns>
    public static bool HasRole(HttpContext httpContext, string role)
    {
        return httpContext.User.IsInRole(role);
    }

    /// <summary>
    /// Gets all claims from the JWT token
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>Dictionary of claim types and values</returns>
    public static Dictionary<string, string> GetAllClaims(HttpContext httpContext)
    {
        return httpContext.User.Claims.ToDictionary(c => c.Type, c => c.Value);
    }

    /// <summary>
    /// Gets the email confirmation status from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>True if email is confirmed, false otherwise</returns>
    public static bool GetEmailConfirmationStatus(HttpContext httpContext)
    {
        var confirmEmailClaim = httpContext.User.FindFirst("confirmEmail");
        if (confirmEmailClaim != null && bool.TryParse(confirmEmailClaim.Value, out var isConfirmed))
        {
            return isConfirmed;
        }
        return false;
    }

    /// <summary>
    /// Gets the phone confirmation status from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>True if phone is confirmed, false otherwise</returns>
    public static bool GetPhoneConfirmationStatus(HttpContext httpContext)
    {
        var confirmPhoneClaim = httpContext.User.FindFirst("confirmPhone");
        if (confirmPhoneClaim != null && bool.TryParse(confirmPhoneClaim.Value, out var isConfirmed))
        {
            return isConfirmed;
        }
        return false;
    }

    /// <summary>
    /// Gets both email and phone confirmation statuses from JWT token claims
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>Tuple with email and phone confirmation statuses</returns>
    public static (bool EmailConfirmed, bool PhoneConfirmed) GetConfirmationStatuses(HttpContext httpContext)
    {
        return (GetEmailConfirmationStatus(httpContext), GetPhoneConfirmationStatus(httpContext));
    }
}
