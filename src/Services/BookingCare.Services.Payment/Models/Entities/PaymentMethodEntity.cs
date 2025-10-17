using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for the payment_methods table - lookup table for payment methods
/// </summary>
[Table("payment_methods")]
public class PaymentMethodEntity
{
    /// <summary>
    /// ID of the payment method
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Payment method name (unique)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the payment method
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// URL of the payment method image/logo
    /// </summary>
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Status of the payment method (ACTIVE, INACTIVE)
    /// </summary>
    [Column("status")]
    public PaymentMethodStatus Status { get; set; } = PaymentMethodStatus.ACTIVE;

    /// <summary>
    /// Navigation property - list of payments using this method
    /// </summary>
    public virtual ICollection<PaymentEntity> Payments { get; set; } = new List<PaymentEntity>();
}