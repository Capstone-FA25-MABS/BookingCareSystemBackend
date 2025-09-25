using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [Column("pattern_id")]
    public long PatternId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PatternId")]
    public virtual SchedulePatternEntity Pattern { get; set; } = null!;
}