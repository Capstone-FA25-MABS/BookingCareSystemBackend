using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class SpecialtyException : BookingCareException
{
    public SpecialtyException(
        string message,
        string errorCode = "SPECIALTY_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// Specialty Exceptions
public class SpecialtyNotFoundException : NotFoundException
{
    public SpecialtyNotFoundException(string message)
        : base(message, "SPECIALTY_NOT_FOUND")
    {
    }

    public static SpecialtyNotFoundException WithId(Guid specialtyId)
    {
        var exception = new SpecialtyNotFoundException($"Specialty with ID '{specialtyId}' was not found.");
        exception.Details["SpecialtyId"] = specialtyId;
        return exception;
    }
}

public class SpecialtyValidationException : ValidationException
{
    public SpecialtyValidationException(string message)
        : base(message, null, "SPECIALTY_VALIDATION_ERROR")
    {
    }

    public SpecialtyValidationException(List<ValidationError> validationErrors)
        : base("Specialty validation failed", validationErrors, "SPECIALTY_VALIDATION_ERROR")
    {
    }
}

public class SpecialtyConflictException : ConflictException
{
    public SpecialtyConflictException(string message)
        : base(message, "SPECIALTY_CONFLICT")
    {
    }

    public static SpecialtyConflictException WithName(string name)
    {
        var exception = new SpecialtyConflictException($"Specialty with name '{name}' already exists.");
        exception.Details["Name"] = name;
        return exception;
    }
}
