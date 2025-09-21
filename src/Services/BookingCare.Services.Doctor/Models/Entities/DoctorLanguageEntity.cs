using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("doctor_languages")]
public class DoctorLanguageEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [Column("language_id")]
    public Guid LanguageId { get; set; }

    public virtual DoctorEntity Doctor { get; set; } = null!;
    public virtual LanguageEntity Language { get; set; } = null!;
}
