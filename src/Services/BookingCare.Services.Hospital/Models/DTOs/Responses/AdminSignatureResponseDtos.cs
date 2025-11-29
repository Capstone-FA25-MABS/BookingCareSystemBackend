namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

/// <summary>
/// DTO for admin signature response
/// </summary>
public class AdminSignatureResponseDto
{
    public Guid Id { get; set; }
    public string AdminId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string SignatureImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
