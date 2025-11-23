using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents exceptions for service medical schedules (override schedule: full day off, block slot or reopen slot)
/// </summary>
[Table("service_medical_schedule_exceptions")]
public class ServiceMedicalScheduleExceptionEntity : ScheduleExceptionEntityBase
{
    [Required]
    [Column("service_medical_id")]
    public Guid ServiceMedicalId { get; set; }
}
