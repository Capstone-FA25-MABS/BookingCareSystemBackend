using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.DTOs;

/// <summary>
/// DTO for appointment time slots
/// </summary>
public class AppointmentTimeDto
{
    public long Id { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

/// <summary>
/// DTO for doctor daily schedule
/// </summary>
public class DoctorDailyScheduleDto
{
    public long Id { get; set; }
    public long DoctorId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public SchedulePatterns SchedulePattern { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for doctor schedule exceptions
/// </summary>
public class DoctorScheduleExceptionDto
{
    public long Id { get; set; }
    public long DoctorId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public AppointmentTime? AppointmentTime { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for clinic exceptions
/// </summary>
public class ClinicExceptionDto
{
    public long Id { get; set; }
    public long ClinicId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for service schedules
/// </summary>
public class ServiceScheduleDto
{
    public long Id { get; set; }
    public long ServiceId { get; set; }
    public SchedulePatterns SchedulePattern { get; set; }
    public long? ClinicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}