namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// Request model for Gemini audio transcription
/// </summary>
public class GeminiTranscriptRequest
{
    /// <summary>
    /// Base64 encoded audio data
    /// </summary>
    public required string Audio { get; set; }

    /// <summary>
    /// Audio MIME type (audio/webm or audio/wav)
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Optional language code (e.g., "en", "vi")
    /// </summary>
    public string? Language { get; set; }
}
