namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// DTO containing appointment booking email data
/// Used to resolve SonarQube issue: Method has too many parameters
/// </summary>
public class AppointmentBookingEmailData
{
    /// <summary>
    /// Patient name
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Appointment date
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Appointment time (formatted string)
    /// </summary>
    public string AppointmentTime { get; set; } = string.Empty;

    /// <summary>
    /// Doctor name (optional)
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Doctor specialty (optional)
    /// </summary>
    public string? DoctorSpecialty { get; set; }

    /// <summary>
    /// Hospital name (optional)
    /// </summary>
    public string? HospitalName { get; set; }

    /// <summary>
    /// Hospital address (optional)
    /// </summary>
    public string? HospitalAddress { get; set; }

    /// <summary>
    /// Service name (optional)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Appointment type
    /// </summary>
    public string AppointmentType { get; set; } = string.Empty;
}