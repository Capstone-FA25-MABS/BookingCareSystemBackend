namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration for Gemini AI API
/// </summary>
public class GeminiConfiguration
{
    /// <summary>
    /// Gemini API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gemini API Endpoint (default: https://generativelanguage.googleapis.com)
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>
    /// Model to use (default: gemini-1.5-pro)
    /// </summary>
    public string Model { get; set; } = "gemini-1.5-pro";

    /// <summary>
    /// Temperature for response generation (0.0 - 1.0)
    /// Higher = more creative, Lower = more focused
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Maximum tokens in response
    /// </summary>
    public int MaxTokens { get; set; } = 2048;
}
