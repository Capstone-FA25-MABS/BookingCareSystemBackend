namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration for Groq AI API (common settings - used by GroqApiHelper)
/// </summary>
public class GroqConfiguration
{
    /// <summary>
    /// Groq API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Groq API Endpoint (default: https://api.groq.com)
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://api.groq.com";

    /// <summary>
    /// Model for asking mode (default: qwen/qwen3-32b)
    /// </summary>
    public string AskingModel { get; set; } = "qwen/qwen3-32b";

    /// <summary>
    /// Model for conclusion mode (default: qwen/qwen3-32b)
    /// </summary>
    public string ConclusionModel { get; set; } = "qwen/qwen3-32b";

    /// <summary>
    /// Temperature for response generation (0.0 - 2.0)
    /// Higher = more creative, Lower = more focused
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Maximum tokens in response
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Maximum retries for API calls
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether Groq is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}


