using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateHospitalRequest
{
    [Required]
    [JsonRequired]
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(255)]
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonRequired]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? BackgroundUrl { get; set; }

    public string? AvatarUrl { get; set; }

    public List<Guid>? SpecialtyIds { get; set; }
}

public class UpdateHospitalRequest
{
    [Required(ErrorMessage = "Tên bệnh viện là bắt buộc! Vui lòng nhập tên bệnh viện")]
    [MaxLength(255, ErrorMessage = "Tên bệnh viện không được vượt quá 255 ký tự")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Địa chỉ là bắt buộc! Vui lòng nhập địa chỉ")]
    [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string? Address { get; set; }

    // Email and Phone are read-only and should not be updated
    // These fields are kept in DTO for backward compatibility but will be ignored in UpdateAsync
    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Mô tả là bắt buộc! Vui lòng nhập mô tả")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "URL ảnh nền không hợp lệ")]
    [MaxLength(500, ErrorMessage = "URL ảnh nền không được vượt quá 500 ký tự")]
    public string? BackgroundUrl { get; set; }

    [Url(ErrorMessage = "URL ảnh đại diện không hợp lệ")]
    [MaxLength(500, ErrorMessage = "URL ảnh đại diện không được vượt quá 500 ký tự")]
    public string? AvatarUrl { get; set; }

    public List<Guid>? SpecialtyIds { get; set; }
    public List<Guid>? ServiceTypeIds { get; set; }
    public List<Guid>? ServiceMedicalIds { get; set; }
}

public class HospitalFilterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public List<Guid>? SpecialtyIds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}

/// <summary>
/// Optimized filter request for hospital list with essential filters
/// </summary>
public class HospitalListOptimizedFilterRequest
{
    public string? Search { get; set; }
    public string[]? SpecialtyIds { get; set; }
    public string? ProvinceId { get; set; }
    public string? DistrictId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "Name";
    public string? SortOrder { get; set; } = "asc";
}