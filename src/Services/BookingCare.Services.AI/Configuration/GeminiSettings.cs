namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Configuration settings for Google Gemini API
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    // Model options: "gemini-1.5-flash-latest" (fastest, recommended), "gemini-1.5-flash", "gemini-1.5-pro"
    public string Model { get; set; } = "gemini-1.5-flash-latest";
    // Use v1 endpoint for stable models, v1beta for newer models
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1/models";
    public double Temperature { get; set; } = 0.5;
    public int MaxTokens { get; set; } = 4096;
    public int TopK { get; set; } = 40;
    public double TopP { get; set; } = 0.95;
}


