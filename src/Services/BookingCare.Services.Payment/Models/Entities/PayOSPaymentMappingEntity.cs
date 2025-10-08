using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Payment.Models.Entities;

/// <summary>
/// Entity for mapping between PaymentId and PayOS OrderCode
/// Temporary table to store information during the payment process
/// Records will be deleted after the payment completes to save storage
/// </summary>
[Table("payos_payment_mappings")]
public class PayOSPaymentMappingEntity
{
    /// <summary>
    /// ID of the mapping (Primary key)
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the payment in the system
    /// </summary>
    [Required]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Order code from PayOS (unique)
    /// </summary>
    [Required]
    [Column("order_code")]
    public long OrderCode { get; set; }

    /// <summary>
    /// Creation time of the mapping
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration time (used for automatic cleanup)
    /// </summary>
    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public PayOSPaymentMappingEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        // Default expiration after 24 hours (allow PayOS timeout + buffer)
        ExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Constructor with parameters
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