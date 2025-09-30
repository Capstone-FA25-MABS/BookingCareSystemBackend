using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs.Responses;

public class PositionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public int DoctorCount { get; set; } = 0; // Số bác sĩ có position này
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PositionListResponse
{
    public List<PositionResponse> Positions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
