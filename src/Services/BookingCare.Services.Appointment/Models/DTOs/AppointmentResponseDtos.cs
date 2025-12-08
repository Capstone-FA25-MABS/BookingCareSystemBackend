using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.DTOs;

/// <summary>
/// Response for appointment operations - flexible for different user roles
/// </summary>
public class AppointmentResponse
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? PatientAccountId { get; set; }

    /// <summary>
    /// Relative ID when booking for a family member (null = booking for self)
    /// </summary>
    public Guid? RelativeId { get; set; }

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

    // Additional IDs for convenience (used for fetching available doctors, etc.)
    public Guid? SpecialtyId { get; set; }

    /// <summary>
    /// Original consultation/service fee at the time of booking (before any discounts)
    /// This is stored in the database for statistics and reporting
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Remaining payment amount for Staff role (Amount - Deposit from Payment)
    /// This field is only populated for Staff users to show how much the patient still needs to pay
    /// Calculation: Amount (total fee) - Payment.Amount (deposit already paid)
    /// For other roles (Admin, Doctor, Patient), this field remains null
    /// </summary>
    public decimal? ConsultationFees { get; set; }

    // Cancellation information
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Related entities information populated via gRPC calls based on user role
    public PatientInfo? PatientInfo { get; set; }
    public RelativeInfo? RelativeInfo { get; set; }
    public DoctorInfo? DoctorInfo { get; set; }
    public ServiceInfo? ServiceInfo { get; set; }
    public HospitalInfo? HospitalInfo { get; set; }
    public SpecialtyInfo? SpecialtyInfo { get; set; }
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
/// Relative (family member) information from gRPC call
/// </summary>
public class RelativeInfo
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Phone { get; set; }
    public string? Relationship { get; set; }
    public string? RelationshipDisplay { get; set; }
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
    public decimal? ConsultationFee { get; set; } // Consultation fee based on appointment type
}

/// <summary>
/// Service information from gRPC call
/// </summary>
public class ServiceInfo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
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
/// Specialty information from gRPC call (for hospital assigns doctor mode)
/// </summary>
public class SpecialtyInfo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
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

/// <summary>
/// Response for reschedule operations with deep links for all 4 options
/// </summary>
public class RescheduleResponse
{
    public Guid AppointmentId { get; set; }
    public string RescheduleToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
    public string Message { get; set; } = string.Empty;

    // Deep links for patient (4 options)
    /// <summary>
    /// Option 1: Reschedule with same doctor
    /// </summary>
    public string? SameDoctorRescheduleUrl { get; set; }

    /// <summary>
    /// Option 2: Confirm new doctor assigned by hospital staff
    /// URL contains placeholder {newDoctorId} that will be replaced when staff assigns a doctor
    /// </summary>
    public string? ConfirmNewDoctorUrl { get; set; }

    /// <summary>
    /// Option 3: Choose new doctor yourself (redirects to doctors list with filters)
    /// No API endpoint needed - just redirect to frontend page
    /// </summary>
    public string? ChooseNewDoctorUrl { get; set; }

    /// <summary>
    /// Option 4: Request refund
    /// </summary>
    public string? RefundRequestUrl { get; set; }
}

/// <summary>
/// Response for available doctors query
/// Returns doctors from same hospital + specialty that are available at specified date/time
/// </summary>
public class AvailableDoctorsResponse
{
    public List<AvailableDoctors> Doctors { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Available doctor information
/// </summary>
public class AvailableDoctors
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PositionName { get; set; }
    public string? SpecialtyName { get; set; }
    public int YearsOfExperience { get; set; }
}

/// <summary>
/// Response for choosing new doctor (Option 3)
/// Includes action to take based on price comparison
/// </summary>
public class ChooseNewDoctorResponse
{
    public Guid AppointmentId { get; set; }
    public string Action { get; set; } = string.Empty; // "direct_update", "payment_required", "refund_created"
    public decimal OriginalPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public string? PaymentUrl { get; set; } // For higher price scenario
    public string? RefundRequestId { get; set; } // For lower price scenario
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for lazy reschedule token generation
/// </summary>
public class GenerateRescheduleTokenResponse
{
    public string RescheduleToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for doctors for assignment query
/// Contains recommended doctors and previous doctors who treated this patient
/// </summary>
public class DoctorsForAssignmentResponse
{
    /// <summary>
    /// Recommended doctors sorted by experience, rating, booking count
    /// </summary>
    public List<DoctorForAssignment> RecommendedDoctors { get; set; } = new();

    /// <summary>
    /// Doctors who have previously treated this patient (completed appointments)
    /// </summary>
    public List<DoctorForAssignment> PreviousDoctors { get; set; } = new();

    public int TotalRecommended { get; set; }
    public int TotalPrevious { get; set; }
}

/// <summary>
/// Doctor information for assignment with full details
/// </summary>
public class DoctorForAssignment
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PositionName { get; set; }
    public string? SpecialtyName { get; set; }
    public int YearsOfExperience { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int BookingCount { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailableAtOriginalTime { get; set; }
}

/// <summary>
/// Response for assigning doctor to a pending specialty appointment
/// </summary>
public class AssignDoctorToAppointmentResponse
{
    public bool Success { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string AppointmentTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for rejecting a pending appointment
/// </summary>
public class RejectPendingAppointmentResponse
{
    public bool Success { get; set; }
    public Guid AppointmentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
    public bool PatientNotified { get; set; }
}
