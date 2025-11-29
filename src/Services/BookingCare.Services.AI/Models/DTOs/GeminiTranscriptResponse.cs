namespace BookingCare.Services.AI.Models.DTOs;

/// <summary>
/// Response model from Gemini audio transcription API
/// </summary>
public class GeminiTranscriptResponse
{
    /// <summary>
    /// Transcribed text from audio
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double? Confidence { get; set; }

    /// <summary>
    /// Language detected in audio
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Error message if transcription failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Wrapper for Gemini API response
/// </summary>
public class GeminiApiResponse
{
    public List<Candidate>? Candidates { get; set; }
    public PromptFeedback? PromptFeedback { get; set; }
}

public class Candidate
{
    public Content? Content { get; set; }
    public string? FinishReason { get; set; }
    public int Index { get; set; }
    public List<SafetyRating>? SafetyRatings { get; set; }
}

public class Content
{
    public List<Part>? Parts { get; set; }
    public string? Role { get; set; }
}

public class Part
{
    public string? Text { get; set; }
}

public class SafetyRating
{
    public string? Category { get; set; }
    public string? Probability { get; set; }
}

public class PromptFeedback
{
    public List<SafetyRating>? SafetyRatings { get; set; }
}
