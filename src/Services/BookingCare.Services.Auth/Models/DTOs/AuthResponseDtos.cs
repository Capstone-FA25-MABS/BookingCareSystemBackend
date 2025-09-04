using BookingCare.Services.Auth.Enums;

namespace BookingCare.Services.Auth.Models.DTOs;

#region Authentication Response DTOs

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public class AuthResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AccountResponse? Account { get; set; }
}

/// <summary>
/// Response DTO for token refresh
/// </summary>
public class RefreshTokenResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Response DTO for password operations
/// </summary>
public class PasswordResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for issued reset token and URL
/// </summary>
public class IssueResetTokenResponse
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
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

/// <summary>
/// Response DTO for account list with pagination
/// </summary>
public class AccountListResponse
{
    public List<AccountResponse> Accounts { get; set; } = new List<AccountResponse>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
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
    public Guid AccountId { get; set; }
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

#region Utility Response DTOs

/// <summary>
/// Response DTO for general operations
/// </summary>
public class OperationResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

/// <summary>
/// Response DTO for validation errors
/// </summary>
public class ValidationErrorResponse
{
    public bool IsSuccess { get; set; } = false;
    public string Message { get; set; } = "Validation failed";
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
}

/// <summary>
/// Response DTO for pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Response DTO for search results
/// </summary>
public class SearchResponse<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
    public string? SearchTerm { get; set; }
    public int TotalResults { get; set; }
}

#endregion

#region Enum Response DTOs

/// <summary>
/// Response DTO for account status options
/// </summary>
public class AccountStatusResponse
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for gender options
/// </summary>
public class GenderResponse
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

#endregion

#region Additional Response DTOs

/// <summary>
/// Response DTO for logout
/// </summary>
public class LogoutResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for delete account
/// </summary>
public class DeleteAccountResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for delete role
/// </summary>
public class DeleteRoleResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for delete permission
/// </summary>
public class DeletePermissionResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for remove role
/// </summary>
public class RemoveRoleResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for remove permission
/// </summary>
public class RemovePermissionResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for get account roles
/// </summary>
public class GetAccountRolesResponse
{
    public List<RoleResponse> Roles { get; set; } = new List<RoleResponse>();
}

/// <summary>
/// Response DTO for get role permissions
/// </summary>
public class GetRolePermissionsResponse
{
    public List<PermissionResponse> Permissions { get; set; } = new List<PermissionResponse>();
}

/// <summary>
/// Response DTO for is authorized
/// </summary>
public class IsAuthorizedResponse
{
    public bool IsAuthorized { get; set; }
}

/// <summary>
/// Response DTO for get account permissions
/// </summary>
public class GetAccountPermissionsResponse
{
    public List<string> Permissions { get; set; } = new List<string>();
}

#endregion

#region Validation Error

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

#endregion
