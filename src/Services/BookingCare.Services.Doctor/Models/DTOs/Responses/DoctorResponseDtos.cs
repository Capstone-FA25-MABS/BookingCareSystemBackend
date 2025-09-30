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
    public Guid? HospitalId { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public PositionResponse? Position { get; set; }
    public SpecialtyResponse? Specialty { get; set; }
    public List<DoctorPriceResponse> Prices { get; set; } = new();
    public List<LanguageResponse> Languages { get; set; } = new();
    public bool IsFavorited { get; set; }
    public Status Status { get; set; } // Không set mặc định

    // Hospital information
    public HospitalBasicInfo? Hospital { get; set; }
    // Review statistics - can be detailed (with rating distribution) or basic (without)
    public IDoctorReviewStatistics? ReviewStatistics { get; set; }
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

public class HospitalBasicInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class HospitalDetailInfo : HospitalBasicInfo
{
    public Guid AccountId { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BackgroundUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DoctorDetailResponse : DoctorResponse
{
    // Override Hospital with detailed info for detail view
    public new HospitalDetailInfo? Hospital { get; set; }
}

public class DoctorBasicInfoResponse
{
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public interface IDoctorReviewStatistics
{
    double AverageRating { get; set; }
    long TotalReviews { get; set; }
}

public class DoctorReviewStatistics : IDoctorReviewStatistics
{
    public double AverageRating { get; set; }
    public long TotalReviews { get; set; }
    public Dictionary<int, long> RatingDistribution { get; set; } = new();
}

public class DoctorReviewStatisticsBasic : IDoctorReviewStatistics
{
    public double AverageRating { get; set; }
    public long TotalReviews { get; set; }
}
