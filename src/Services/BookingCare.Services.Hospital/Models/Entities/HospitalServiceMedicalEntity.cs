using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_service_medicals")]
public class HospitalServiceMedicalEntity
{
    [Key, Column("hospital_id", Order = 0)]
    public Guid HospitalId { get; set; }

    [Key, Column("service_medical_id", Order = 1)]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("HospitalId")]
    public virtual HospitalEntity Hospital { get; set; } = null!;

    // Note: ServiceEntity is defined in ServiceMedical service
    // For now, we'll store only the ID reference
}
