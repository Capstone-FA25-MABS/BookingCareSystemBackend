using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospitals")]
public class HospitalEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("account_id")]
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("background_url")]
    public string? BackgroundUrl { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<HospitalSubscriptionEntity> HospitalSubscriptions { get; set; } = new List<HospitalSubscriptionEntity>();
    public virtual ICollection<HospitalSpecialtyEntity> HospitalSpecialties { get; set; } = new List<HospitalSpecialtyEntity>();
    public virtual ICollection<HospitalImageEntity> HospitalImages { get; set; } = new List<HospitalImageEntity>();
}
