using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents daily schedules for medical services (according to pattern)
/// </summary>
[Table("service_medical_daily_schedules")]
public class ServiceMedicalDailyScheduleEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("service_medical_id")]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    [Column("schedule_date")]
    public DateOnly ScheduleDate { get; set; }

    [Required]
    [Column("schedule_patterns")]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
