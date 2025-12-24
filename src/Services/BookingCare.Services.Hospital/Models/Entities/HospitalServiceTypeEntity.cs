using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_service_types")]
public class HospitalServiceTypeEntity
{
    [Key, Column("hospital_id", Order = 0)]
    public Guid HospitalId { get; set; }

    [Key, Column("service_type_id", Order = 1)]
    public Guid ServiceTypeId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("HospitalId")]
    public virtual HospitalEntity Hospital { get; set; } = null!;

    // Note: ServiceTypeEntity is defined in Doctor service
    // For now, we'll store only the ID reference
}
