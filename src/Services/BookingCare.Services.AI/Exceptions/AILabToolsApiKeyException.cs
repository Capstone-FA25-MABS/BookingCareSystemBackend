namespace BookingCare.Services.AI.Exceptions;

/// <summary>
/// Domain exception for AI Lab Tools API key operations.
/// </summary>
public class AILabToolsApiKeyException : Exception
{
    public AILabToolsApiKeyException(string message)
        : base(message)
    {
    }

    public AILabToolsApiKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

