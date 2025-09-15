using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.Entities;

[Table("Users")]
public class UserEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public Gender? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Address { get; set; }

    [MaxLength(10)]
    public string? Phone { get; set; }

    public string AvatarUrl { get; set; } = "https://bookingcaree.com/user-avatar-default.png";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}
