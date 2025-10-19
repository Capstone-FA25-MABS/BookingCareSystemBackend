using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for the payments table - stores payment information
/// </summary>
[Table("payments")]
public class PaymentEntity
{
    /// <summary>
    /// ID of the payment
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the appointment (nullable)
    /// </summary>
    [Column("appointment_id")]
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// ID of the hospital (nullable)
    /// </summary>
    [Column("hospital_id")]
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// ID of the patient (nullable)
    /// </summary>
    [Column("patient_id")]
    public Guid? PatientId { get; set; }

    /// <summary>
    /// ID of the subscription (nullable - used for hospitals subscribing to a plan)
    /// </summary>
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction type (APPOINTMENT, SUBSCRIPTION)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("transaction_type")]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// ID of the payment method
    /// </summary>
    [Required]
    [Column("payment_method_id")]
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Payment status (PENDING, COMPLETED, FAILED, REFUNDED)
    /// </summary>
    [Column("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    /// <summary>
    /// Creation time
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property - Payment method
    /// </summary>
    public virtual PaymentMethodEntity PaymentMethod { get; set; } = null!;
}