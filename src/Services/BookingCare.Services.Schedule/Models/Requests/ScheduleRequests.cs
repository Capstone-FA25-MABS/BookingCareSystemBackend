using System.ComponentModel.DataAnnotations;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;

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
    public long DoctorId { get; set; }

    [Required]
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
    public long DoctorId { get; set; }

    [Required]
    public DateOnly ExceptionDate { get; set; }

    public AppointmentTime? AppointmentTime { get; set; } // NULL for full day off

    [Required]
    public ExceptionType ExceptionType { get; set; }

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
    public long ClinicId { get; set; }

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
    public long ServiceId { get; set; }

    [Required]
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();

    public long? ClinicId { get; set; }
}

/// <summary>
/// Request to get available slots for a doctor
/// </summary>
public class GetAvailableSlotsRequest
{
    [Required]
    public long DoctorId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    public long? ServiceId { get; set; }
}

/// <summary>
/// Request to get doctor schedule for a date range
/// </summary>
public class GetDoctorScheduleRequest
{
    [Required]
    public long DoctorId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}