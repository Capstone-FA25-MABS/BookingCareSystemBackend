using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Auth.Models.DTOs;

#region Authentication Request DTOs

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    [Required]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Birthday { get; set; }
}

/// <summary>
/// Request DTO for password change
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public Guid AccountId { get; set; }

    // CurrentPassword is optional for external login accounts (Google/Facebook)
    public string? CurrentPassword { get; set; }

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for forgot password
/// </summary>
public class ForgotPasswordRequest
{
    // Either Email or PhoneNumber is required
    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    // Required when using PhoneNumber to deliver OTP via a specific device route
    public string? DeviceId { get; set; }
}

/// <summary>
/// Request DTO for password reset
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    //[EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO to issue reset token after OTP verification (phone flow)
/// </summary>
public class IssueResetTokenRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    // Optional: pass purpose to validate namespacing if needed
    public string Purpose { get; set; } = "forgot-password";

    // HMAC-based proof and timestamp from Notification Verify response
    public string? Proof { get; set; }
    public long? IssuedAt { get; set; }
}

#endregion

#region Account Request DTOs

/// <summary>
/// Request DTO for creating a new account
/// </summary>
public class CreateAccountRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Birthday { get; set; }
}

/// <summary>
/// Request DTO for updating an existing account
/// </summary>
public class UpdateAccountRequest
{
    [Required]
    public Guid Id { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }
}

#endregion

#region Role Request DTOs

/// <summary>
/// Request DTO for creating a new role
/// </summary>
public class CreateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating an existing role
/// </summary>
public class UpdateRoleRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }
}

#endregion

#region Permission Request DTOs

/// <summary>
/// Request DTO for creating a new permission
/// </summary>
public class CreatePermissionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating an existing permission
/// </summary>
public class UpdatePermissionRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

#endregion

#region Relationship Request DTOs

/// <summary>
/// Request DTO for assigning a role to an account
/// </summary>
public class AssignRoleRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request DTO for removing a role from an account
/// </summary>
public class RemoveRoleRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request DTO for assigning a permission to a role
/// </summary>
public class AssignPermissionRequest
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
}

/// <summary>
/// Request DTO for removing a permission from a role
/// </summary>
public class RemovePermissionRequest
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
}

#endregion

#region Query Request DTOs

/// <summary>
/// Request DTO for account queries
/// </summary>
public class AccountQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Request DTO for role queries
/// </summary>
public class RoleQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Request DTO for permission queries
/// </summary>
public class PermissionQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

#endregion
