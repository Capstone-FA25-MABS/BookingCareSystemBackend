using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

/// <summary>
/// Entity for managing secure tokens for contract signing
/// </summary>
[Table("contract_signing_tokens")]
public class ContractSigningTokenEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to hospital registration
    /// </summary>
    [Required]
    [Column("registration_id")]
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Secure token for accessing the contract signing page
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires
    /// </summary>
    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used (contract signed)
    /// </summary>
    [Required]
    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When the token was used (contract signed)
    /// </summary>
    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the contract was signed
    /// </summary>
    [MaxLength(45)]
    [Column("signed_from_ip")]
    public string? SignedFromIp { get; set; }

    /// <summary>
    /// User agent of the device used for signing
    /// </summary>
    [MaxLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the token was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual HospitalRegistrationEntity? Registration { get; set; }
}
