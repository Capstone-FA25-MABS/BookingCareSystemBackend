using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Discount.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Discount.Models.Entities;

[Table("discounts")]
public class DiscountEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("clinic_id")]
    public Guid ClinicId { get; set; }

    [Column("specialty_id")]
    public Guid? SpecialtyId { get; set; }

    [Column("doctor_id")]
    public Guid? DoctorId { get; set; }

    [MaxLength(20)]
    [Column("applicable_to")]
    public DiscountApplicableTo ApplicableTo { get; set; } = DiscountApplicableTo.ALL;

    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("discount_type")]
    public DiscountType DiscountType { get; set; } = DiscountType.PERCENTAGE;

    [Required]
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("max_uses")]
    public int? MaxUses { get; set; }

    [Column("uses_count")]
    public int UsesCount { get; set; } = 0;

    [MaxLength(10)]
    [Column("status")]
    public DiscountStatus Status { get; set; } = DiscountStatus.ACTIVE;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}