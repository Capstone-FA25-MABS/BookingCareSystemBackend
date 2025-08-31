using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs;

// Request DTOs
public class CreateDoctorRequest
{
    [Required(ErrorMessage = "AccountId is required")]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public string LastName { get; set; } = string.Empty;

    [EnumDataType(typeof(Gender), ErrorMessage = "Gender must be one of: MALE, FEMALE, OTHER")]
    public Gender? Gender { get; set; }

    public Guid? PositionId { get; set; }

    public Guid? SpecialtyId { get; set; }

    public Guid? ClinicId { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    public int YearsOfExperience { get; set; } = 0;

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? AvatarUrl { get; set; }
}

public class UpdateDoctorRequest
{
    [Required(ErrorMessage = "Doctor ID is required")]
    public Guid Id { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public string? LastName { get; set; }

    [EnumDataType(typeof(Gender), ErrorMessage = "Gender must be one of: MALE, FEMALE, OTHER")]
    public Gender? Gender { get; set; }

    public Guid? PositionId { get; set; }

    public Guid? SpecialtyId { get; set; }

    public Guid? ClinicId { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    public int? YearsOfExperience { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? AvatarUrl { get; set; }
}

public class CreatePositionRequest
{
    [Required(ErrorMessage = "Position name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 255 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_()]+$", ErrorMessage = "Position name can only contain letters, numbers, spaces, hyphens, underscores, and parentheses")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class UpdatePositionRequest
{
    [Required(ErrorMessage = "Position ID is required")]
    public Guid Id { get; set; }

    [StringLength(255, MinimumLength = 2, ErrorMessage = "Position name must be between 2 and 255 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_()]+$", ErrorMessage = "Position name can only contain letters, numbers, spaces, hyphens, underscores, and parentheses")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class CreatePriceRequest
{
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
    public decimal Amount { get; set; }
}

public class UpdatePriceRequest
{
    [Required(ErrorMessage = "Price ID is required")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
    public decimal Amount { get; set; }
}

public class AssignPriceToDoctorRequest
{
    [Required(ErrorMessage = "Doctor ID is required")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Price ID is required")]
    public Guid PriceId { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}
