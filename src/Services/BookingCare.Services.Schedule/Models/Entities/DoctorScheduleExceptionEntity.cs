using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Exception types for schedule exceptions
/// </summary>
public enum ExceptionType
{
    BLOCK_SLOT,
    UNBLOCK_SLOT,
    DAY_OFF,
    CAPACITY_CHANGE
}

/// <summary>
/// Represents exceptions (override schedule: full day off, block slot or reopen slot)
/// </summary>
[Table("doctor_schedule_exceptions")]
public class DoctorScheduleExceptionEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("doctor_id")]
    public long DoctorId { get; set; }

    [Required]
    [Column("exception_date")]
    public DateOnly ExceptionDate { get; set; }

    [Column("appointment_time")]
    public AppointmentTime? AppointmentTime { get; set; } // NULL = full day off

    [Required]
    [StringLength(20)]
    [Column("exception_type")]
    public ExceptionType ExceptionType { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; } = false; // 0 = block slot/day, 1 = reopen slot

    [StringLength(255)]
    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


}