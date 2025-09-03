using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Doctor.Exceptions;

public class PriceException : BookingCareException
{
    public PriceException(
        string message, 
        string errorCode = "PRICE_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null) 
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

// Price Exceptions
public class PriceNotFoundException : NotFoundException
{
    public PriceNotFoundException(string message) 
        : base(message, "PRICE_NOT_FOUND")
    {
    }

    public static PriceNotFoundException WithId(Guid priceId)
    {
        var exception = new PriceNotFoundException($"Price with ID '{priceId}' was not found.");
        exception.Details["PriceId"] = priceId;
        return exception;
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
