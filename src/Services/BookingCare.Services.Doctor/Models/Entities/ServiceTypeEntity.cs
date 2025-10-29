using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("doctor_service_types")]
public class ServiceTypeEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty; // Ví dụ: IN_PERSON, TELEHEALTH, HOME_VISIT

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public Status Status { get; set; } = Status.ACTIVE;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<DoctorPriceEntity> DoctorPrices { get; set; } = new List<DoctorPriceEntity>();
}
