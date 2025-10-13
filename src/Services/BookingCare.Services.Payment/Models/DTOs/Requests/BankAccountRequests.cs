using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request to create a new bank account
/// </summary>
public class CreateBankAccountRequest
{
    /// <summary>
    /// ID of the user who owns the bank account
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// Bank code (e.g. VCB, TCB, VTB)
    /// </summary>
    [Required]
    [MaxLength(10, ErrorMessage = "Bank code must not exceed 10 characters")]
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Full bank name
    /// </summary>
    [Required]
    [MaxLength(255, ErrorMessage = "Bank name must not exceed 255 characters")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Account number must not exceed 50 characters")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Account number must contain digits only")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Account holder name
    /// </summary>
    [Required]
    [MaxLength(255, ErrorMessage = "Account holder name must not exceed 255 characters")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default account
    /// </summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Request to update a bank account
/// </summary>
public class UpdateBankAccountRequest
{
    /// <summary>
    /// ID of the bank account to update
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid Id { get; set; }

    /// <summary>
    /// Bank code (e.g. VCB, TCB, VTB)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Bank code must not exceed 10 characters")]
    public string? BankCode { get; set; }

    /// <summary>
    /// Full bank name
    /// </summary>
    [MaxLength(255, ErrorMessage = "Bank name must not exceed 255 characters")]
    public string? BankName { get; set; }

    /// <summary>
    /// Bank account number
    /// </summary>
    [MaxLength(50, ErrorMessage = "Account number must not exceed 50 characters")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Account number must contain digits only")]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Account holder name
    /// </summary>
    [MaxLength(255, ErrorMessage = "Account holder name must not exceed 255 characters")]
    public string? AccountName { get; set; }

    /// <summary>
    /// Whether this is the default account
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to get user's bank accounts with pagination
/// </summary>
public class GetBankAccountsRequest
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// Page number (starts from 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Only include active accounts
    /// </summary>
    public bool? ActiveOnly { get; set; }
}