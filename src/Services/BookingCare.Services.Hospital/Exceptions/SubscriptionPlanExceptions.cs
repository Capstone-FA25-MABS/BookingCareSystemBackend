namespace BookingCare.Services.Hospital.Exceptions;

public class SubscriptionPlanNotFoundException : Exception
{
    public SubscriptionPlanNotFoundException(Guid id)
        : base($"Subscription plan with ID {id} was not found.")
    {
    }

    public SubscriptionPlanNotFoundException(string name)
        : base($"Subscription plan with name {name} was not found.")
    {
    }
}

public class SubscriptionPlanAlreadyExistsException : Exception
{
    public SubscriptionPlanAlreadyExistsException(string name)
        : base($"Subscription plan with name {name} already exists.")
    {
    }
}

public class InvalidSubscriptionPlanDataException : Exception
{
    public InvalidSubscriptionPlanDataException(string message)
        : base($"Invalid subscription plan data: {message}")
    {
    }
}

public class SubscriptionPlanOperationException : Exception
{
    public SubscriptionPlanOperationException(string message)
        : base(message)
    {
    }

    public SubscriptionPlanOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
