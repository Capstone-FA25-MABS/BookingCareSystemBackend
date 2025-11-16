using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Models.Entities;

[Table("Appointments")]
public class AppointmentEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Account ID of the patient who booked the appointment
    /// </summary>
    public Guid? PatientAccountId { get; set; }

    public Guid? DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? SpecialtyId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public AppointmentTime AppointmentTimeId { get; set; }

    public Guid? HospitalId { get; set; }

    [Required]
    public AppointmentType AppointmentType { get; set; } = AppointmentType.IN_PERSON;

    [Required]
    [MaxLength(20)]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.PENDING;

    [MaxLength(4000)]
    public string? Reason { get; set; }

    [MaxLength(4000)]
    public string? Result { get; set; }

    [MaxLength(4000)]
    public string? Symptoms { get; set; }

    [MaxLength(2000)]
    public string? AttachmentUrls { get; set; }

    // Cancellation information
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Reschedule tracking
    public bool IsRescheduled { get; set; } = false;

    [MaxLength(100)]
    public string? RescheduleToken { get; set; }
    public DateTime? RescheduleTokenExpiry { get; set; }

    /// <summary>
    /// Pending reschedule action type (for lazy token generation)
    /// Null = no pending action, SAME_DOCTOR = reschedule with same doctor, NEW_DOCTOR = choose new doctor
    /// This allows user to initiate reschedule without cancelling appointment immediately
    /// </summary>
    [MaxLength(20)]
    public string? PendingRescheduleAction { get; set; }

    // Soft reservation for staff-assigned doctor (Option 2)
    /// <summary>
    /// Doctor ID assigned by staff, pending patient confirmation
    /// This creates a soft lock on the doctor's schedule until patient confirms or token expires
    /// </summary>
    public Guid? AssignedDoctorId { get; set; }

    /// <summary>
    /// Soft reservation expiry timestamp
    /// After this time, the assigned doctor slot is released and available for other bookings
    /// </summary>
    public DateTime? SoftReservedUntil { get; set; }

    // Pending doctor change for supplementary payment (Option 3 - Scenario 2: Higher price)
    /// <summary>
    /// Pending new doctor ID (will be applied after successful supplementary payment)
    /// This is used when patient chooses new doctor with higher price
    /// </summary>
    public Guid? PendingNewDoctorId { get; set; }

    /// <summary>
    /// Pending new appointment date (will be applied after successful supplementary payment)
    /// </summary>
    public DateTime? PendingNewAppointmentDate { get; set; }

    /// <summary>
    /// Pending new appointment time (will be applied after successful supplementary payment)
    /// </summary>
    public AppointmentTime? PendingNewAppointmentTimeId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
