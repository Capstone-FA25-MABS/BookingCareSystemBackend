using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs.Requests;

// Base class for common doctor query properties
public abstract class BaseDoctorQueryRequest
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? ServiceType { get; set; }
    [JsonPropertyName("serviceTypes")]
    public List<string>? ServiceTypes { get; set; } // Support multiple service type filters
    public string? Language { get; set; }
    [JsonPropertyName("languages")]
    public List<string>? Languages { get; set; } // Support multiple language filters
    public double? MinRating { get; set; }
    [JsonPropertyName("minRatings")]
    public List<double>? MinRatings { get; set; } // Support multiple rating filters
    public string? Address { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // asc/desc
}

// Base class for common doctor properties
public abstract class BaseDoctorRequest
{
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

    public Guid? HospitalId { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    public int? YearsOfExperience { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? AvatarUrl { get; set; }

    public List<Guid>? LanguageIds { get; set; }

    public List<DoctorPriceRequest>? Prices { get; set; }
}

// Doctor Request DTOs
public class CreateDoctorRequest : BaseDoctorRequest
{
    [Required(ErrorMessage = "AccountId is required")]
    [JsonRequired]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    // Override base properties with required attributes for creation
    [Required(ErrorMessage = "First name is required")]
    public new string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public new string LastName { get; set; } = string.Empty;

    [JsonRequired]
    public new int YearsOfExperience { get; set; } = 0;
}

public class UpdateDoctorRequest : BaseDoctorRequest
{
    [Required(ErrorMessage = "Doctor ID is required")]
    [JsonRequired]
    public Guid Id { get; set; }
}

public class DoctorQueryRequest : BaseDoctorQueryRequest
{
    public Guid? AccountId { get; set; }
    public Guid? PositionId { get; set; }
    public List<Guid>? PositionIds { get; set; } // Support multiple position filters
    public Guid? SpecialtyId { get; set; }
    public List<Guid>? SpecialtyIds { get; set; } // Support multiple specialty filters
    public Guid? HospitalId { get; set; }
    public List<Guid>? HospitalIds { get; set; } // Support multiple hospital filters
    public string? ProvinceId { get; set; } // Province/City ID for location filtering
    public string? DistrictId { get; set; } // District ID for location filtering
    public string? Gender { get; set; }
    public List<string>? Genders { get; set; } // Support multiple gender filters
    public Status? Status { get; set; } // Filter by doctor status (ACTIVE/INACTIVE)
    public string? SearchTerm { get; set; }
    public int? MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    [JsonPropertyName("experienceRanges")]
    public List<ExperienceRange>? ExperienceRanges { get; set; } // Support multiple experience ranges
    [JsonRequired]
    public int PageNumber { get; set; } = 1;
    [JsonRequired]
    public int PageSize { get; set; } = 10;
    public string? AvailableTime { get; set; } // ISO 8601 hoặc custom format
}

public class ExperienceRange
{
    [JsonPropertyName("MinYears")]
    public int MinYears { get; set; }

    [JsonPropertyName("MaxYears")]
    public int MaxYears { get; set; }
}

public class DoctorAdvancedFilterRequest : BaseDoctorQueryRequest
{
    public Guid? SpecialtyId { get; set; }
    [JsonPropertyName("specialtyIds")]
    public List<Guid>? SpecialtyIds { get; set; } // Support multiple specialty filters
    public Guid? PositionId { get; set; }
    [JsonPropertyName("positionIds")]
    public List<Guid>? PositionIds { get; set; } // Support multiple position filters
    public string? AvailableTime { get; set; }
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
    [JsonPropertyName("genders")]
    public List<string>? Genders { get; set; } // Support multiple gender filters
    public int? MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    [JsonPropertyName("experienceRanges")]
    public List<ExperienceRange>? ExperienceRanges { get; set; } // Support multiple experience ranges
    public Guid? HospitalId { get; set; }
    [JsonPropertyName("hospitalIds")]
    public List<Guid>? HospitalIds { get; set; } // Support multiple hospital filters
    public string? ProvinceId { get; set; } // Province/City ID for location filtering
    public string? DistrictId { get; set; } // District ID for location filtering
    [JsonRequired]
    public int PageNumber { get; set; } = 1;
    [JsonRequired]
    public int PageSize { get; set; } = 10;
}

public class DoctorPriceRequest
{
    [Required(ErrorMessage = "Service type ID is required")]
    public Guid ServiceTypeId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0, 100000000, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }
}

public class AssignPriceToDoctorRequest
{
    [Required(ErrorMessage = "Doctor ID is required")]
    [JsonRequired]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Service type ID is required")]
    [JsonRequired]
    public Guid ServiceTypeId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0, 100000000, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }
}