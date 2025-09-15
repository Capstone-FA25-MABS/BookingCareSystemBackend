using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho b?ng payments - L?u tr? thông tin thanh toán
/// </summary>
[Table("payments")]
public class PaymentEntity
{
    /// <summary>
    /// ID c?a payment
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID c?a appointment (có th? null)
    /// </summary>
    [Column("appointment_id")]
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID c?a clinic (có th? null)
    /// </summary>
    [Column("clinic_id")]
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// ID c?a patient (có th? null)
    /// </summary>
    [Column("patient_id")]
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID c?a subscription (có th? null - ch? d?ng cho clinic khi ??ng ký gói)
    /// </summary>
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// S? ti?n thanh toán
    /// </summary>
    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Lo?i giao d?ch (APPOINTMENT, SUBSCRIPTION)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("transaction_type")]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID c?a ph??ng th?c thanh toán
    /// </summary>
    [Required]
    [Column("payment_method_id")]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Tr?ng thái thanh toán (PENDING, COMPLETED, FAILED, REFUNDED)
    /// </summary>
    [Column("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property - Ph??ng th?c thanh toán
    /// </summary>
    public virtual PaymentMethodEntity PaymentMethod { get; set; } = null!;
}