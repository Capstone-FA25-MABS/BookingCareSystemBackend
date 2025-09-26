using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.ServiceMedical.Models.Entities
{
    [Table("service_schedules")]
    public class ServiceScheduleEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("service_id")]
        public Guid ServiceId { get; set; }

        [Required]
        [Column("pattern_id")]
        public Guid PatternId { get; set; }

        [Column("hospital_id")]
        public Guid? HospitalId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 🔗 Navigation properties
        [ForeignKey("ServiceId")]
        public ServiceEntity? Service { get; set; }

        // Note: Add these navigation properties when the entities are available:
        // [ForeignKey("PatternId")]
        // public SchedulePatternEntity? SchedulePattern { get; set; }
        
        // [ForeignKey("HospitalId")]
        // public HospitalEntity? Hospital { get; set; }
    }
}
