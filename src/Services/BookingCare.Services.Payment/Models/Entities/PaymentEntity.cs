using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho bảng payments - Lưu trữ thông tin thanh toán
/// </summary>
[Table("payments")]
public class PaymentEntity
{
    /// <summary>
    /// ID của payment
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID của appointment (có thể null)
    /// </summary>
    [Column("appointment_id")]
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID của clinic (có thể null)
    /// </summary>
    [Column("clinic_id")]
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID của patient (có thể null)
    /// </summary>
    [Column("patient_id")]
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID của subscription (có thể null - chỉ dùng cho clinic khi đăng ký gói)
    /// </summary>
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Loại giao dịch (APPOINTMENT, SUBSCRIPTION)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("transaction_type")]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID của phương thức thanh toán
    /// </summary>
    [Required]
    [Column("payment_method_id")]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Trạng thái thanh toán (PENDING, COMPLETED, FAILED, REFUNDED)
    /// </summary>
    [Column("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property - Phương thức thanh toán
    /// </summary>
    public virtual PaymentMethodEntity PaymentMethod { get; set; } = null!;
}