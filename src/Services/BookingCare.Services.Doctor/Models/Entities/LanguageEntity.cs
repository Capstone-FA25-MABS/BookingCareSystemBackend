using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("languages")]
public class LanguageEntity : BaseEntity
{
    [MaxLength(100)]
    [Column("name")]
    public new string Name { get; set; } = string.Empty;

    public virtual ICollection<DoctorLanguageEntity> DoctorLanguages { get; set; } = new List<DoctorLanguageEntity>();
}
