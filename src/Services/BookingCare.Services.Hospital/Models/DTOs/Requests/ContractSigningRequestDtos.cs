using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Hospital.Models.DTOs.Requests;

/// <summary>
/// DTO containing all data needed to generate a contract
/// </summary>
public class ContractDataDto
{
    // Contract metadata
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    // Party A (BookingCare - Admin)
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminPosition { get; set; } = string.Empty;
    public string AdminSignatureUrl { get; set; } = string.Empty;
    public string CompanyName { get; set; } = "CÔNG TY TNHH BOOKINGCARE";
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyTaxCode { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;

    // Party B (Hospital)
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalAddress { get; set; } = string.Empty;
    public string HospitalTaxCode { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;
    public string HospitalEmail { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativePosition { get; set; } = "Giám đốc";
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string RepresentativePhone { get; set; } = string.Empty;

    // Contract terms
    public decimal ServiceFeePercentage { get; set; } = 10; // 10% commission
    public string PaymentTerms { get; set; } = "Thanh toán hàng tháng";
    public int PaymentDueDays { get; set; } = 15; // Payment due within 15 days
}

/// <summary>
/// Request DTO for validating contract signing token
/// </summary>
public class ValidateTokenRequestDto
{
    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for signing contract
/// </summary>
public class SignContractRequestDto
{
    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chữ ký là bắt buộc")]
    public string SignatureBase64 { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có 6 ký tự")]
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>
    /// IP address (will be set by controller)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent (will be set by controller)
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Request DTO for sending OTP for contract signing
/// </summary>
public class SendSigningOtpRequestDto
{
    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; } = string.Empty;
}
