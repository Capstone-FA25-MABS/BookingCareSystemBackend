namespace BookingCare.Services.Doctor.Models.DTOs.Responses;

/// <summary>
/// Simple response DTO for specialties with minimal fields for performance optimization
/// </summary>
public class SpecialtySimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Simple response DTO for positions with minimal fields for performance optimization
/// </summary>
public class PositionSimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DoctorCount { get; set; } = 0;
}

/// <summary>
/// Simple response DTO for languages with minimal fields for performance optimization
/// </summary>
public class LanguageSimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Simple response DTO for service types with minimal fields for performance optimization
/// </summary>
public class ServiceTypeSimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
