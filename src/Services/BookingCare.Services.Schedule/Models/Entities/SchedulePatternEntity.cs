using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents schedule patterns (full day, morning only, afternoon only, evening only)
/// </summary>
[Table("schedule_patterns")]
public class SchedulePatternEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = "FULL_DAY";

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<SchedulePatternSlotEntity> SchedulePatternSlots { get; set; } = new List<SchedulePatternSlotEntity>();
    public virtual ICollection<DoctorDailyScheduleEntity> DoctorDailySchedules { get; set; } = new List<DoctorDailyScheduleEntity>();
    public virtual ICollection<ServiceScheduleEntity> ServiceSchedules { get; set; } = new List<ServiceScheduleEntity>();
}