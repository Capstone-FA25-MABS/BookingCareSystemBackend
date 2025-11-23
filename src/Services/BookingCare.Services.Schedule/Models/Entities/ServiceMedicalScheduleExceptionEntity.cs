using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Schedule.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents exceptions for service medical schedules (override schedule: full day off, block slot or reopen slot)
/// </summary>
[Table("service_medical_schedule_exceptions")]
public class ServiceMedicalScheduleExceptionEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("service_medical_id")]
    public Guid ServiceMedicalId { get; set; }

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
