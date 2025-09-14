using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("doctors")]
public class DoctorEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("account_id")]
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("gender")]
    public Gender? Gender { get; set; }

    [Column("position_id")]
    public Guid? PositionId { get; set; }

    [Column("specialty_id")]
    public Guid? SpecialtyId { get; set; }

    [Column("clinic_id")]
    public Guid? ClinicId { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("years_of_experience")]
    public int YearsOfExperience { get; set; } = 0;

    [Column("avatar_url")]
    public string AvatarUrl { get; set; } = "https://bookingcaree.com/user-avatar-default.png";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual PositionEntity? Position { get; set; }
    public virtual ICollection<DoctorPriceEntity> DoctorPrices { get; set; } = new List<DoctorPriceEntity>();
    public virtual ICollection<DoctorLanguageEntity> DoctorLanguages { get; set; } = new List<DoctorLanguageEntity>();
}
