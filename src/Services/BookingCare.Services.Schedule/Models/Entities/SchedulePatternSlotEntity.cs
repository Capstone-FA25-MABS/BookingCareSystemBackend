using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents slots that belong to a pattern (pattern = collection of slots)
/// </summary>
[Table("schedule_pattern_slots")]
public class SchedulePatternSlotEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("pattern_id")]
    public long PatternId { get; set; }

    [Required]
    [Column("appointment_time_id")]
    public long AppointmentTimeId { get; set; }

    // Navigation properties
    [ForeignKey("PatternId")]
    public virtual SchedulePatternEntity Pattern { get; set; } = null!;

    [ForeignKey("AppointmentTimeId")]
    public virtual AppointmentTimeEntity AppointmentTime { get; set; } = null!;
}