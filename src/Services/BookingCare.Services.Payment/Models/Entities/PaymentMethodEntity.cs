using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho bảng payment_methods - Lookup table cho các phương thức thanh toán
/// </summary>
[Table("payment_methods")]
public class PaymentMethodEntity
{
    /// <summary>
    /// ID của phương thức thanh toán
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tên phương thức thanh toán (unique)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả phương thức thanh toán
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái phương thức thanh toán (ACTIVE, INACTIVE)
    /// </summary>
    [Column("status")]
    public PaymentMethodStatus Status { get; set; } = PaymentMethodStatus.ACTIVE;

    /// <summary>
    /// Navigation property - Danh sách các payment sử dụng phương thức này
    /// </summary>
    public virtual ICollection<PaymentEntity> Payments { get; set; } = new List<PaymentEntity>();
}