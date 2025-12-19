namespace BookingCare.Services.Content.Exceptions;

public class BlogServiceException : Exception
{
    public BlogServiceException()
    {
    }

    public BlogServiceException(string message) : base(message)
    {
    }

    public BlogServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}


