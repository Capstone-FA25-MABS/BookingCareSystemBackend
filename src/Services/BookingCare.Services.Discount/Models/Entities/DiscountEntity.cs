using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Discount.Models.Entities;

[Table("discounts")]
public class DiscountEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

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
    public long ClinicId { get; set; }

    [Column("specialty_id")]
    public long? SpecialtyId { get; set; }

    [Column("doctor_id")]
    public long? DoctorId { get; set; }

    [MaxLength(20)]
    [Column("applicable_to")]
    public string ApplicableTo { get; set; } = "ALL";

    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("discount_type")]
    public string DiscountType { get; set; } = string.Empty;

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
    public string Status { get; set; } = "ACTIVE";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Enums for better type safety
public enum DiscountApplicableTo
{
    ALL,
    SPECIALTY,
    DOCTOR
}

public enum DiscountType
{
    FIXED_AMOUNT,
    PERCENTAGE
}

public enum DiscountStatus
{
    ACTIVE,
    INACTIVE,
    EXPIRED
}
