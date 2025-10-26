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

    // Navigation properties
    public virtual ICollection<AccountRoleEntity> UserRoles { get; set; } = new List<AccountRoleEntity>();
}