using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Hospital.Exceptions;

/// <summary>
/// Base exception for Hospital Registration service
/// </summary>
public class HospitalRegistrationException : BookingCareException
{
    public HospitalRegistrationException(
        string message,
        string errorCode = "HOSPITAL_REGISTRATION_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

/// <summary>
/// Exception thrown when hospital registration is not found
/// </summary>
public class HospitalRegistrationNotFoundException : NotFoundException
{
    public HospitalRegistrationNotFoundException(Guid registrationId)
        : base("Hospital Registration", registrationId, "HOSPITAL_REGISTRATION_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when hospital registration validation fails
/// </summary>
public class HospitalRegistrationValidationException : ValidationException
{
    public HospitalRegistrationValidationException(string message)
        : base(message, null, "HOSPITAL_REGISTRATION_VALIDATION_ERROR")
    {
    }

    public HospitalRegistrationValidationException(string message, List<ValidationError> validationErrors)
        : base(message, validationErrors, "HOSPITAL_REGISTRATION_VALIDATION_ERROR")
    {
    }
}

/// <summary>
/// Exception thrown when hospital registration status is invalid for the operation
/// </summary>
public class InvalidRegistrationStatusException : HospitalRegistrationException
{
    public InvalidRegistrationStatusException(string message)
        : base(message, "INVALID_REGISTRATION_STATUS", HttpStatusCode.BadRequest)
    {
    }
}

/// <summary>
/// Exception thrown when duplicate registration data is found
/// </summary>
public class DuplicateRegistrationException : HospitalRegistrationException
{
    public DuplicateRegistrationException(string message)
        : base(message, "DUPLICATE_REGISTRATION", HttpStatusCode.Conflict)
    {
    }
}

/// <summary>
/// Exception thrown when file upload fails
/// </summary>
public class FileUploadException : HospitalRegistrationException
{
    public FileUploadException(string message, Exception? innerException = null)
        : base(message, "FILE_UPLOAD_ERROR", HttpStatusCode.BadRequest, innerException)
    {
    }
}

