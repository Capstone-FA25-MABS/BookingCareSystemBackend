using BookingCare.Services.AI.Models.DTOs.Requests;

namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service interface for Google Gemini API integration
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Analyze symptoms using Gemini AI
    /// </summary>
    /// <param name="message">User's symptom description</param>
    /// <param name="conversationHistory">Previous conversation history for context</param>
    /// <returns>JSON response from Gemini</returns>
    Task<string> AnalyzeSymptomsAsync(string message, List<ConversationMessage>? conversationHistory = null);

    /// <summary>
    /// Parse Gemini response to structured format
    /// </summary>
    /// <param name="geminiResponse">Raw JSON response from Gemini</param>
    /// <returns>Parsed analysis result</returns>
    GeminiAnalysisResult ParseGeminiResponse(string geminiResponse);
}

/// <summary>
/// Structured result from Gemini analysis
/// </summary>
public class GeminiAnalysisResult
{
    public List<GeminiDisease> PossibleDiseases { get; set; } = new();
    public List<GeminiQuestion> NextQuestions { get; set; } = new();
    public List<GeminiSpecialty> RecommendedSpecialties { get; set; } = new();
    public List<string> GeneralAdvice { get; set; } = new();
    public bool AnalysisComplete { get; set; }
}

public class GeminiDisease
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GeminiQuestion
{
    public string Question { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
}

public class GeminiSpecialty
{
    public string SpecialtyName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Urgency { get; set; } = "NORMAL";
    public List<string> Reasons { get; set; } = new();
}


