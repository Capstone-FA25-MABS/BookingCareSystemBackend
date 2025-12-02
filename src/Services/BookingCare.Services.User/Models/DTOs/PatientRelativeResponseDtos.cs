using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.DTOs;

/// <summary>
/// Response DTO for patient relative information
/// </summary>
public class PatientRelativeResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string GenderDisplay { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string? Phone { get; set; }
    public Relationship Relationship { get; set; }
    public string RelationshipDisplay { get; set; } = string.Empty;
    public string? HealthInsuranceNumber { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Lightweight response for dropdown/selection
/// </summary>
public class PatientRelativeBasicResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Relationship Relationship { get; set; }
    public string RelationshipDisplay { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
}
