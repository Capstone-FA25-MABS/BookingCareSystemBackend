using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents daily schedules for doctors (according to pattern)
/// </summary>
[Table("doctor_daily_schedules")]
public class DoctorDailyScheduleEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("doctor_id")]
    public long DoctorId { get; set; }

    [Required]
    [Column("schedule_date")]
    public DateOnly ScheduleDate { get; set; }

    [Required]
    [Column("schedule_pattern")]
    public SchedulePatterns SchedulePattern { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}