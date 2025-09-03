using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class PositionException : BookingCareException
{
    public PositionException(
        string message, 
        string errorCode = "POSITION_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null) 
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// Position Exceptions
public class PositionNotFoundException : NotFoundException
{
    public PositionNotFoundException(string message) 
        : base(message, "POSITION_NOT_FOUND")
    {
    }

    public static PositionNotFoundException WithId(Guid positionId)
    {
        var exception = new PositionNotFoundException($"Position with ID '{positionId}' was not found.");
        exception.Details["PositionId"] = positionId;
        return exception;
    }
}

public class PositionValidationException : ValidationException
{
    public PositionValidationException(string message) 
        : base(message, null, "POSITION_VALIDATION_ERROR")
    {
    }

    public PositionValidationException(List<ValidationError> validationErrors)
        : base("Position validation failed", validationErrors, "POSITION_VALIDATION_ERROR")
    {
    }
}

public class PositionConflictException : ConflictException
{
    public PositionConflictException(string message) 
        : base(message, "POSITION_CONFLICT")
    {
    }

    public static PositionConflictException WithName(string name)
    {
        var exception = new PositionConflictException($"Position with name '{name}' already exists.");
        exception.Details["Name"] = name;
        return exception;
    }
}
