using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class ServiceTypeException : BookingCareException
{
    public ServiceTypeException(
        string message,
        string errorCode = "SERVICE_TYPE_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// ServiceType Exceptions
public class ServiceTypeNotFoundException : NotFoundException
{
    public ServiceTypeNotFoundException(string message)
        : base(message, "SERVICE_TYPE_NOT_FOUND")
    {
    }

    public static ServiceTypeNotFoundException WithId(Guid serviceTypeId)
    {
        var exception = new ServiceTypeNotFoundException($"Không tìm thấy loại dịch vụ với ID '{serviceTypeId}'.");
        exception.Details["ServiceTypeId"] = serviceTypeId;
        return exception;
    }
}

public class ServiceTypeValidationException : ValidationException
{
    public ServiceTypeValidationException(string message)
        : base(message, null, "SERVICE_TYPE_VALIDATION_ERROR")
    {
    }

    public ServiceTypeValidationException(List<ValidationError> validationErrors)
        : base("Xác thực loại dịch vụ thất bại", validationErrors, "SERVICE_TYPE_VALIDATION_ERROR")
    {
    }
}

public class ServiceTypeConflictException : ConflictException
{
    public ServiceTypeConflictException(string message)
        : base(message, "SERVICE_TYPE_CONFLICT")
    {
    }

    public static ServiceTypeConflictException WithName(string name)
    {
        var exception = new ServiceTypeConflictException($"Loại dịch vụ với tên '{name}' đã tồn tại.");
        exception.Details["Name"] = name;
        return exception;
    }
}
