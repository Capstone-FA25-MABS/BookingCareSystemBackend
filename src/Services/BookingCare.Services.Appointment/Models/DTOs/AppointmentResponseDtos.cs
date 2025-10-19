using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Response for appointment operations - flexible for different user roles
/// </summary>
public class AppointmentResponse
{
    public Guid Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public AppointmentTime AppointmentTimeId { get; set; }
    public AppointmentType AppointmentType { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Result { get; set; }
    public string? Symptoms { get; set; }
    public string? AttachmentUrls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Payment information from gRPC call
    public decimal? ConsultationFees { get; set; }

    // Cancellation information
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Related entities information populated via gRPC calls based on user role
    public PatientInfo? PatientInfo { get; set; }
    public DoctorInfo? DoctorInfo { get; set; }
    public ServiceInfo? ServiceInfo { get; set; }
    public HospitalInfo? HospitalInfo { get; set; }
}

/// <summary>
/// Patient information from gRPC call
/// </summary>
public class PatientInfo
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Doctor information from gRPC call
/// </summary>
public class DoctorInfo
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? PositionName { get; set; }
    public string? SpecialtyName { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? HospitalId { get; set; }
}

/// <summary>
/// Service information from gRPC call (to be implemented)
/// </summary>
public class ServiceInfo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Hospital information from gRPC call
/// </summary>
public class HospitalInfo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Status counts for all appointment statuses
/// </summary>
public class AppointmentStatusCounts
{
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Cancelled { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
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

    /// <summary>
    /// Counts for each status - only populated when requesting all statuses
    /// </summary>
    public AppointmentStatusCounts? StatusCounts { get; set; }
}

