using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Auth.Models.DTOs;

/// <summary>
/// Request DTO for password reset initiation
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Client application URL for reset link
    /// </summary>
    public string? ClientUrl { get; set; }
}

/// <summary>
/// Response DTO for password reset initiation
/// </summary>
public class ForgotPasswordResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "If the email exists, a password reset link has been sent.";

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reset token expiration time (for client reference)
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 30;
}

/// <summary>
/// Request DTO for password reset confirmation
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Password reset token
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for password reset confirmation
/// </summary>
public class ResetPasswordResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Password has been reset successfully.";

    /// <summary>
    /// Reset timestamp
    /// </summary>
    public DateTime ResetAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether automatic login is performed
    /// </summary>
    public bool AutoLogin { get; set; } = false;

    /// <summary>
    /// Optional: Login response if auto-login is enabled
    /// </summary>
    public LoginResponse? LoginResponse { get; set; }
}

/// <summary>
/// Request DTO for email verification
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// Email verification token
    /// </summary>
    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for email verification
/// </summary>
public class VerifyEmailResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Email has been verified successfully.";

    /// <summary>
    /// Verification timestamp
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether automatic login is performed
    /// </summary>
    public bool AutoLogin { get; set; } = false;

    /// <summary>
    /// Optional: Login response if auto-login is enabled
    /// </summary>
    public LoginResponse? LoginResponse { get; set; }
}

/// <summary>
/// Request DTO for resending verification email
/// </summary>
public class ResendVerificationRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for resending verification email
/// </summary>
public class ResendVerificationResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Verification email has been sent.";

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Verification token expiration time (for client reference)
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 60;
}
