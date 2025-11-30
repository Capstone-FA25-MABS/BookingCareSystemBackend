using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for hospital_payouts table - tracks monthly payouts to hospitals
/// </summary>
[Table("hospital_payouts")]
public class HospitalPayoutEntity
{
    /// <summary>
    /// ID of the payout record
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the hospital receiving the payout
    /// </summary>
    [Required]
    [Column("hospital_id")]
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Name of the hospital (denormalized for search performance)
    /// </summary>
    [Required]
    [Column("hospital_name")]
    [MaxLength(255)]
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the bank account for this payout
    /// </summary>
    [Required]
    [Column("bank_account_id")]
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// Start date of the payout period (inclusive)
    /// </summary>
    [Required]
    [Column("period_start")]
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of the payout period (inclusive)
    /// </summary>
    [Required]
    [Column("period_end")]
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total amount to be paid to the hospital
    /// </summary>
    [Required]
    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of completed appointments in this period
    /// </summary>
    [Required]
    [Column("appointment_count")]
    public int AppointmentCount { get; set; }

    /// <summary>
    /// Status of the payout (PENDING, COMPLETED)
    /// </summary>
    [Required]
    [Column("status")]
    public PayoutStatus Status { get; set; } = PayoutStatus.PENDING;

    /// <summary>
    /// ID of the admin who marked this payout as completed
    /// </summary>
    [Column("processed_by_admin_id")]
    public Guid? ProcessedByAdminId { get; set; }

    /// <summary>
    /// Date when the payout was marked as completed
    /// </summary>
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Optional notes from admin about the payout
    /// </summary>
    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

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

    /// <summary>
    /// Navigation property - Bank account
    /// </summary>
    public virtual BankAccountEntity BankAccount { get; set; } = null!;
}
