using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

/// <summary>
/// Entity for storing admin signature information for contract signing
/// </summary>
[Table("admin_signatures")]
public class AdminSignatureEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Admin account ID from Auth service
    /// </summary>
    [Required]
    [Column("admin_id")]
    public string AdminId { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the admin
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Position/Title of the admin (e.g., "Giám đốc", "Trưởng phòng")
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("position")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Signature image URL (stored in blob storage)
    /// </summary>
    [Required]
    [Column("signature_image_url")]
    public string SignatureImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether this signature is currently active
    /// </summary>
    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the signature was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the signature was last updated
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
