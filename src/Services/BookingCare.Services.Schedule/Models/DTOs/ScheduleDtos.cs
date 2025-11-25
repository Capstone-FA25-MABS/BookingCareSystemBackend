using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.DTOs;

/// <summary>
/// DTO for appointment time slots
/// </summary>
public class AppointmentTimeDto
{
    public Guid Id { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

/// <summary>
/// DTO for doctor daily schedule
/// </summary>
public class DoctorDailyScheduleDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for doctor schedule exceptions
/// </summary>
public class DoctorScheduleExceptionDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
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
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for service schedules
/// </summary>
public class ServiceScheduleDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();
    public Guid? ClinicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for service medical daily schedule
/// </summary>
public class ServiceMedicalDailyScheduleDto
{
    public Guid Id { get; set; }
    public Guid ServiceMedicalId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public List<SchedulePatterns> SchedulePatterns { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for service medical schedule exceptions
/// </summary>
public class ServiceMedicalScheduleExceptionDto
{
    public Guid Id { get; set; }
    public Guid ServiceMedicalId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public AppointmentTime? AppointmentTime { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}