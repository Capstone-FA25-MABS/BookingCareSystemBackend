using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Doctor.Models.DTOs.Requests;

public class CreateLanguageRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateLanguageRequest
{
    [Required(ErrorMessage = "Language ID is required")]
    [JsonRequired]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
}

public class LanguageQueryRequest
{
    public string? SearchTerm { get; set; }
    [JsonRequired]
    public int PageNumber { get; set; } = 1;
    [JsonRequired]
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // asc/desc
}