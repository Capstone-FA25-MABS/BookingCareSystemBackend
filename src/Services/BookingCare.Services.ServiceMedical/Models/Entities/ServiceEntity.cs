using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.ServiceMedical.Models.Entities
{
    [Table("services")]
    public class ServiceEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("duration_time")]
        public int DurationTime { get; set; }

        [Required]
        [Column("hospital_id")]
        public Guid HospitalId { get; set; }

        [Column("service_category_id")]
        public Guid? ServiceCategoryId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("status")]
        public string Status { get; set; } = "INACTIVE";

        // 🔗 Navigation properties
        [ForeignKey("ServiceCategoryId")]
        public ServiceCategoryEntity? ServiceCategory { get; set; }

        public ICollection<ServiceScheduleEntity> Schedules { get; set; } = new List<ServiceScheduleEntity>();
    }
}
