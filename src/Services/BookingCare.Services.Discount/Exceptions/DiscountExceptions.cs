using System.Net;

namespace BookingCare.Services.Discount.Exceptions;

public class DiscountException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public DiscountException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
        : base(message)
    {
        StatusCode = statusCode;
    }

    public DiscountException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

public class DiscountNotFoundException : DiscountException
{
    public DiscountNotFoundException(string message) 
        : base(message, HttpStatusCode.NotFound)
    {
    }
}

public class DiscountValidationException : DiscountException
{
    public DiscountValidationException(string message) 
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}

public class DiscountBusinessException : DiscountException
{
    public DiscountBusinessException(string message) 
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}

public class DiscountConflictException : DiscountException
{
    public DiscountConflictException(string message) 
        : base(message, HttpStatusCode.Conflict)
    {
    }
}
