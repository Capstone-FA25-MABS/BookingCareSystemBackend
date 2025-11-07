using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateHospitalImageRequest
{
    [Required]
    public Guid HospitalId { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateHospitalImageRequest
{
    public string? ImageUrl { get; set; }
}

public class HospitalImageFilterRequest
{
    public Guid? HospitalId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
