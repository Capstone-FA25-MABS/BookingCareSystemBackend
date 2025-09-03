using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class DoctorException : BookingCareException
{
    public DoctorException(
        string message, 
        string errorCode = "DOCTOR_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null) 
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// Doctor Exceptions
public class DoctorNotFoundException : NotFoundException
{
    public DoctorNotFoundException(string message) 
        : base(message, "DOCTOR_NOT_FOUND")
    {
    }

    public static DoctorNotFoundException WithId(Guid doctorId)
    {
        var exception = new DoctorNotFoundException($"Doctor with ID '{doctorId}' was not found.");
        exception.Details["DoctorId"] = doctorId;
        return exception;
    }

    public static DoctorNotFoundException WithEmail(string email)
    {
        var exception = new DoctorNotFoundException($"Doctor with email '{email}' was not found.");
        exception.Details["Email"] = email;
        return exception;
    }

    public static DoctorNotFoundException WithAccountId(Guid accountId)
    {
        var exception = new DoctorNotFoundException($"Doctor with account ID '{accountId}' was not found.");
        exception.Details["AccountId"] = accountId;
        return exception;
    }
}

public class DoctorValidationException : ValidationException
{
    public DoctorValidationException(string message) 
        : base(message, null, "DOCTOR_VALIDATION_ERROR")
    {
    }

    public DoctorValidationException(List<ValidationError> validationErrors)
        : base("Doctor validation failed", validationErrors, "DOCTOR_VALIDATION_ERROR")
    {
    }
}

public class DoctorBusinessException : BusinessException
{
    public DoctorBusinessException(string message) 
        : base(message, "DOCTOR_BUSINESS_ERROR")
    {
    }
}

public class DoctorConflictException : ConflictException
{
    public DoctorConflictException(string message) 
        : base(message, "DOCTOR_CONFLICT")
    {
    }

    public static DoctorConflictException WithEmail(string email)
    {
        var exception = new DoctorConflictException($"Doctor with email '{email}' already exists.");
        exception.Details["Email"] = email;
        return exception;
    }

    public static DoctorConflictException WithAccountId(Guid accountId)
    {
        var exception = new DoctorConflictException($"Doctor with account ID '{accountId}' already exists.");
        exception.Details["AccountId"] = accountId;
        return exception;
    }
}

// DoctorPrice Exceptions
public class DoctorPriceNotFoundException : NotFoundException
{
    public DoctorPriceNotFoundException(string message) 
        : base(message, "DOCTOR_PRICE_NOT_FOUND")
    {
    }

    public static DoctorPriceNotFoundException WithIds(Guid doctorId, Guid priceId)
    {
        var exception = new DoctorPriceNotFoundException($"Doctor price relationship not found for doctor ID '{doctorId}' and price ID '{priceId}'.");
        exception.Details["DoctorId"] = doctorId;
        exception.Details["PriceId"] = priceId;
        return exception;
    }
}

public class DoctorPriceConflictException : ConflictException
{
    public DoctorPriceConflictException(string message) 
        : base(message, "DOCTOR_PRICE_CONFLICT")
    {
    }

    public static DoctorPriceConflictException WithIds(Guid doctorId, Guid priceId)
    {
        var exception = new DoctorPriceConflictException($"Doctor price relationship already exists for doctor ID '{doctorId}' and price ID '{priceId}'.");
        exception.Details["DoctorId"] = doctorId;
        exception.Details["PriceId"] = priceId;
        return exception;
    }
}

public class DoctorPriceValidationException : ValidationException
{
    public DoctorPriceValidationException(string message) 
        : base(message, null, "DOCTOR_PRICE_VALIDATION_ERROR")
    {
    }

    public DoctorPriceValidationException(List<ValidationError> validationErrors)
        : base("Doctor price validation failed", validationErrors, "DOCTOR_PRICE_VALIDATION_ERROR")
    {
    }
}
