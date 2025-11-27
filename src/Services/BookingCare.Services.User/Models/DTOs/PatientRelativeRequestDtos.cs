using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.DTOs;

/// <summary>
/// Request DTO for creating a new patient relative
/// </summary>
public class CreatePatientRelativeRequest
{
    [Required(ErrorMessage = "Họ là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    public DateTime DateOfBirth { get; set; }

    [Phone]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Mối quan hệ là bắt buộc")]
    public Relationship Relationship { get; set; }

    [MaxLength(20, ErrorMessage = "Số BHYT không được vượt quá 20 ký tự")]
    public string? HealthInsuranceNumber { get; set; }

    [MaxLength(20, ErrorMessage = "Số CMND/CCCD không được vượt quá 20 ký tự")]
    public string? IdentityNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for updating an existing patient relative
/// </summary>
public class UpdatePatientRelativeRequest
{
    [Required(ErrorMessage = "Họ là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    public DateTime DateOfBirth { get; set; }

    [Phone]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Mối quan hệ là bắt buộc")]
    public Relationship Relationship { get; set; }

    [MaxLength(20, ErrorMessage = "Số BHYT không được vượt quá 20 ký tự")]
    public string? HealthInsuranceNumber { get; set; }

    [MaxLength(20, ErrorMessage = "Số CMND/CCCD không được vượt quá 20 ký tự")]
    public string? IdentityNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? Notes { get; set; }
}
