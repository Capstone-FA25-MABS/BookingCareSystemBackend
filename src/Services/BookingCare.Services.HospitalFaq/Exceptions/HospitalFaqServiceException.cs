namespace BookingCare.Services.HospitalFaq.Exceptions;

/// <summary>
/// Base exception for Hospital FAQ service
/// </summary>
public class HospitalFaqServiceException : Exception
{
    public HospitalFaqServiceException(string message) : base(message)
    {
    }

    public HospitalFaqServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when FAQ is not found
/// </summary>
public class HospitalFaqNotFoundException : HospitalFaqServiceException
{
    public HospitalFaqNotFoundException(Guid id) : base($"FAQ with ID {id} not found")
    {
    }
}

