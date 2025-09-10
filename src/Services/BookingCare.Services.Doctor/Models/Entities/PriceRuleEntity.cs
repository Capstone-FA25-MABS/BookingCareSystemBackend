using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("price_rules")]
public class PriceRuleEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("min_experience")]
    public int? MinExperience { get; set; }

    [Column("position")]
    [MaxLength(100)]
    public string? Position { get; set; }

    [Required]
    [Column("base_price", TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }

    [Column("status")]
    public Status Status { get; set; } = Status.ACTIVE;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
