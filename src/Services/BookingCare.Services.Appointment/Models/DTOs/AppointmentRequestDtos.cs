using System.ComponentModel.DataAnnotations;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Request to create a new appointment
/// </summary>
public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Patient ID is required")]
    public required Guid PatientId { get; set; }

    public Guid? DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    [Required(ErrorMessage = "Appointment date is required")]
    public required DateTime AppointmentDate { get; set; }

    [Required(ErrorMessage = "Appointment time is required")]
    public required AppointmentTime AppointmentTimeId { get; set; }

    public Guid? HospitalId { get; set; }

    [Required(ErrorMessage = "Appointment type is required")]
    public required AppointmentType AppointmentType { get; set; } = AppointmentType.IN_PERSON;

    public AppointmentStatus? Status { get; set; }
}

/// <summary>
/// Request to update appointment status
/// </summary>
public class UpdateAppointmentStatusRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid Id { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public required AppointmentStatus Status { get; set; }

    [MaxLength(4000, ErrorMessage = "Result cannot exceed 4000 characters")]
    public string? Result { get; set; }
}

/// <summary>
/// Request to query appointments with filtering and pagination
/// </summary>
public class AppointmentQueryRequest
{
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? ServiceId { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public AppointmentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Search term for filtering appointments
    /// Note: Currently handled client-side in frontend for better UX
    /// (allows searching doctor/hospital/service names from gRPC data)
    /// </summary>
    public string? SearchTerm { get; set; }

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Sorting
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;

    // Additional options
    /// <summary>
    /// Include counts for all statuses in the response
    /// </summary>
    public bool IncludeStatusCounts { get; set; } = false;
}


