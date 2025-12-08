using System.ComponentModel.DataAnnotations;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new hospital registration
/// </summary>
public class CreateHospitalRegistrationRequestDto
{
    // Representative Information
    [Required(ErrorMessage = "Tên người đại diện là bắt buộc")]
    [MaxLength(255, ErrorMessage = "Tên người đại diện không được vượt quá 255 ký tự")]
    public string RepresentativeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email người đại diện là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email người đại diện không hợp lệ")]
    [MaxLength(100, ErrorMessage = "Email người đại diện không được vượt quá 100 ký tự")]
    public string RepresentativeEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại người đại diện là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại người đại diện không hợp lệ")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại người đại diện phải là số điện thoại Việt Nam hợp lệ")]
    public string RepresentativePhone { get; set; } = string.Empty;

    // Hospital Information
    [Required(ErrorMessage = "Tên bệnh viện là bắt buộc")]
    [MaxLength(255, ErrorMessage = "Tên bệnh viện không được vượt quá 255 ký tự")]
    public string HospitalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email bệnh viện là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email bệnh viện không hợp lệ")]
    [MaxLength(100, ErrorMessage = "Email bệnh viện không được vượt quá 100 ký tự")]
    public string HospitalEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại bệnh viện là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại bệnh viện không hợp lệ")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại bệnh viện phải là số điện thoại Việt Nam hợp lệ")]
    public string HospitalPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giấy phép kinh doanh là bắt buộc")]
    public IFormFile LicenseFile { get; set; } = null!;

    [Required(ErrorMessage = "Giấy chứng nhận đăng ký doanh nghiệp là bắt buộc")]
    public IFormFile BusinessCertificateFile { get; set; } = null!;

    [Required(ErrorMessage = "CMND/CCCD là bắt buộc")]
    public IFormFile IdentityCardFile { get; set; } = null!;

    [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
    [RegularExpression(@"^\d{10}(-\d{3})?$", ErrorMessage = "Mã số thuế không hợp lệ. Mã số thuế phải có 10 chữ số hoặc 10 chữ số theo sau bởi -XXX")]
    public string TaxCode { get; set; } = string.Empty;

    // eKYC Information (populated after eKYC verification)
    // Privacy-friendly: Only store verification status and scores, not PII

    /// <summary>
    /// eKYC session ID from verification process
    /// </summary>
    public string? EkycSessionId { get; set; }

    /// <summary>
    /// Face matching similarity score (0-100)
    /// </summary>
    public decimal? FaceMatchScore { get; set; }

    /// <summary>
    /// Liveness detection score (0-100)
    /// </summary>
    public decimal? LivenessScore { get; set; }

    /// <summary>
    /// Whether eKYC verification was successful
    /// </summary>
    public bool IsEkycVerified { get; set; }
}

/// <summary>
/// Request DTO for updating hospital registration status (admin only)
/// </summary>
public class UpdateRegistrationStatusRequestDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    [Range(0, 2, ErrorMessage = "Trạng thái không hợp lệ (0=PENDING, 1=CONFIRMED, 2=CANCELLED)")]
    public int Status { get; set; }

    /// <summary>
    /// Reason for cancellation (required when status is CANCELLED)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
    public string? Reason { get; set; }

    /// <summary>
    /// Contract file (optional, can be uploaded when confirming)
    /// </summary>
    public IFormFile? ContractFile { get; set; }

    /// <summary>
    /// Hospital ID (required when status is CONFIRMED)
    /// </summary>
    public Guid? HospitalId { get; set; }
}

/// <summary>
/// Request DTO for approving hospital registration
/// NOTE: Contract file is no longer required as it's already signed by hospital
/// </summary>
public class ApproveRegistrationRequestDto
{
    /// <summary>
    /// Optional approval notes from admin
    /// </summary>
    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// Request DTO for rejecting hospital registration
/// </summary>
public class RejectRegistrationRequestDto
{
    [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating hospital registration (contract file updates)
/// </summary>
public class UpdateRegistrationRequestDto
{
    /// <summary>
    /// Updated contract file
    /// </summary>
    [Required(ErrorMessage = "File hợp đồng là bắt buộc")]
    public IFormFile ContractFile { get; set; } = null!;
}

/// <summary>
/// Request DTO for filtering hospital registrations
/// </summary>
public class HospitalRegistrationFilterRequestDto
{
    public string? SearchTerm { get; set; }
    public RegistrationStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "DESC";
}

