using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Auth.Models.Entities;

/// <summary>
/// Entity for storing refresh tokens with high security
/// </summary>
[Table("RefreshTokens")]
public class RefreshTokenEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(450)] // Indexed field
    public string Token { get; set; } = string.Empty;

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    // Navigation property
    [ForeignKey(nameof(AccountId))]
    public virtual AccountEntity Account { get; set; } = null!;
}
