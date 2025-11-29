namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for general Gemini AI text generation tasks
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Translate disease name from English to Vietnamese with medical context
    /// </summary>
    /// <param name="englishDiseaseName">Disease name in English</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Disease name in Vietnamese</returns>
    Task<string> TranslateDiseaseNameAsync(
        string englishDiseaseName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate detailed medical conclusion for dermatology diagnosis (500-800 words)
    /// Includes: description, causes, symptoms, treatment, prognosis
    /// </summary>
    /// <param name="diseaseName">Disease name in Vietnamese</param>
    /// <param name="confidence">Confidence score (0-1)</param>
    /// <param name="severity">Severity level in Vietnamese</param>
    /// <param name="riskCategory">Risk category in Vietnamese</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed medical conclusion in Vietnamese</returns>
    Task<string> GenerateDermatologyConclusionAsync(
        string diseaseName,
        double confidence,
        string severity,
        string riskCategory,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate general advice for a specific skin condition (3-5 bullet points)
    /// </summary>
    /// <param name="diseaseName">Disease name in Vietnamese</param>
    /// <param name="severity">Severity level in Vietnamese</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>General advice as bullet points</returns>
    Task<string> GenerateGeneralAdviceAsync(
        string diseaseName,
        string severity,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate text using Gemini AI with custom prompt
    /// </summary>
    /// <param name="prompt">Prompt for text generation</param>
    /// <param name="temperature">Temperature for response generation (0.0 - 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text</returns>
    Task<string> GenerateTextAsync(
        string prompt,
        double temperature = 0.3,
        CancellationToken cancellationToken = default
    );
}
