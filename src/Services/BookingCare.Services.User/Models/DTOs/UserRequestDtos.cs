using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.DTOs;

// Request DTOs

public class CreateUserRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100, MinimumLength = 5)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    public Gender? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Phone]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? Phone { get; set; }

    [Url]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }
}

public class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(100, MinimumLength = 5)]
    public string? Email { get; set; }

    [Phone]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? Phone { get; set; }

    public Gender? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Url]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }
}

public class UserQueryRequest
{
    [StringLength(100)]
    public string? SearchTerm { get; set; }

    public Gender? Gender { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    [StringLength(50)]
    public string? SortBy { get; set; } = "CreatedAt";

    public bool SortDescending { get; set; } = true;
}


