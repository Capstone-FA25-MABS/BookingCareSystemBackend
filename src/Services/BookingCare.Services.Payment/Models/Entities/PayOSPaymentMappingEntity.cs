using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity cho b?ng mapping gi?a PaymentId và OrderCode c?a PayOS
/// B?ng t?m th?i ?? l?u thông tin trong quá trình thanh toán
/// S? ???c xóa sau khi thanh toán hoàn t?t ?? ti?t ki?m dung l??ng
/// </summary>
[Table("payos_payment_mappings")]
public class PayOSPaymentMappingEntity
{
    /// <summary>
    /// ID c?a mapping (Primary key)
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// ID c?a payment trong h? th?ng
    /// </summary>
    [Required]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Order code t? PayOS (unique)
    /// </summary>
    [Required]
    [Column("order_code")]
    public long OrderCode { get; set; }

    /// <summary>
    /// Th?i gian t?o mapping
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Th?i gian h?t h?n (?? cleanup t? ??ng)
    /// </summary>
    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Constructor m?c ??nh
    /// </summary>
    public PayOSPaymentMappingEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        // M?c ??nh h?t h?n sau 24 gi? (?? th?i gian cho PayOS timeout + buffer)
        ExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Constructor v?i parameters
    /// </summary>
    public PayOSPaymentMappingEntity(Guid paymentId, long orderCode, DateTime? expiresAt = null)
        : this()
    {
        PaymentId = paymentId;
        OrderCode = orderCode;
        if (expiresAt.HasValue)
        {
            ExpiresAt = expiresAt.Value;
        }
    }
}