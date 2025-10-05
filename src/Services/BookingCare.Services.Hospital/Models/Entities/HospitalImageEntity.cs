using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_images")]
public class HospitalImageEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("hospital_id")]
    public Guid HospitalId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("s3_key")]
    public string S3Key { get; set; } = string.Empty;

    [Required]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("HospitalId")]
    public virtual HospitalEntity Hospital { get; set; } = null!;
}
