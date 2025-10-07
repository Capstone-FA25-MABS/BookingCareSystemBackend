using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho b?ng refund_histories - L?u tr? l?ch s? refund c?a b?nh nhân
/// </summary>
[Table("refund_histories")]
public class RefundHistoryEntity
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID c?a bank account ?? refund (nullable - có th? ch?a có bank account)
    /// </summary>
    [Column("bank_account_id")]
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// ID c?a user yêu c?u refund
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Tr?ng thái refund
    /// </summary>
    [Required]
    [Column("status")]
    public RefundStatus Status { get; set; } = RefundStatus.WAITING;

    /// <summary>
    /// Ngày chuy?n ti?n (nullable - ch? có khi status = COMPLETED)
    /// </summary>
    [Column("transfer_date")]
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// ID c?a payment c?n refund
    /// </summary>
    [Required]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// S? ti?n refund
    /// </summary>
    [Required]
    [Column("refund_amount", TypeName = "decimal(10,2)")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Lý do refund
    /// </summary>
    [MaxLength(500)]
    [Column("refund_reason")]
    public string? RefundReason { get; set; }

    /// <summary>
    /// Ghi chú t? staff (khi x? lý refund)
    /// </summary>
    [MaxLength(500)]
    [Column("staff_notes")]
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID c?a staff x? lý refund
    /// </summary>
    [Column("processed_by_staff_id")]
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Th?i gian t?o yêu c?u refund
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Th?i gian c?p nh?t cu?i cùng
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property - Payment ???c refund
    /// </summary>
    public virtual PaymentEntity Payment { get; set; } = null!;

    /// <summary>
    /// Navigation property - Bank account ?? refund (nullable)
    /// </summary>
    public virtual BankAccountEntity? BankAccount { get; set; }
}