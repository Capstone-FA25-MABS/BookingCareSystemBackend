using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Response for appointment operations
/// </summary>
public class AppointmentResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public Guid AppointmentTimeId { get; set; }
    public Guid? HospitalId { get; set; }
    public AppointmentType AppointmentType { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response for appointment list with pagination
/// </summary>
public class AppointmentListResponse
{
    public List<AppointmentResponse> Appointments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

