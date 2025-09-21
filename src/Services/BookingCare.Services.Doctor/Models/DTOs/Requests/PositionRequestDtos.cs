using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Doctor.Models.DTOs.Requests;

public class CreatePositionRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;
}

public class UpdatePositionRequest
{
    [Required(ErrorMessage = "Position ID is required")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;
}

public class PositionQueryRequest
{
    public string? SearchTerm { get; set; }
    [JsonRequired]
    public int PageNumber { get; set; } = 1;
    [JsonRequired]
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // asc/desc
}
