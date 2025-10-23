using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs.Requests;

public class CreateSpecialtyRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;
}

public class CreateSpecialtyWithImageRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;

    // ImageUrl will be set by the controller after upload
    public string? ImageUrl { get; set; }
}

public class UpdateSpecialtyRequest
{
    [Required(ErrorMessage = "Specialty ID is required")]
    [JsonRequired]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;
}

public class UpdateSpecialtyWithImageRequest
{
    [Required(ErrorMessage = "Specialty ID is required")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;

    // ImageUrl will be set by the controller after upload
    public string? ImageUrl { get; set; }
}

public class SpecialtyQueryRequest
{
    public string? SearchTerm { get; set; }
    public Status? Status { get; set; }
    [JsonRequired]
    public int PageNumber { get; set; } = 1;
    [JsonRequired]
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // asc/desc
}
