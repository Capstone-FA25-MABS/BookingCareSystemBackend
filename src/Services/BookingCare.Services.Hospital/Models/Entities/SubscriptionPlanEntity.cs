using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("subscription_plans")]
public class SubscriptionPlanEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(20)]
    [Column("billing_cycle")]
    public string BillingCycle { get; set; } = "MONTHLY";

    [Column("max_doctors")]
    public int? MaxDoctors { get; set; } = 0; // null = unlimited

    [Column("max_specialties")]
    public int? MaxSpecialties { get; set; } = 0; // null = unlimited

    [Column("max_appointments")]
    public int? MaxAppointments { get; set; } = 0; // null = unlimited

    [Column("features")]
    public string? Features { get; set; }

    [Column("status")]
    public Status Status { get; set; } = Status.ACTIVE;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<HospitalSubscriptionEntity> HospitalSubscriptions { get; set; } = new List<HospitalSubscriptionEntity>();
}
