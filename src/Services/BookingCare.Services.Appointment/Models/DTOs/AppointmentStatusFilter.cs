using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Filter parameters for appointment status counting.
/// Encapsulates all optional filters into a single object to keep method
/// signatures focused and maintainable.
/// </summary>
public sealed class AppointmentStatusFilter
{
    public Guid? PatientId { get; init; }
    public Guid? DoctorId { get; init; }
    public Guid? HospitalId { get; init; }
    public bool CountAll { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public AppointmentType? AppointmentType { get; init; }
    public bool? ForRelative { get; init; }
    public string? SearchTerm { get; init; }
}


