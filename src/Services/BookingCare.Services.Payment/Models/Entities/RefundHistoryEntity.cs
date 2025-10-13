using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for the refund_histories table - stores refund history records for patients
/// </summary>
[Table("refund_histories")]
public class RefundHistoryEntity
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the bank account for the refund (nullable - may not have a bank account)
    /// </summary>
    [Column("bank_account_id")]
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// ID of the user who requested the refund
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID of the hospital that needs to process the refund (required)
    /// </summary>
    [Required]
    [Column("hospital_id")]
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Refund status
    /// </summary>
    [Required]
    [Column("status")]
    public RefundStatus Status { get; set; } = RefundStatus.WAITING;

    /// <summary>
    /// Transfer date (nullable - only set when status = COMPLETED)
    /// </summary>
    [Column("transfer_date")]
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// ID of the payment to be refunded
    /// </summary>
    [Required]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    [Required]
    [Column("refund_amount", TypeName = "decimal(10,2)")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    [MaxLength(500)]
    [Column("refund_reason")]
    public string? RefundReason { get; set; }

    /// <summary>
    /// Notes from staff (when processing the refund)
    /// </summary>
    [MaxLength(500)]
    [Column("staff_notes")]
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID of the staff who processed the refund
    /// </summary>
    [Column("processed_by_staff_id")]
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Time when the refund request was created
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
    /// Navigation property - Payment being refunded
    /// </summary>
    public virtual PaymentEntity Payment { get; set; } = null!;

    /// <summary>
    /// Navigation property - Bank account for the refund (nullable)
    /// </summary>
    public virtual BankAccountEntity? BankAccount { get; set; }
}