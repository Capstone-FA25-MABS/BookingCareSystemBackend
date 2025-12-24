using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Appointment.Exceptions;

/// <summary>
/// Base exception for Appointment service
/// </summary>
public class AppointmentException : BookingCareException
{
    public AppointmentException(
        string message,
        string errorCode = "APPOINTMENT_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

/// <summary>
/// Exception thrown when appointment is not found
/// </summary>
public class AppointmentNotFoundException : NotFoundException
{
    public AppointmentNotFoundException(Guid appointmentId)
        : base("Appointment", appointmentId, "APPOINTMENT_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when appointment already exists for the same time slot
/// </summary>
public class AppointmentConflictException : ConflictException
{
    public AppointmentConflictException(Guid patientId, DateTime appointmentDate)
        : base($"Patient already has an appointment on {appointmentDate:yyyy-MM-dd} at the specified time slot.", "APPOINTMENT_CONFLICT")
    {
        Details["PatientId"] = patientId;
        Details["AppointmentDate"] = appointmentDate;
    }

    /// <summary>
    /// Constructor for relative conflict with custom message
    /// </summary>
    public AppointmentConflictException(Guid relativeId, DateTime appointmentDate, string customMessage)
        : base(customMessage, "APPOINTMENT_CONFLICT")
    {
        Details["RelativeId"] = relativeId;
        Details["AppointmentDate"] = appointmentDate;
    }
}

/// <summary>
/// Exception thrown when doctor is not available for appointment
/// </summary>
public class DoctorNotAvailableException : ConflictException
{
    public DoctorNotAvailableException(Guid doctorId, DateTime appointmentDate)
        : base($"Doctor is not available on {appointmentDate:yyyy-MM-dd} at the specified time slot.", "DOCTOR_NOT_AVAILABLE")
    {
        Details["DoctorId"] = doctorId;
        Details["AppointmentDate"] = appointmentDate;
    }
}

/// <summary>
/// Exception thrown when service medical slot is not available for appointment
/// </summary>
public class ServiceMedicalNotAvailableException : ConflictException
{
    public ServiceMedicalNotAvailableException(Guid serviceId, DateTime appointmentDate)
        : base($"Service medical slot is not available on {appointmentDate:yyyy-MM-dd} at the specified time slot.", "SERVICE_MEDICAL_NOT_AVAILABLE")
    {
        Details["ServiceId"] = serviceId;
        Details["AppointmentDate"] = appointmentDate;
    }
}

/// <summary>
/// Exception thrown when trying to update appointment status with invalid transition
/// </summary>
public class InvalidAppointmentStatusTransitionException : ValidationException
{
    public InvalidAppointmentStatusTransitionException(string currentStatus, string newStatus)
        : base($"Cannot change appointment status from '{currentStatus}' to '{newStatus}'.", null, "INVALID_STATUS_TRANSITION")
    {
        Details["CurrentStatus"] = currentStatus;
        Details["NewStatus"] = newStatus;
    }
}

/// <summary>
/// Exception thrown when appointment date is in the past
/// </summary>
public class AppointmentDateInPastException : ValidationException
{
    public AppointmentDateInPastException(DateTime appointmentDate)
        : base($"Cannot create appointment for past date: {appointmentDate:yyyy-MM-dd}.", null, "APPOINTMENT_DATE_IN_PAST")
    {
        Details["AppointmentDate"] = appointmentDate;
    }
}


