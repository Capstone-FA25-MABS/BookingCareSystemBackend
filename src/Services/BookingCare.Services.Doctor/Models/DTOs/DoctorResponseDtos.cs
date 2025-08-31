using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs;

// Response DTOs
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
    public List<PriceResponse> Prices { get; set; } = new();
}

public class PositionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PriceResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class DoctorPriceResponse
{
    public Guid DoctorId { get; set; }
    public Guid PriceId { get; set; }
    public string? Description { get; set; }
    public DoctorResponse Doctor { get; set; } = null!;
    public PriceResponse Price { get; set; } = null!;
}

public class DoctorListResponse
{
    public List<DoctorResponse> Doctors { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PositionListResponse
{
    public List<PositionResponse> Positions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PriceListResponse
{
    public List<PriceResponse> Prices { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// Query DTOs
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
}

public class PositionQueryRequest
{
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PriceQueryRequest
{
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
