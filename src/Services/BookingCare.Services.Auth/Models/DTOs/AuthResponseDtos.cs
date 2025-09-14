using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Auth.Models.DTOs;

#region Authentication Response DTOs

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public class AuthResponse
{
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for issued reset token and URL
/// </summary>
public class ResetTokenResponse
{
    public string ResetUrl { get; set; } = string.Empty;
}

#endregion

#region Account Response DTOs

/// <summary>
/// Response DTO for account information
/// </summary>
public class AccountResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RoleResponse> Roles { get; set; } = new List<RoleResponse>();
}

#endregion

#region External Auth Response DTOs


/// <summary>
/// Google userinfo API response model
/// </summary>
public class GoogleUserInfoResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("verified_email")]
    [JsonConverter(typeof(StringToBoolConverter))]
    public bool VerifiedEmail { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
}

/// <summary>
/// Google token validation response model
/// </summary>
public class GoogleTokenValidationResponse
{
    [JsonPropertyName("audience")]
    public string? Audience { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("access_type")]
    public string? AccessType { get; set; }
}

public class FacebookTokenVerifyResult
{
    [JsonPropertyName("data")]
    public FacebookTokenData? Data { get; set; }
}

public class FacebookTokenData
{
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("application")]
    public string? Application { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }

    [JsonPropertyName("issued_at")]
    public long? IssuedAt { get; set; }

    [JsonPropertyName("scopes")]
    public string[]? Scopes { get; set; }
}

#endregion

#region Role Response DTOs

/// <summary>
/// Response DTO for role information
/// </summary>
public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = new List<PermissionResponse>();
}

/// <summary>
/// Response DTO for role list with pagination
/// </summary>
public class RoleListResponse
{
    public List<RoleResponse> Roles { get; set; } = new List<RoleResponse>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

#endregion

#region Permission Response DTOs

/// <summary>
/// Response DTO for permission information
/// </summary>
public class PermissionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for permission list with pagination
/// </summary>
public class PermissionListResponse
{
    public List<PermissionResponse> Permissions { get; set; } = new List<PermissionResponse>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

#endregion

#region Account-Role Response DTOs

/// <summary>
/// Response DTO for account-role relationship
/// </summary>
public class AccountRoleResponse
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AccountResponse? Account { get; set; }
    public RoleResponse? Role { get; set; }
}

#endregion

#region Role-Permission Response DTOs

/// <summary>
/// Response DTO for role-permission relationship
/// </summary>
public class RolePermissionResponse
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public RoleResponse? Role { get; set; }
    public PermissionResponse? Permission { get; set; }
}

#endregion

