using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs;

// Doctor Request DTOs
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

    [Range(0, 100000000, ErrorMessage = "Price must be positive")]
    public decimal? Price { get; set; }

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

    [Range(0, 100000000, ErrorMessage = "Price must be positive")]
    public decimal? Price { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? AvatarUrl { get; set; }
}

public class DoctorQueryRequest
{
    public Guid? AccountId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? ClinicId { get; set; }
    public Gender? Gender { get; set; }
    public string? SearchTerm { get; set; }
    public int? MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? AvailableTime { get; set; } // ISO 8601 hoặc custom format
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? ServiceType { get; set; }
    public string? Language { get; set; }
    public double? MinRating { get; set; }
    public string? Address { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // asc/desc
}

public class DoctorAdvancedFilterRequest
{
    public Guid? SpecialtyId { get; set; }
    public string? AvailableTime { get; set; }
    public Gender? Gender { get; set; }
    public int? MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Guid? ClinicId { get; set; }
    public string? ServiceType { get; set; }
    public string? Language { get; set; }
    public double? MinRating { get; set; }
    public string? Address { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AssignPriceToDoctorRequest
{
    [Required(ErrorMessage = "Doctor ID is required")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0, 100000000, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }
}
