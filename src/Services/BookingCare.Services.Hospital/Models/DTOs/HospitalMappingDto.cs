using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs;

/// <summary>
/// DTO for hospital mapping data to reduce method parameters
/// </summary>
public class HospitalMappingDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackgroundUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
