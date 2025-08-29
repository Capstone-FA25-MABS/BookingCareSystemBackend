using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Auth.Models.DTOs;

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's gender
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// User role (Patient, Doctor, Admin, etc.)
    /// </summary>
    public string Role { get; set; } = "Patient";
}

/// <summary>
/// Response DTO for successful registration
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Newly created user's ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's assigned role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether email verification is required
    /// </summary>
    public bool RequiresEmailVerification { get; set; } = true;

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Registration successful. Please check your email for verification.";

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
