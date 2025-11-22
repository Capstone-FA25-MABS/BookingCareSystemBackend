using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

    /// <summary>
    /// Account ID of the patient who booked the appointment
    /// Links to the Account service for user authentication and authorization
    /// </summary>
    [Required(ErrorMessage = "Patient Account ID is required")]
    public required Guid PatientAccountId { get; set; }

    public Guid? DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? SpecialtyId { get; set; }

    [Required(ErrorMessage = "Appointment date is required")]
    public required DateTime AppointmentDate { get; set; }

    [Required(ErrorMessage = "Appointment time is required")]
    public required AppointmentTime AppointmentTimeId { get; set; }

    public Guid? HospitalId { get; set; }

    [Required(ErrorMessage = "Appointment type is required")]
    public required AppointmentType AppointmentType { get; set; } = AppointmentType.IN_PERSON;

    public AppointmentStatus? Status { get; set; }

    [MaxLength(4000, ErrorMessage = "Symptoms cannot exceed 4000 characters")]
    public string? Symptoms { get; set; }

    /// <summary>
    /// Comma-separated URLs of attachment files
    /// Example: "url1,url2,url3"
    /// Supports multiple files for cases like follow-up appointments, medical records, etc.
    /// </summary>
    [MaxLength(4000, ErrorMessage = "Attachment URLs cannot exceed 4000 characters")]
    public string? AttachmentUrls { get; set; }

    /// <summary>
    /// Indicates whether to skip payment and send booking confirmation email immediately.
    /// If true, no payment will be required and email notification will be sent right after appointment creation.
    /// If false (default), payment will be required and email will be sent after successful payment.
    /// </summary>
    public bool SkipPayment { get; set; } = false;
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

    [Required(ErrorMessage = "Result is required")]
    [MaxLength(4000, ErrorMessage = "Result cannot exceed 4000 characters")]
    public required string Result { get; set; }
}

/// <summary>
/// Request to cancel an appointment
/// </summary>
public class CancelAppointmentRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Cancellation reason is required")]
    [MaxLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
    public required string CancellationReason { get; set; }

    /// <summary>
    /// Optional: ID of the staff who cancelled the appointment
    /// </summary>
    public Guid? CancelledByStaffId { get; set; }

    /// <summary>
    /// Optional: ID of the patient who cancelled the appointment
    /// </summary>
    public Guid? CancelledByPatientId { get; set; }

    /// <summary>
    /// Enable reschedule options for patient (staff cancellation only)
    /// </summary>
    public bool EnableRescheduleOptions { get; set; } = true;

    /// <summary>
    /// Specific reschedule options enabled by staff (only relevant if EnableRescheduleOptions = true)
    /// </summary>
    public RescheduleOptionsSelection? RescheduleOptions { get; set; }
}

/// <summary>
/// Specifies which reschedule options are enabled by staff
/// </summary>
public class RescheduleOptionsSelection
{
    /// <summary>
    /// Option 1: Allow patient to reschedule with same doctor
    /// </summary>
    [JsonRequired]
    public bool EnableSameDoctorReschedule { get; set; }

    /// <summary>
    /// Option 2: Allow staff to assign new doctor
    /// </summary>
    [JsonRequired]
    public bool EnableNewDoctorAssignment { get; set; }

    /// <summary>
    /// Option 3: Allow patient to choose new doctor
    /// </summary>
    [JsonRequired]
    public bool EnableDoctorSelection { get; set; }

    /// <summary>
    /// Option 4: Allow patient to request refund
    /// </summary>
    [JsonRequired]
    public bool EnableRefundRequest { get; set; }
}

/// <summary>
/// Request to reschedule appointment with same doctor
/// </summary>
public class RescheduleSameDoctorRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Reschedule token is required")]
    public required string RescheduleToken { get; set; }

    [Required(ErrorMessage = "New appointment date is required")]
    public required DateTime NewAppointmentDate { get; set; }

    [Required(ErrorMessage = "New appointment time is required")]
    public required AppointmentTime NewAppointmentTimeId { get; set; }
}

/// <summary>
/// Request for staff to assign new doctor (creates soft reservation)
/// This is called by STAFF to assign a replacement doctor
/// Creates a soft lock on the doctor's schedule until patient confirms
/// </summary>
public class AssignNewDoctorRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "New doctor ID is required")]
    public required Guid NewDoctorId { get; set; }

    /// <summary>
    /// Optional: New appointment date (if different from original)
    /// If not provided, keeps original appointment date
    /// </summary>
    public DateTime? NewAppointmentDate { get; set; }

    /// <summary>
    /// Optional: New appointment time (if different from original)
    /// If not provided, keeps original appointment time
    /// </summary>
    public AppointmentTime? NewAppointmentTimeId { get; set; }

    /// <summary>
    /// Staff ID who is assigning the new doctor
    /// </summary>
    [Required(ErrorMessage = "Staff ID is required")]
    public required Guid AssignedByStaffId { get; set; }

    /// <summary>
    /// Reason for cancelling the original appointment (required if appointment is not yet cancelled)
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Optional note from staff about the assignment
    /// </summary>
    [MaxLength(500)]
    public string? StaffNote { get; set; }
}

/// <summary>
/// Request refund for cancelled appointment
/// </summary>
public class RequestRefundRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Reschedule token is required")]
    public required string RescheduleToken { get; set; }
}

/// <summary>
/// Request to choose new doctor (Option 3)
/// Used when patient selects a different doctor from same hospital + specialty
/// Handles 3 scenarios: same price, higher price, lower price
/// </summary>
public class ChooseNewDoctorRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Reschedule token is required")]
    [MaxLength(100)]
    public required string RescheduleToken { get; set; }

    [Required(ErrorMessage = "New doctor ID is required")]
    public required Guid NewDoctorId { get; set; }

    [Required(ErrorMessage = "New appointment date is required")]
    public required DateTime NewAppointmentDate { get; set; }

    [Required(ErrorMessage = "New appointment time is required")]
    public required AppointmentTime NewAppointmentTimeId { get; set; }

    /// <summary>
    /// Price ID of the new doctor's service
    /// Used to calculate price difference
    /// </summary>
    [Required(ErrorMessage = "Doctor price ID is required")]
    public required Guid DoctorPriceId { get; set; }

    /// <summary>
    /// Flag to indicate if this is a staff-assigned doctor (Option 2)
    /// True: Use ConfirmNewDoctorAsync (staff assigned)
    /// False: Use UpdateAppointmentWithNewDoctorAsync (patient chose)
    /// </summary>
    public bool IsStaffAssigned { get; set; } = false;
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

    /// <summary>
    /// Filter by single status (for backward compatibility)
    /// </summary>
    public AppointmentStatus? Status { get; set; }

    /// <summary>
    /// Filter by multiple statuses (e.g., for calendar view showing only CONFIRMED and COMPLETED)
    /// Takes precedence over Status if provided
    /// </summary>
    public List<AppointmentStatus>? Statuses { get; set; }

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

/// <summary>
/// Request to generate reschedule token without cancelling appointment (lazy token generation)
/// Used when patient clicks reschedule/choose new doctor button
/// </summary>
public class GenerateRescheduleTokenRequest
{
    [Required(ErrorMessage = "Appointment ID is required")]
    public required Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Reschedule action is required")]
    [MaxLength(20, ErrorMessage = "Reschedule action cannot exceed 20 characters")]
    public required string RescheduleAction { get; set; } // "SAME_DOCTOR" or "NEW_DOCTOR"

    [Required(ErrorMessage = "Patient ID is required")]
    public required Guid PatientId { get; set; }
}
