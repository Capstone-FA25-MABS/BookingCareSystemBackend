using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_specialties")]
public class HospitalSpecialtyEntity
{
    [Key, Column("hospital_id", Order = 0)]
    public Guid HospitalId { get; set; }

    [Key, Column("specialty_id", Order = 1)]
    public Guid SpecialtyId { get; set; }

    // Navigation properties
    [ForeignKey("HospitalId")]
    public virtual HospitalEntity Hospital { get; set; } = null!;

    // Note: SpecialtyEntity should be defined in a shared project or referenced from another service
    // For now, we'll assume it exists in the system
}
