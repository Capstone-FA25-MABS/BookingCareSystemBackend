using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateHospitalImageRequest
{
    [Required]
    public Guid HospitalId { get; set; }

    [Required]
    [MaxLength(500)]
    public string S3Key { get; set; } = string.Empty;

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

public class UpdateHospitalImageRequest
{
    [MaxLength(500)]
    public string? S3Key { get; set; }

    public string? ImageUrl { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}

public class HospitalImageFilterRequest
{
    public Guid? HospitalId { get; set; }
    public string? Description { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
