using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookingCare.Services.Auth.Models.Entities;

[Table("AspNetUserRoles")]
public class AccountRoleEntity : IdentityUserRole<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual AccountEntity User { get; set; } = null!;
    public virtual RoleEntity Role { get; set; } = null!;
}