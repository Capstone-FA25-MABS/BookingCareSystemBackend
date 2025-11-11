namespace BookingCare.Services.AI.Exceptions;

/// <summary>
/// Exception thrown when failed to parse Gemini API response
/// </summary>
public class GeminiResponseParseException : Exception
{
    public string? ResponsePreview { get; }

    public GeminiResponseParseException(string message) : base(message)
    {
    }

    public GeminiResponseParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GeminiResponseParseException(string message, string? responsePreview, Exception innerException)
        : base(message, innerException)
    {
        ResponsePreview = responsePreview;
    }
}
