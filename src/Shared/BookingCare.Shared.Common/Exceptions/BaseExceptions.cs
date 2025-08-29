using System.Net;

namespace BookingCare.Shared.Common.Exceptions;

/// <summary>
/// Base exception class for all BookingCare domain exceptions
/// </summary>
public abstract class BookingCareException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }
    public Dictionary<string, object> Details { get; }

    protected BookingCareException(
        string message,
        string errorCode,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// Exception for business logic violations
/// </summary>
public class BusinessException : BookingCareException
{
    public BusinessException(
        string message,
        string errorCode = "BUSINESS_ERROR",
        Dictionary<string, object>? details = null)
        : base(message, errorCode, HttpStatusCode.BadRequest, null, details)
    {
    }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : BookingCareException
{
    public List<ValidationError> ValidationErrors { get; }

    public ValidationException(
        string message,
        List<ValidationError>? validationErrors = null,
        string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode, HttpStatusCode.BadRequest)
    {
        ValidationErrors = validationErrors ?? new List<ValidationError>();
        
        if (ValidationErrors.Any())
        {
            Details["ValidationErrors"] = ValidationErrors;
        }
    }

    public ValidationException(ValidationError validationError)
        : this("Validation failed", new List<ValidationError> { validationError })
    {
    }
}

/// <summary>
/// Exception for resource not found scenarios
/// </summary>
public class NotFoundException : BookingCareException
{
    public NotFoundException(
        string resource,
        object identifier,
        string errorCode = "RESOURCE_NOT_FOUND")
        : base($"{resource} with identifier '{identifier}' was not found.", errorCode, HttpStatusCode.NotFound)
    {
        Details["Resource"] = resource;
        Details["Identifier"] = identifier;
    }

    public NotFoundException(string message, string errorCode = "RESOURCE_NOT_FOUND")
        : base(message, errorCode, HttpStatusCode.NotFound)
    {
    }
}

/// <summary>
/// Exception for unauthorized access
/// </summary>
public class UnauthorizedException : BookingCareException
{
    public UnauthorizedException(
        string message = "Unauthorized access",
        string errorCode = "UNAUTHORIZED")
        : base(message, errorCode, HttpStatusCode.Unauthorized)
    {
    }
}

/// <summary>
/// Exception for forbidden access
/// </summary>
public class ForbiddenException : BookingCareException
{
    public ForbiddenException(
        string message = "Access forbidden",
        string errorCode = "FORBIDDEN")
        : base(message, errorCode, HttpStatusCode.Forbidden)
    {
    }
}

/// <summary>
/// Exception for resource conflicts
/// </summary>
public class ConflictException : BookingCareException
{
    public ConflictException(
        string message,
        string errorCode = "CONFLICT")
        : base(message, errorCode, HttpStatusCode.Conflict)
    {
    }
}

/// <summary>
/// Exception for external service failures
/// </summary>
public class ExternalServiceException : BookingCareException
{
    public string ServiceName { get; }

    public ExternalServiceException(
        string serviceName,
        string message,
        string errorCode = "EXTERNAL_SERVICE_ERROR",
        Exception? innerException = null)
        : base($"External service '{serviceName}' error: {message}", errorCode, HttpStatusCode.BadGateway, innerException)
    {
        ServiceName = serviceName;
        Details["ServiceName"] = serviceName;
    }
}

/// <summary>
/// Represents a single validation error
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }

    public ValidationError() { }

    public ValidationError(string field, string message, object? attemptedValue = null)
    {
        Field = field;
        Message = message;
        AttemptedValue = attemptedValue;
    }
}
