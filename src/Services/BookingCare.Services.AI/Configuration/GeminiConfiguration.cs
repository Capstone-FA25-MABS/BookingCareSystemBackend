namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration for Gemini AI API (common settings - used by GeminiApiHelper)
/// </summary>
public class GeminiConfiguration
{
    /// <summary>
    /// Gemini API Endpoint (default: https://generativelanguage.googleapis.com)
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>
    /// Temperature for response generation (0.0 - 1.0)
    /// Higher = more creative, Lower = more focused
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Maximum tokens in response
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// TopK parameter for sampling
    /// </summary>
    public int TopK { get; set; } = 40;

    /// <summary>
    /// TopP parameter for sampling
    /// </summary>
    public double TopP { get; set; } = 0.9;
}
