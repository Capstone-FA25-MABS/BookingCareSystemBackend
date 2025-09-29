using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents service schedules - defines what time slots a service can be performed
/// </summary>
[Table("service_schedules")]
public class ServiceScheduleEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("service_id")]
    public long ServiceId { get; set; }

    [Required]
    [Column("schedule_patterns")]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();

    [Column("clinic_id")]
    public long? ClinicId { get; set; } // if service applies only to one clinic

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}