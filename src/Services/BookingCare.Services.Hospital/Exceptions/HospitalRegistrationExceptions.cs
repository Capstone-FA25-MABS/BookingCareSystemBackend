namespace BookingCare.Services.Hospital.Exceptions;

public class HospitalRegistrationException : Exception
{
    public HospitalRegistrationException(string message) : base(message)
    {
    }

    public HospitalRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class HospitalRegistrationNotFoundException : HospitalRegistrationException
{
    public HospitalRegistrationNotFoundException(Guid registrationId)
        : base($"Không tìm thấy đơn đăng ký với ID: {registrationId}")
    {
    }
}

