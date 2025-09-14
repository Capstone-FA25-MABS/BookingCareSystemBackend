using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs.Responses;

// Doctor Response DTOs
public class DoctorResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender? Gender { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? ClinicId { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public PositionResponse? Position { get; set; }
    public List<DoctorPriceResponse> Prices { get; set; } = new();
    public List<LanguageResponse> Languages { get; set; } = new();
}

public class DoctorPriceResponse
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DoctorListResponse
{
    public List<DoctorResponse> Doctors { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
