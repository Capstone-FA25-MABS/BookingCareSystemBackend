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

    // Navigation properties
    public virtual ICollection<AccountRoleEntity> UserRoles { get; set; } = new List<AccountRoleEntity>();
}