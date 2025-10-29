using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.DTOs.Requests;

public class CreateServiceTypeRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 255 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;
}

public class CreateServiceTypeWithImageRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 255 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;

    // ImageUrl will be set by the controller after upload
    public string? ImageUrl { get; set; }
}

public class UpdateServiceTypeRequest
{
    [Required(ErrorMessage = "Service type ID is required")]
    [JsonRequired]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 255 characters")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;
}

public class UpdateServiceTypeWithImageRequest
{
    [Required(ErrorMessage = "Service type ID is required")]
    [JsonRequired]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 255 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public Status Status { get; set; } = Status.ACTIVE;

    // ImageUrl will be set by the controller after upload
    public string? ImageUrl { get; set; }
}

public class ServiceTypeQueryRequest
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
