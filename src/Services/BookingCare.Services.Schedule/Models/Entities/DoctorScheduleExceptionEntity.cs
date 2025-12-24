using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents exceptions for doctor schedules (override schedule: full day off, block slot or reopen slot)
/// </summary>
[Table("doctor_schedule_exceptions")]
public class DoctorScheduleExceptionEntity : ScheduleExceptionEntityBase
{
    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }
}