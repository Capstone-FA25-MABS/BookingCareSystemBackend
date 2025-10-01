using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Response for appointment operations - flexible for different user roles
/// </summary>
public class AppointmentResponse
{
    public Guid Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public Guid AppointmentTimeId { get; set; }
    public AppointmentType AppointmentType { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

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
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// Doctor information from gRPC call
/// </summary>
public class DoctorInfo
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? HospitalId { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Status { get; set; }
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
/// Hospital information from gRPC call (to be implemented)
/// </summary>
public class HospitalInfo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
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

