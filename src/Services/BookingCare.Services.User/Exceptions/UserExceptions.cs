using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.User.Exceptions;

public class UserException : BookingCareException
{
    public UserException(
        string message,
        string errorCode = "USER_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(Guid userId)
        : base("User", userId.ToString(), "USER_NOT_FOUND")
    {
    }
}

public class EmailAlreadyExistsException : ConflictException
{
    public EmailAlreadyExistsException(string email)
        : base($"Email {email} already exists.", "EMAIL_ALREADY_EXISTS")
    {
    }
}

public class PhoneAlreadyExistsException : ConflictException
{
    public PhoneAlreadyExistsException(string phone)
        : base($"Phone {phone} already exists.", "PHONE_ALREADY_EXISTS")
    {
    }
}






