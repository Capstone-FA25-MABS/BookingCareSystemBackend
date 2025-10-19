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
