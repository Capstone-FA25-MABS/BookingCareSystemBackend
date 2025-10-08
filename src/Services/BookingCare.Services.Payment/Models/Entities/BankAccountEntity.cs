using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for the bank_accounts table - stores user's bank account information
/// </summary>
[Table("bank_accounts")]
public class BankAccountEntity
{
    /// <summary>
    /// ID of the bank account
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the user who owns the bank account
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Bank code (e.g. VCB, TCB, VTB)
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("bank_code")]
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Full bank name
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("bank_name")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("account_number")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Account holder name
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("account_name")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this account is the default account
    /// </summary>
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Indicates whether the account is active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation time
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update time
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}