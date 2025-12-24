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

/// <summary>
/// DTO for specialty available time slot with capacity information
/// Used for "hospital assigns doctor" mode where we aggregate availability across all doctors
/// </summary>
public class SpecialtyAvailableSlotDto
{
    public Guid Id { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// Number of doctors available for this time slot
    /// Slot is available if AvailableDoctorCount > 0
    /// </summary>
    public int AvailableDoctorCount { get; set; }

    /// <summary>
    /// Number of slots currently held by other users
    /// Available capacity = AvailableDoctorCount - HeldCount
    /// </summary>
    public int HeldCount { get; set; }

    /// <summary>
    /// Whether this slot is available for booking
    /// True if (AvailableDoctorCount - HeldCount) > 0
    /// </summary>
    public bool IsAvailable => (AvailableDoctorCount - HeldCount) > 0;
}

/// <summary>
/// Response DTO for specialty available slots
/// </summary>
public class SpecialtyAvailableSlotsResponseDto
{
    public Guid HospitalId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateOnly Date { get; set; }
    public AppointmentType AppointmentType { get; set; }

    /// <summary>
    /// Total number of doctors available for this specialty on this date
    /// </summary>
    public int TotalDoctorsAvailable { get; set; }

    /// <summary>
    /// List of available time slots with capacity information
    /// </summary>
    public List<SpecialtyAvailableSlotDto> AvailableSlots { get; set; } = new();
}