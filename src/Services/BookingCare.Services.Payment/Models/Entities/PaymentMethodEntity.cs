using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho b?ng payment_methods - Lookup table cho các ph??ng th?c thanh toán
/// </summary>
[Table("payment_methods")]
public class PaymentMethodEntity
{
    /// <summary>
    /// ID c?a ph??ng th?c thanh toán
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tên ph??ng th?c thanh toán (unique)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô t? ph??ng th?c thanh toán
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Tr?ng thái ph??ng th?c thanh toán (ACTIVE, INACTIVE)
    /// </summary>
    [Column("status")]
    public PaymentMethodStatus Status { get; set; } = PaymentMethodStatus.ACTIVE;

    /// <summary>
    /// Navigation property - Danh sách các payment s? d?ng ph??ng th?c này
    /// </summary>
    public virtual ICollection<PaymentEntity> Payments { get; set; } = new List<PaymentEntity>();
}