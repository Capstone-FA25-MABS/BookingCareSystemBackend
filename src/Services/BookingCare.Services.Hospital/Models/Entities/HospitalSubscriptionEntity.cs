using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_subscriptions")]
public class HospitalSubscriptionEntity
{
    [Key]
    [Column("hospital_subscription_id")]
    public Guid HospitalSubscriptionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("hospital_id")]
    public Guid HospitalId { get; set; }

    [Required]
    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; }

    [Required]
    [Column("start_date")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Required]
    [Column("end_date")]
    public DateTime EndDate { get; set; } = DateTime.Now;

    [Column("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.ACTIVE;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Usage counts - Track actual usage against subscription limits
    [Column("doctor_count")]
    public int DoctorCount { get; set; } = 0;

    [Column("specialty_count")]
    public int SpecialtyCount { get; set; } = 0;

    [Column("appointment_count")]
    public int AppointmentCount { get; set; } = 0;

    [Column("service_count")]
    public int ServiceCount { get; set; } = 0;

    // Navigation properties
    [ForeignKey("HospitalId")]
    public virtual HospitalEntity Hospital { get; set; } = null!;

    [ForeignKey("SubscriptionId")]
    public virtual SubscriptionPlanEntity SubscriptionPlan { get; set; } = null!;
}
