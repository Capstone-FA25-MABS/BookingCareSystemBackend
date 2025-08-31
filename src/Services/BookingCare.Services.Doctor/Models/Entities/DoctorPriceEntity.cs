using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("doctor_prices")]
public class DoctorPriceEntity
{
    [Key]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Key]
    [Column("price_id")]
    public Guid PriceId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public virtual DoctorEntity Doctor { get; set; } = null!;
    public virtual PriceEntity Price { get; set; } = null!;
}

