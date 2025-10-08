namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for bank account
/// </summary>
public class BankAccountResponse
{
    /// <summary>
    /// ID of the bank account
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user who owns the bank account
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Bank code
    /// </summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Full bank name
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number (masked for security)
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Full account number (only shown for certain roles)
    /// </summary>
    public string? FullAccountNumber { get; set; }

    /// <summary>
    /// Account holder name
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default account
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}