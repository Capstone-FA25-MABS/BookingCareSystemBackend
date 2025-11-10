using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// Base class containing common hospital properties
/// </summary>
public abstract class HospitalBaseResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BackgroundUrl { get; set; }
    public string? AvatarUrl { get; set; }
}

public class HospitalResponse : HospitalBaseResponse
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<HospitalSpecialtyResponse>? Specialties { get; set; }
    public List<HospitalServiceTypeResponse>? ServiceTypes { get; set; }
    public List<HospitalServiceMedicalResponse>? ServiceMedicals { get; set; }
    public List<HospitalImageResponse>? Images { get; set; }
    public HospitalSubscriptionResponse? CurrentSubscription { get; set; }
}

public class HospitalSpecialtyResponse
{
    public Guid SpecialtyId { get; set; }
    public string? SpecialtyName { get; set; }
}

public class HospitalServiceTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DoctorCount { get; set; }
}

public class HospitalServiceMedicalResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class HospitalListResponse
{
    public List<HospitalResponse> Hospitals { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class HospitalDetailResponse : HospitalResponse
{
    public List<HospitalSubscriptionResponse>? SubscriptionHistory { get; set; }
}
