using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;
using Microsoft.AspNetCore.Identity;

namespace BookingCare.Services.Auth.Models.Entities;

[Table("AspNetUsers")]
public class AccountEntity : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Status Status { get; set; } = Status.ACTIVE;

    /// <summary>
    /// Flag to indicate if user must change password on first login
    /// Used for doctor accounts with auto-generated passwords
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Secret key for TOTP (Time-based One-Time Password) 2FA
    /// </summary>
    public string? TwoFactorSecretKey { get; set; }

    /// <summary>
    /// Backup codes for 2FA recovery (stored as JSON array)
    /// </summary>
    public string? TwoFactorBackupCodes { get; set; }

    /// <summary>
    /// Timestamp when 2FA was enabled
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; set; }

    // Navigation properties
    public virtual ICollection<AccountRoleEntity> UserRoles { get; set; } = new List<AccountRoleEntity>();
}