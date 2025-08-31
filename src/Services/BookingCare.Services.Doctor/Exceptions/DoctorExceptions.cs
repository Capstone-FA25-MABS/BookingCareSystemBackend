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

    public DoctorNotFoundException(Guid doctorId)
        : base("Doctor", doctorId, "DOCTOR_NOT_FOUND")
    {
    }

    public DoctorNotFoundException(string email, bool byEmail)
        : base($"Doctor with email '{email}' was not found.", "DOCTOR_NOT_FOUND")
    {
        Details["Email"] = email;
    }

    public DoctorNotFoundException(Guid accountId, bool byAccountId)
        : base($"Doctor with account ID '{accountId}' was not found.", "DOCTOR_NOT_FOUND")
    {
        Details["AccountId"] = accountId;
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

    public DoctorConflictException(string email)
        : base($"Doctor with email '{email}' already exists.", "DOCTOR_CONFLICT")
    {
        Details["Email"] = email;
    }

    public DoctorConflictException(Guid accountId)
        : base($"Doctor with account ID '{accountId}' already exists.", "DOCTOR_CONFLICT")
    {
        Details["AccountId"] = accountId;
    }
}

// Position Exceptions
public class PositionNotFoundException : NotFoundException
{
    public PositionNotFoundException(string message) 
        : base(message, "POSITION_NOT_FOUND")
    {
    }

    public PositionNotFoundException(Guid positionId)
        : base("Position", positionId, "POSITION_NOT_FOUND")
    {
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

    public PositionConflictException(string name)
        : base($"Position with name '{name}' already exists.", "POSITION_CONFLICT")
    {
        Details["Name"] = name;
    }
}

// Price Exceptions
public class PriceNotFoundException : NotFoundException
{
    public PriceNotFoundException(string message) 
        : base(message, "PRICE_NOT_FOUND")
    {
    }

    public PriceNotFoundException(Guid priceId)
        : base("Price", priceId, "PRICE_NOT_FOUND")
    {
    }
}

public class PriceValidationException : ValidationException
{
    public PriceValidationException(string message) 
        : base(message, null, "PRICE_VALIDATION_ERROR")
    {
    }

    public PriceValidationException(List<ValidationError> validationErrors)
        : base("Price validation failed", validationErrors, "PRICE_VALIDATION_ERROR")
    {
    }
}

// DoctorPrice Exceptions
public class DoctorPriceNotFoundException : NotFoundException
{
    public DoctorPriceNotFoundException(string message) 
        : base(message, "DOCTOR_PRICE_NOT_FOUND")
    {
    }

    public DoctorPriceNotFoundException(Guid doctorId, Guid priceId)
        : base($"Doctor price relationship not found for doctor ID '{doctorId}' and price ID '{priceId}'.", "DOCTOR_PRICE_NOT_FOUND")
    {
        Details["DoctorId"] = doctorId;
        Details["PriceId"] = priceId;
    }
}

public class DoctorPriceConflictException : ConflictException
{
    public DoctorPriceConflictException(string message) 
        : base(message, "DOCTOR_PRICE_CONFLICT")
    {
    }

    public DoctorPriceConflictException(Guid doctorId, Guid priceId)
        : base($"Doctor price relationship already exists for doctor ID '{doctorId}' and price ID '{priceId}'.", "DOCTOR_PRICE_CONFLICT")
    {
        Details["DoctorId"] = doctorId;
        Details["PriceId"] = priceId;
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
