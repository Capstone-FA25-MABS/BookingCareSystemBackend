namespace BookingCare.Services.Appointment.Enums;

/// <summary>
/// Represents the status of an appointment.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// The appointment is pending and awaiting confirmation.
    /// </summary>
    PENDING,

    /// <summary>
    /// The appointment has been confirmed.
    /// </summary>
    CONFIRMED,

    /// <summary>
    /// The appointment has been cancelled.
    /// </summary>
    CANCELLED,

    /// <summary>
    /// The appointment has been completed.
    /// </summary>
    COMPLETED
}