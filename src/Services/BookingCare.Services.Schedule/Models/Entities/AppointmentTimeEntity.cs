using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents appointment time slots with start and end times
/// </summary>
[Table("appointment_times")]
public class AppointmentTimeEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [StringLength(5)]
    [Column("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    [StringLength(5)]
    [Column("end_time")]
    public string EndTime { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<SchedulePatternSlotEntity> SchedulePatternSlots { get; set; } = new List<SchedulePatternSlotEntity>();
    public virtual ICollection<DoctorScheduleExceptionEntity> DoctorScheduleExceptions { get; set; } = new List<DoctorScheduleExceptionEntity>();
}