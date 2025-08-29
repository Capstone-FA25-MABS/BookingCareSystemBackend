using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Auth.Models.DTOs;

/// <summary>
/// Request DTO for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token to use for generating a new access token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The expired access token (for additional validation)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Optional: Device information for security tracking
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Optional: Client application identifier
    /// </summary>
    public string? ClientId { get; set; }
}

/// <summary>
/// Response DTO for successful token refresh
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// New refresh token (if rotation is enabled)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Updated user information (if needed)
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Updated permissions/roles (if changed)
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Token refreshed successfully";

    /// <summary>
    /// Token issued timestamp
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is a new refresh token (rotation enabled)
    /// </summary>
    public bool IsNewRefreshToken { get; set; } = false;
}

/// <summary>
/// Request DTO for revoking refresh tokens
/// </summary>
public class RevokeTokenRequest
{
    /// <summary>
    /// The refresh token to revoke
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to revoke all tokens for the user
    /// </summary>
    public bool RevokeAll { get; set; } = false;

    /// <summary>
    /// Optional: Reason for revocation
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response DTO for token revocation
/// </summary>
public class RevokeTokenResponse
{
    /// <summary>
    /// Whether the revocation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Token revoked successfully";

    /// <summary>
    /// Number of tokens revoked
    /// </summary>
    public int TokensRevoked { get; set; }

    /// <summary>
    /// Revocation timestamp
    /// </summary>
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
