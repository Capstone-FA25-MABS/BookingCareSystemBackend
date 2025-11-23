using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Schedule.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Base class for schedule exception entities with common properties
/// </summary>
public abstract class ScheduleExceptionEntityBase
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

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
