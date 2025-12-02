namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Enum representing different types of appointments
/// Used across multiple services (Appointment, Schedule, etc.)
/// </summary>
public enum AppointmentType
{
    /// <summary>
    /// Regular in-person consultation at clinic/hospital
    /// </summary>
    IN_PERSON = 0,

    /// <summary>
    /// Online consultation via video call
    /// </summary>
    TELEHEALTH = 1
}
