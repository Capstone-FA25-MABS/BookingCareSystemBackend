namespace BookingCare.Services.Hospital.Models.DTOs.Responses;

public class HospitalImageResponse
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public string S3Key { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HospitalImageListResponse
{
    public List<HospitalImageResponse> HospitalImages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
