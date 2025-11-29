using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

/// <summary>
/// DTO for creating a new admin signature
/// </summary>
public class CreateAdminSignatureRequestDto
{
    /// <summary>
    /// Full name of the admin
    /// </summary>
    [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
    [MaxLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Position/Title of the admin
    /// </summary>
    [Required(ErrorMessage = "Chức vụ là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Signature image file
    /// </summary>
    [Required(ErrorMessage = "File chữ ký là bắt buộc")]
    public IFormFile SignatureFile { get; set; } = null!;
}

/// <summary>
/// DTO for updating an existing admin signature
/// </summary>
public class UpdateAdminSignatureRequestDto
{
    /// <summary>
    /// Full name of the admin
    /// </summary>
    [MaxLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự")]
    public string? FullName { get; set; }

    /// <summary>
    /// Position/Title of the admin
    /// </summary>
    [MaxLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
    public string? Position { get; set; }

    /// <summary>
    /// New signature image file (optional)
    /// </summary>
    public IFormFile? SignatureFile { get; set; }

    /// <summary>
    /// Whether this signature is active
    /// </summary>
    public bool? IsActive { get; set; }
}
