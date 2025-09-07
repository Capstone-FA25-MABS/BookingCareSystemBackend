namespace BookingCare.Services.Auth.Models.DTOs;

#region Authentication Response DTOs

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public class AuthResponse
{
    public string Message { get; set; } = string.Empty;
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

