namespace BookingCare.Services.Hospital.Exceptions;

public class HospitalNotFoundException : Exception
{
    public HospitalNotFoundException(Guid id)
        : base($"Hospital with ID {id} was not found.")
    {
    }

    public HospitalNotFoundException(string email)
        : base($"Hospital with email {email} was not found.")
    {
    }
}

public class HospitalAlreadyExistsException : Exception
{
    public HospitalAlreadyExistsException(string email)
        : base($"Hospital with email {email} already exists.")
    {
    }
}

public class InvalidHospitalDataException : Exception
{
    public InvalidHospitalDataException(string message)
        : base($"Invalid hospital data: {message}")
    {
    }
}

public class HospitalOperationException : Exception
{
    public HospitalOperationException(string message)
        : base(message)
    {
    }

    public HospitalOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
