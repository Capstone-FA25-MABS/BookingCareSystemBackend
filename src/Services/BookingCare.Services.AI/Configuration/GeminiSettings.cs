namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration settings for Google Gemini API
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    // Model options: "gemini-pro" (v1), "gemini-1.5-flash" (v1), "gemini-1.5-pro" (v1)
    public string Model { get; set; } = "gemini-1.5-flash";
    // Use v1 endpoint for stable models, v1beta for newer models
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1/models";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
    public int TopK { get; set; } = 40;
    public double TopP { get; set; } = 0.95;
}


