namespace BookingCare.Shared.Common.Interfaces;

/// <summary>
/// Abstract base class for appointment data
/// Used to eliminate SonarQube "Duplicated Lines" issue between Event and DTO classes
/// This approach ensures properties are defined only once in the base class
/// </summary>
public abstract class AppointmentDataBase
{
    /// <summary>
    /// Patient name for email/notification
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Appointment date and time
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Formatted appointment time slot (e.g., "08:00 - 09:00")
    /// </summary>
    public string AppointmentTime { get; set; } = string.Empty;

    /// <summary>
    /// Doctor name (if assigned)
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Doctor specialty (if assigned)
    /// </summary>
    public string? DoctorSpecialty { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string? HospitalName { get; set; }

    /// <summary>
    /// Hospital address
    /// </summary>
    public string? HospitalAddress { get; set; }

    /// <summary>
    /// Service name (if applicable)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Appointment type (e.g., "CONSULTATION", "CHECKUP")
    /// </summary>
    public string AppointmentType { get; set; } = string.Empty;
}

/// <summary>
/// Interface for appointment data - kept for backward compatibility
/// Classes should inherit from AppointmentDataBase instead
/// </summary>
[Obsolete("Use AppointmentDataBase abstract class instead to eliminate property duplication")]
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