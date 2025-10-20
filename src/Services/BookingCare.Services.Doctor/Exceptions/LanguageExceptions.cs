using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class LanguageException : BookingCareException
{
    public LanguageException(
        string message,
        string errorCode = "LANGUAGE_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// Language Exceptions
public class LanguageNotFoundException : NotFoundException
{
    public LanguageNotFoundException(string message)
        : base(message, "LANGUAGE_NOT_FOUND")
    {
    }

    public static LanguageNotFoundException WithId(Guid languageId)
    {
        var exception = new LanguageNotFoundException($"Language with ID '{languageId}' was not found.");
        exception.Details["LanguageId"] = languageId;
        return exception;
    }
}

public class LanguageValidationException : ValidationException
{
    public LanguageValidationException(string message)
        : base(message, null, "LANGUAGE_VALIDATION_ERROR")
    {
    }

    public LanguageValidationException(List<ValidationError> validationErrors)
        : base("Language validation failed", validationErrors, "LANGUAGE_VALIDATION_ERROR")
    {
    }
}

public class LanguageConflictException : ConflictException
{
    public LanguageConflictException(string message)
        : base(message, "LANGUAGE_CONFLICT")
    {
    }

    public static LanguageConflictException WithName(string name)
    {
        var exception = new LanguageConflictException($"Language with name '{name}' already exists.");
        exception.Details["Name"] = name;
        return exception;
    }
}
