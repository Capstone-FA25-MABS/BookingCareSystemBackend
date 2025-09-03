using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Discount.Exceptions;

public class DiscountException : BookingCareException
{
    public DiscountException(
        string message, 
        string errorCode = "DISCOUNT_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null) 
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

public class DiscountNotFoundException : NotFoundException
{
    public DiscountNotFoundException(string message) 
        : base(message, "DISCOUNT_NOT_FOUND")
    {
    }

    public DiscountNotFoundException(Guid discountId)
        : base("Discount", discountId.ToString(), "DISCOUNT_NOT_FOUND")
    {
    }

    public DiscountNotFoundException(string code, bool byCode)
        : base($"Discount with code '{code}' was not found.", "DISCOUNT_NOT_FOUND")
    {
        Details["Code"] = code;
    }
}

public class DiscountValidationException : ValidationException
{
    public DiscountValidationException(string message) 
        : base(message, null, "DISCOUNT_VALIDATION_ERROR")
    {
    }

    public DiscountValidationException(List<ValidationError> validationErrors)
        : base("Discount validation failed", validationErrors, "DISCOUNT_VALIDATION_ERROR")
    {
    }
}

public class DiscountBusinessException : BusinessException
{
    public DiscountBusinessException(string message) 
        : base(message, "DISCOUNT_BUSINESS_ERROR")
    {
    }
}

public class DiscountConflictException : ConflictException
{
    public DiscountConflictException(string message) 
        : base(message, "DISCOUNT_CONFLICT")
    {
    }
}
