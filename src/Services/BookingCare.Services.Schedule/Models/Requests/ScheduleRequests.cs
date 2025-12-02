using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Schedule.Models.Requests;

/// <summary>
/// Request to create an appointment time slot
/// </summary>
public class CreateAppointmentTimeRequest
{
    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Start time must be in HH:mm format")]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "End time must be in HH:mm format")]
    public string EndTime { get; set; } = string.Empty;
}

/// <summary>
/// Request to create or update doctor daily schedule
/// </summary>
public class CreateDoctorDailyScheduleRequest
{
    [Required]
    [JsonRequired]
    public Guid DoctorId { get; set; }

    [Required]
    [JsonRequired]
    public DateOnly ScheduleDate { get; set; }

    [Required]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();
}

/// <summary>
/// Request to create doctor schedule exception
/// </summary>
public class CreateDoctorScheduleExceptionRequest
{
    [Required]
    [JsonRequired]
    public Guid DoctorId { get; set; }

    [Required]
    [JsonRequired]
    public DateOnly ExceptionDate { get; set; }

    public List<AppointmentTime>? AppointmentTimes { get; set; } // NULL or empty for full day off

    [Required]
    [JsonRequired]
    public ExceptionType ExceptionType { get; set; }

    [Required]
    public bool IsAvailable { get; set; } = false;

    [StringLength(255)]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to create clinic exception
/// </summary>
public class CreateClinicExceptionRequest
{
    [Required]
    public Guid ClinicId { get; set; }

    [Required]
    public DateOnly ExceptionDate { get; set; }

    [StringLength(255)]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to create service schedule
/// </summary>
public class CreateServiceScheduleRequest
{
    [Required]
    public Guid ServiceId { get; set; }

    [Required]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();

    public Guid? ClinicId { get; set; }
}

/// <summary>
/// Request to get available slots for a doctor
/// </summary>
public class GetAvailableSlotsRequest
{
    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    public Guid? ServiceId { get; set; }
}

/// <summary>
/// Request to get doctor schedule for a date range
/// </summary>
public class GetDoctorScheduleRequest
{
    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

/// <summary>
/// Request to create or update service medical daily schedule
/// </summary>
public class CreateServiceMedicalDailyScheduleRequest
{
    [Required]
    [JsonRequired]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    [JsonRequired]
    public DateOnly ScheduleDate { get; set; }

    [Required]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();
}

/// <summary>
/// Request to create service medical schedule exception
/// </summary>
public class CreateServiceMedicalScheduleExceptionRequest
{
    [Required]
    [JsonRequired]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    [JsonRequired]
    public DateOnly ExceptionDate { get; set; }

    public List<AppointmentTime>? AppointmentTimes { get; set; } // NULL or empty for full day off

    [Required]
    [JsonRequired]
    public ExceptionType ExceptionType { get; set; }

    [Required]
    public bool IsAvailable { get; set; } = false;

    [StringLength(255)]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to get service medical schedule for a date range
/// </summary>
public class GetServiceMedicalScheduleRequest
{
    [Required]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

/// <summary>
/// Request to get available slots for a service medical
/// </summary>
public class GetServiceMedicalAvailableSlotsRequest
{
    [Required]
    public Guid ServiceMedicalId { get; set; }

    [Required]
    public DateOnly Date { get; set; }
}

/// <summary>
/// Request to get aggregated available slots for a specialty (hospital assigns doctor mode)
/// This aggregates availability across all doctors in the specialty
/// </summary>
public class GetSpecialtyAvailableSlotsRequest
{
    [Required]
    public Guid HospitalId { get; set; }

    [Required]
    public Guid SpecialtyId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public AppointmentType AppointmentType { get; set; }
}