using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

public class CreateHospitalRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
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
    [MaxLength(255)]
    public string? Name { get; set; }

    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    public string? Description { get; set; }

    public string? BackgroundUrl { get; set; }

    public string? AvatarUrl { get; set; }

    public Status? Status { get; set; }

    public List<Guid>? SpecialtyIds { get; set; }
}

public class HospitalFilterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Status? Status { get; set; }
    public List<Guid>? SpecialtyIds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
