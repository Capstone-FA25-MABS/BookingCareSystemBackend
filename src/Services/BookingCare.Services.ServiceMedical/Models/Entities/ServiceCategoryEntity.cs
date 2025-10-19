using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.ServiceMedical.Models.Entities
{
    [Table("service_categories")]
    public class ServiceCategoryEntity
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

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("parent_id")]
        public Guid? ParentId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("status")]
        public string Status { get; set; } = "INACTIVE";

        // 🔗 Navigation properties
        [ForeignKey("ParentId")]
        public ServiceCategoryEntity? Parent { get; set; }

        public ICollection<ServiceCategoryEntity> Children { get; set; } = new List<ServiceCategoryEntity>();
        public ICollection<ServiceEntity> Services { get; set; } = new List<ServiceEntity>();
    }
}
