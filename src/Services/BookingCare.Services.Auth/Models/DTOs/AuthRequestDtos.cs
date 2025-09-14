using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Auth.Utils;

namespace BookingCare.Services.Auth.Models.DTOs;

/// <summary>
/// Base class for DTOs that require either Email or PhoneNumber validation
/// </summary>
public abstract class EmailOrPhoneRequest : IValidatableObject
{
    // Either Email or PhoneNumber is required
    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? PhoneNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Either Email or PhoneNumber must be provided
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return new ValidationResult("Either Email or PhoneNumber is required", new[] { nameof(Email), nameof(PhoneNumber) });
        }

        // Both Email and PhoneNumber cannot be provided at the same time
        if (!string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return new ValidationResult("Please provide either Email or PhoneNumber, not both", new[] { nameof(Email), nameof(PhoneNumber) });
        }
    }
}

#region Authentication Request DTOs

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest : EmailOrPhoneRequest
{
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
    [Compare("Password", ErrorMessage = "Password and confirm password do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Required for Patient and Doctor (validated in service by role)
    public string? FullName { get; set; }

    [Required]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string PhoneNumber { get; set; } = string.Empty;

    // Required for Patient and Doctor (validated in service by role)
    public Gender? Gender { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    // Required only for Patient (validated in service by role)
    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    // Optional extended profiles depending on Role
    public DoctorProfileRequest? DoctorProfile { get; set; }
    public ClinicProfileRequest? ClinicProfile { get; set; }

    // OTP verification proof (used when registration requires prior OTP verification)
    public string? Proof { get; set; }
    public long? IssuedAt { get; set; }

    // Who initiated/verified the OTP: "phone" or "email". Defaults to phone.
    [Required]
    public string Channel { get; set; } = "phone";

    // Purpose namespace for OTP verification. Defaults to registration.
    [Required]
    public OtpPurpose Purpose { get; set; }


    /// <summary>
    /// Validate role-specific requirements
    /// </summary>
    public IEnumerable<ValidationResult> ValidateByRole(Role role)
    {
        if (role == Role.PATIENT)
        {
            if (string.IsNullOrWhiteSpace(FullName))
                yield return new ValidationResult("FullName is required for Patient", new[] { nameof(FullName) });

            if (!Gender.HasValue)
                yield return new ValidationResult("Gender is required for Patient", new[] { nameof(Gender) });

            if (!Birthday.HasValue)
                yield return new ValidationResult("Birthday is required for Patient", new[] { nameof(Birthday) });
            else if (Birthday.Value >= DateTime.Today)
                yield return new ValidationResult("Birthday cannot be today or in the future", new[] { nameof(Birthday) });
            else if (Birthday.Value < DateTime.Today.AddYears(-120))
                yield return new ValidationResult("Birthday seems invalid (too far in the past)", new[] { nameof(Birthday) });
            else if (DateTime.Today.Year - Birthday.Value.Year < 18)
                yield return new ValidationResult("You must be at least 18 years old to register", new[] { nameof(Birthday) });
        }
        else if (role == Role.DOCTOR)
        {
            if (string.IsNullOrWhiteSpace(FullName))
                yield return new ValidationResult("FullName is required for Doctor", new[] { nameof(FullName) });

            if (!Gender.HasValue)
                yield return new ValidationResult("Gender is required for Doctor", new[] { nameof(Gender) });

            if (DoctorProfile == null)
                yield return new ValidationResult("DoctorProfile is required for Doctor", new[] { nameof(DoctorProfile) });
            // DoctorProfile properties are validated by data annotations
        }
        else if (role == Role.CLINIC)
        {
            if (ClinicProfile == null)
                yield return new ValidationResult("ClinicProfile is required for Clinic", new[] { nameof(ClinicProfile) });
            // ClinicProfile properties are validated by data annotations
        }
    }
}

public class DoctorProfileRequest
{
    [Required]
    public Guid PositionId { get; set; }
    [Required]
    public Guid SpecialtyId { get; set; }
    [Required]
    public Guid ClinicId { get; set; }
    [Required]
    [MaxLength(2000)]
    public string Bio { get; set; } = string.Empty;
    [Required]
    [Range(0, 80)]
    public int YearsOfExperience { get; set; }
}

public class ClinicProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
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
public class ForgotPasswordRequest : EmailOrPhoneRequest
{
    // Required when using PhoneNumber to deliver OTP via a specific device route
    public string? DeviceId { get; set; }
}

/// <summary>
/// Request DTO for password reset
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
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
public class ResetTokenRequest
{
    [Required]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string PhoneNumber { get; set; } = string.Empty;

    // Optional: pass purpose to validate namespacing if needed
    public OtpPurpose Purpose { get; set; } = OtpPurpose.FORGOT_PASSWORD;

    // HMAC-based proof and timestamp from Notification Verify response
    public string? Proof { get; set; }
    public long? IssuedAt { get; set; }
}

/// <summary>
/// Request DTO for Google, Facebook external authentication
/// </summary>
public class ExternalAuthRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}


/// <summary>
/// External user information from Google
/// </summary>
public class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
}


/// <summary>
/// External user information from Facebook
/// </summary>
public class FacebookUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("picture")]
    public FacebookPicture? Picture { get; set; }
}

/// <summary>
/// Facebook picture information
/// </summary>
public class FacebookPicture
{
    [JsonPropertyName("data")]
    public FacebookPictureData? Data { get; set; }
}

/// <summary>
/// Facebook picture data
/// </summary>
public class FacebookPictureData
{
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("is_silhouette")]
    public bool IsSilhouette { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
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
