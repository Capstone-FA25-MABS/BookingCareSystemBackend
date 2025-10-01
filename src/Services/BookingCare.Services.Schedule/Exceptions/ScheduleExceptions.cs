using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Schedule.Exceptions;

/// <summary>
/// Base exception for Schedule service
/// </summary>
public class ScheduleException : BookingCareException
{
    public ScheduleException(
        string message,
        string errorCode = "SCHEDULE_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

#region Doctor Schedule Exceptions

/// <summary>
/// Exception thrown when doctor is not found in the context of schedule operations
/// </summary>
public class DoctorNotFoundException : NotFoundException
{
    public DoctorNotFoundException(string message)
        : base(message, "SCHEDULE_DOCTOR_NOT_FOUND")
    {
    }

    public static DoctorNotFoundException WithId(Guid doctorId)
    {
        var exception = new DoctorNotFoundException($"Doctor with identifier '{doctorId}' was not found.");
        exception.Details["DoctorId"] = doctorId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when doctor is inactive or not available for scheduling
/// </summary>
public class DoctorNotAvailableException : BusinessException
{
    public DoctorNotAvailableException(string message)
        : base(message, "SCHEDULE_DOCTOR_NOT_AVAILABLE")
    {
    }

    public static DoctorNotAvailableException WithId(Guid doctorId)
    {
        var exception = new DoctorNotAvailableException($"Doctor with identifier '{doctorId}' is not available for scheduling.");
        exception.Details["DoctorId"] = doctorId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when doctor schedule already exists for a given date
/// </summary>
public class DoctorScheduleConflictException : ConflictException
{
    public DoctorScheduleConflictException(string message)
        : base(message, "SCHEDULE_DOCTOR_CONFLICT")
    {
    }

    public static DoctorScheduleConflictException WithDoctorAndDate(Guid doctorId, DateOnly date)
    {
        var exception = new DoctorScheduleConflictException($"Doctor schedule already exists for doctor '{doctorId}' on date '{date:yyyy-MM-dd}'.");
        exception.Details["DoctorId"] = doctorId;
        exception.Details["ScheduleDate"] = date;
        return exception;
    }
}

/// <summary>
/// Exception thrown when doctor daily schedule is not found
/// </summary>
public class DoctorDailyScheduleNotFoundException : NotFoundException
{
    public DoctorDailyScheduleNotFoundException(string message)
        : base(message, "SCHEDULE_DOCTOR_DAILY_NOT_FOUND")
    {
    }

    public static DoctorDailyScheduleNotFoundException WithDoctorAndDate(Guid doctorId, DateOnly date)
    {
        var exception = new DoctorDailyScheduleNotFoundException($"Doctor daily schedule not found for doctor '{doctorId}' on date '{date:yyyy-MM-dd}'.");
        exception.Details["DoctorId"] = doctorId;
        exception.Details["ScheduleDate"] = date;
        return exception;
    }
}

#endregion

#region Schedule Exception Management

/// <summary>
/// Exception thrown when schedule exception is not found
/// </summary>
public class ScheduleExceptionNotFoundException : NotFoundException
{
    public ScheduleExceptionNotFoundException(string message)
        : base(message, "SCHEDULE_EXCEPTION_NOT_FOUND")
    {
    }

    public static ScheduleExceptionNotFoundException WithId(Guid exceptionId)
    {
        var exception = new ScheduleExceptionNotFoundException($"Schedule exception with ID '{exceptionId}' was not found.");
        exception.Details["ExceptionId"] = exceptionId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when schedule exception conflicts with existing ones
/// </summary>
public class ScheduleExceptionConflictException : ConflictException
{
    public ScheduleExceptionConflictException(string message)
        : base(message, "SCHEDULE_EXCEPTION_CONFLICT")
    {
    }

    public static ScheduleExceptionConflictException WithDoctorDateAndTime(Guid doctorId, DateOnly date, string appointmentTime)
    {
        var exception = new ScheduleExceptionConflictException($"Schedule exception already exists for doctor '{doctorId}' on date '{date:yyyy-MM-dd}' at time '{appointmentTime}'.");
        exception.Details["DoctorId"] = doctorId;
        exception.Details["ExceptionDate"] = date;
        exception.Details["AppointmentTime"] = appointmentTime;
        return exception;
    }
}

#endregion

#region Clinic Schedule Exceptions

/// <summary>
/// Exception thrown when clinic is not found in the context of schedule operations
/// </summary>
public class ClinicNotFoundException : NotFoundException
{
    public ClinicNotFoundException(string message)
        : base(message, "SCHEDULE_CLINIC_NOT_FOUND")
    {
    }

    public static ClinicNotFoundException WithId(Guid clinicId)
    {
        var exception = new ClinicNotFoundException($"Clinic with identifier '{clinicId}' was not found.");
        exception.Details["ClinicId"] = clinicId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when clinic exception is not found
/// </summary>
public class ClinicExceptionNotFoundException : NotFoundException
{
    public ClinicExceptionNotFoundException(string message)
        : base(message, "CLINIC_EXCEPTION_NOT_FOUND")
    {
    }

    public static ClinicExceptionNotFoundException WithId(Guid exceptionId)
    {
        var exception = new ClinicExceptionNotFoundException($"Clinic exception with ID '{exceptionId}' was not found.");
        exception.Details["ExceptionId"] = exceptionId;
        return exception;
    }
}

#endregion

#region Service Schedule Exceptions

/// <summary>
/// Exception thrown when medical service is not found in the context of schedule operations
/// </summary>
public class ServiceNotFoundException : NotFoundException
{
    public ServiceNotFoundException(string message)
        : base(message, "SCHEDULE_SERVICE_NOT_FOUND")
    {
    }

    public static ServiceNotFoundException WithId(Guid serviceId)
    {
        var exception = new ServiceNotFoundException($"Medical service with identifier '{serviceId}' was not found.");
        exception.Details["ServiceId"] = serviceId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when medical service is inactive or not available for scheduling
/// </summary>
public class ServiceNotAvailableException : BusinessException
{
    public ServiceNotAvailableException(string message)
        : base(message, "SCHEDULE_SERVICE_NOT_AVAILABLE")
    {
    }

    public static ServiceNotAvailableException WithId(Guid serviceId)
    {
        var exception = new ServiceNotAvailableException($"Medical service with identifier '{serviceId}' is not available for scheduling.");
        exception.Details["ServiceId"] = serviceId;
        return exception;
    }
}

/// <summary>
/// Exception thrown when service schedule is not found
/// </summary>
public class ServiceScheduleNotFoundException : NotFoundException
{
    public ServiceScheduleNotFoundException(string message)
        : base(message, "SERVICE_SCHEDULE_NOT_FOUND")
    {
    }

    public static ServiceScheduleNotFoundException WithId(Guid scheduleId)
    {
        var exception = new ServiceScheduleNotFoundException($"Service schedule with ID '{scheduleId}' was not found.");
        exception.Details["ScheduleId"] = scheduleId;
        return exception;
    }

    public static ServiceScheduleNotFoundException WithServiceId(Guid serviceId)
    {
        var exception = new ServiceScheduleNotFoundException($"Service schedule not found for service '{serviceId}'.");
        exception.Details["ServiceId"] = serviceId;
        return exception;
    }
}

#endregion

#region Schedule Pattern Exceptions

/// <summary>
/// Exception thrown when schedule pattern is invalid
/// </summary>
public class InvalidSchedulePatternException : ValidationException
{
    public InvalidSchedulePatternException(string message)
        : base(message, null, "INVALID_SCHEDULE_PATTERN")
    {
    }

    public static InvalidSchedulePatternException WithPattern(string pattern, string reason)
    {
        var exception = new InvalidSchedulePatternException($"Invalid schedule pattern '{pattern}': {reason}");
        exception.Details["Pattern"] = pattern;
        exception.Details["Reason"] = reason;
        return exception;
    }
}

/// <summary>
/// Exception thrown when appointment time is invalid
/// </summary>
public class InvalidAppointmentTimeException : ValidationException
{
    public InvalidAppointmentTimeException(string message)
        : base(message, null, "INVALID_APPOINTMENT_TIME")
    {
    }

    public static InvalidAppointmentTimeException WithTime(string appointmentTime, string reason)
    {
        var exception = new InvalidAppointmentTimeException($"Invalid appointment time '{appointmentTime}': {reason}");
        exception.Details["AppointmentTime"] = appointmentTime;
        exception.Details["Reason"] = reason;
        return exception;
    }
}

/// <summary>
/// Exception thrown when schedule date is invalid
/// </summary>
public class InvalidScheduleDateException : ValidationException
{
    public InvalidScheduleDateException(string message)
        : base(message, null, "INVALID_SCHEDULE_DATE")
    {
    }

    public static InvalidScheduleDateException WithDate(DateOnly date, string reason)
    {
        var exception = new InvalidScheduleDateException($"Invalid schedule date '{date:yyyy-MM-dd}': {reason}");
        exception.Details["ScheduleDate"] = date;
        exception.Details["Reason"] = reason;
        return exception;
    }
}

#endregion

#region Schedule Validation Exceptions

/// <summary>
/// Exception thrown when schedule validation fails
/// </summary>
public class ScheduleValidationException : ValidationException
{
    public ScheduleValidationException(string message)
        : base(message, null, "SCHEDULE_VALIDATION_ERROR")
    {
    }

    public ScheduleValidationException(List<ValidationError> validationErrors)
        : base("Schedule validation failed", validationErrors, "SCHEDULE_VALIDATION_ERROR")
    {
    }
}

/// <summary>
/// Exception thrown when schedule business rules are violated
/// </summary>
public class ScheduleBusinessException : BusinessException
{
    public ScheduleBusinessException(string message)
        : base(message, "SCHEDULE_BUSINESS_ERROR")
    {
    }
}

#endregion