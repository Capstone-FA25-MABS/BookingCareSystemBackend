namespace BookingCare.Services.AI.Exceptions;

/// <summary>
/// Exception thrown when Gemini API returns an error
/// </summary>
public class GeminiApiException : Exception
{
    public string? ErrorCode { get; }
    public int? StatusCode { get; }

    public GeminiApiException(string message) : base(message)
    {
    }

    public GeminiApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GeminiApiException(string message, string? errorCode, int? statusCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public GeminiApiException(string message, string? errorCode, int? statusCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
