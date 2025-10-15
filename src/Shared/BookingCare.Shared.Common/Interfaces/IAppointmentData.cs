namespace BookingCare.Shared.Common.Interfaces;

/// <summary>
/// Common interface for appointment data
/// Used to eliminate SonarQube "Duplicated Lines" issue between Event and DTO classes
/// </summary>
public interface IAppointmentData
{
    /// <summary>
    /// Patient name for email/notification
    /// </summary>
    string PatientName { get; set; }

    /// <summary>
    /// Appointment date and time
    /// </summary>
    DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Formatted appointment time slot (e.g., "08:00 - 09:00")
    /// </summary>
    string AppointmentTime { get; set; }

    /// <summary>
    /// Doctor name (if assigned)
    /// </summary>
    string? DoctorName { get; set; }

    /// <summary>
    /// Doctor specialty (if assigned)
    /// </summary>
    string? DoctorSpecialty { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    string? HospitalName { get; set; }

    /// <summary>
    /// Hospital address
    /// </summary>
    string? HospitalAddress { get; set; }

    /// <summary>
    /// Service name (if applicable)
    /// </summary>
    string? ServiceName { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    decimal Amount { get; set; }

    /// <summary>
    /// Appointment type (e.g., "CONSULTATION", "CHECKUP")
    /// </summary>
    string AppointmentType { get; set; }
}