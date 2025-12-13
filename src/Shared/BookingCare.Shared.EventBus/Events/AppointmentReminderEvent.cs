namespace BookingCare.Shared.EventBus.Events;

/// <summary>
/// Event published when an appointment reminder should be sent to patient
/// This event is consumed by Notification Service to send reminders via:
/// - SignalR (real-time notification bell)
/// - SMS
/// - Email
/// </summary>
public class AppointmentReminderEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the appointment
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Account ID of the patient (for SignalR notification)
    /// </summary>
    public Guid PatientAccountId { get; set; }

    /// <summary>
    /// Patient email address
    /// </summary>
    public string PatientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Patient phone number
    /// </summary>
    public string? PatientPhone { get; set; }

    /// <summary>
    /// Patient full name
    /// </summary>
    public string PatientFullName { get; set; } = string.Empty;

    /// <summary>
    /// Doctor name (if assigned)
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// Hospital address
    /// </summary>
    public string? HospitalAddress { get; set; }

    /// <summary>
    /// Specialty name (if applicable)
    /// </summary>
    public string? SpecialtyName { get; set; }

    /// <summary>
    /// Service name (if applicable)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Appointment date (UTC)
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Appointment time slot display (e.g., "08:00 - 08:30")
    /// </summary>
    public string AppointmentTime { get; set; } = string.Empty;

    /// <summary>
    /// Reminder type: "24_HOURS" or "1_HOUR"
    /// </summary>
    public string ReminderType { get; set; } = string.Empty;

    /// <summary>
    /// Hours before appointment (24 or 1)
    /// </summary>
    public int HoursBeforeAppointment { get; set; }

    /// <summary>
    /// When the reminder was triggered
    /// </summary>
    public DateTime ReminderSentAt { get; set; }
}
