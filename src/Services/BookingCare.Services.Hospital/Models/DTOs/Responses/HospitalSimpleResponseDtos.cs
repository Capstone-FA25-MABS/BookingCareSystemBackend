namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// Simple response DTO for hospitals with minimal fields for performance optimization
/// </summary>
public class HospitalSimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Optimized response DTO for hospital list with essential fields for UI display
/// </summary>
public class HospitalListOptimizedResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<HospitalSpecialtyOptimizedResponse>? Specialties { get; set; }
    public int TotalSpecialties { get; set; }
}

/// <summary>
/// Optimized specialty response for hospital list with essential fields
/// </summary>
public class HospitalSpecialtyOptimizedResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Paginated response for optimized hospital list
/// </summary>
public class HospitalListOptimizedPaginatedResponse
{
    public List<HospitalListOptimizedResponse> Hospitals { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}