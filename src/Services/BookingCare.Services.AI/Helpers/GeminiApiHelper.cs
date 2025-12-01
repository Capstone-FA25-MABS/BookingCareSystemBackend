using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class tối ưu cho việc gọi Gemini API
/// Optimized helper for Gemini API calls with model fallback
/// </summary>
public class GeminiApiHelper
{
    private readonly ILogger<GeminiApiHelper> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _commonConfig;
    private const string SafetyThreshold = "BLOCK_MEDIUM_AND_ABOVE";

    public GeminiApiHelper(
        ILogger<GeminiApiHelper> logger,
        HttpClient httpClient,
        IOptions<GeminiConfiguration> commonConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _commonConfig = commonConfig.Value;

        // Configure HttpClient timeout - increased for longer Gemini processing
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    /// <summary>
    /// Gọi Gemini API với model fallback tự động
    /// Call Gemini API with automatic model fallback (primary -> fallback)
    /// </summary>
    /// <param name="prompt">Prompt để gửi đến Gemini</param>
    /// <param name="serviceConfig">Cấu hình service (API key, models)</param>
    /// <param name="temperature">Temperature (optional, mặc định dùng từ common config)</param>
    /// <param name="maxOutputTokens">Max output tokens (optional, mặc định dùng từ common config)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text từ Gemini</returns>
    public async Task<string> CallGeminiApiAsync(
        string prompt,
        ServiceGeminiConfiguration serviceConfig,
        double? temperature = null,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceConfig.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured for this service");
        }

        var actualTemperature = temperature ?? serviceConfig.Temperature ?? _commonConfig.Temperature;
        var actualMaxTokens = maxOutputTokens ?? serviceConfig.MaxTokens ?? _commonConfig.MaxTokens;

        // Thử primary model trước, sau đó fallback
        // Try primary model first, then fallback
        var modelsToTry = new[]
        {
            serviceConfig.PrimaryModel,
            serviceConfig.FallbackModel
        };

        Exception? lastException = null;

        // Sử dụng v1beta để hỗ trợ models mới nhất
        // Use v1beta for latest model support
        var apiVersion = "v1beta";

        foreach (var model in modelsToTry)
        {
            try
            {
                var url = $"{_commonConfig.ApiEndpoint}/{apiVersion}/models/{model}:generateContent?key={serviceConfig.ApiKey}";
                var result = await CallGeminiApiWithUrlAsync(
                    url,
                    prompt,
                    model,
                    apiVersion,
                    actualTemperature,
                    actualMaxTokens,
                    cancellationToken);

                return result;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("Model not found"))
            {
                _logger.LogDebug(ex, "Model {Model} not found on {ApiVersion}, trying next model", model, apiVersion);
                lastException = ex;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("quota"))
            {
                _logger.LogWarning(ex, "Gemini API quota exceeded for model {Model}", model);
                throw new InvalidOperationException(
                    $"Gemini API quota exceeded for model {model}. Please check your billing plan or wait for quota reset.",
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "Timeout calling Gemini API with model {Model} (120s), trying next model", model);
                lastException = ex;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request canceled for model {Model}, trying next model", model);
                lastException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed with model {Model}, trying next", model);
                lastException = ex;
            }
        }

        throw new InvalidOperationException(
            $"Failed to call Gemini API with all models (tried: {string.Join(", ", modelsToTry)}). Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Gọi Gemini API với URL cụ thể
    /// </summary>
    private async Task<string> CallGeminiApiWithUrlAsync(
        string url,
        string prompt,
        string model,
        string apiVersion,
        double temperature,
        int maxOutputTokens,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = temperature,
                maxOutputTokens = maxOutputTokens,
                topP = _commonConfig.TopP,
                topK = _commonConfig.TopK
            },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = SafetyThreshold },
            },
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Gemini API: {ApiVersion}/models/{Model}", apiVersion, model);

        var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Quota exceeded
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Gemini API quota exceeded. Response: {Response}", responseContent);
                throw new HttpRequestException(
                    "Gemini API quota exceeded. Please check your billing plan or wait for quota reset.");
            }

            // Model not found
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Model {Model} not found on {ApiVersion}", model, apiVersion);
                throw new HttpRequestException($"Model not found: {model}");
            }

            _logger.LogWarning("Gemini API failed: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);
            throw new HttpRequestException($"Gemini API returned error: {response.StatusCode}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
        {
            _logger.LogWarning("Gemini API returned no candidates. Response: {Response}", responseContent);

            // Check for prompt feedback (safety issues)
            if (geminiResponse?.PromptFeedback != null)
            {
                var blockReason = geminiResponse.PromptFeedback.BlockReason;
                var safetyRatings = geminiResponse.PromptFeedback.SafetyRatings;
                _logger.LogWarning("Prompt was blocked. BlockReason: {BlockReason}, SafetyRatings: {SafetyRatings}",
                    blockReason, safetyRatings != null ? string.Join(", ", safetyRatings.Select(r => $"{r.Category}:{r.Probability}")) : "none");

                throw new InvalidOperationException(
                    $"Gemini API blocked the prompt. Reason: {blockReason}. " +
                    $"This may be due to safety filters. Please review your prompt content.");
            }

            throw new InvalidOperationException("Gemini API returned no candidates");
        }

        var candidate = geminiResponse.Candidates[0];

        // Check finish reason (SAFETY, MAX_TOKENS, STOP, etc.)
        if (!string.IsNullOrEmpty(candidate.FinishReason))
        {
            if (candidate.FinishReason == "SAFETY")
            {
                var safetyRatings = candidate.SafetyRatings;
                _logger.LogWarning("Gemini API blocked response due to safety. SafetyRatings: {SafetyRatings}",
                    safetyRatings != null ? string.Join(", ", safetyRatings.Select(r => $"{r.Category}:{r.Probability}")) : "none");
                throw new InvalidOperationException(
                    "Gemini API blocked the response due to safety filters. " +
                    "The generated content may have been flagged as inappropriate. " +
                    "Please try rephrasing your prompt.");
            }

            if (candidate.FinishReason != "STOP" && candidate.FinishReason != "MAX_TOKENS")
            {
                _logger.LogWarning("Gemini API finished with reason: {FinishReason}", candidate.FinishReason);
            }
        }

        var generatedText = candidate.Content?.Parts?[0]?.Text;

        if (string.IsNullOrEmpty(generatedText))
        {
            var finishReason = candidate.FinishReason ?? "unknown";
            var safetyRatings = candidate.SafetyRatings;
            var logMessage = $"Gemini API returned empty text. FinishReason: {finishReason}";

            if (safetyRatings != null && safetyRatings.Length > 0)
            {
                logMessage += $", SafetyRatings: {string.Join(", ", safetyRatings.Select(r => $"{r.Category}:{r.Probability}"))}";
            }

            _logger.LogWarning("{LogMessage}. Full response: {Response}", logMessage, responseContent);

            if (finishReason == "SAFETY")
            {
                throw new InvalidOperationException(
                    "Gemini API returned empty text due to safety filters. " +
                    "The content may have been blocked. Please try rephrasing your prompt.");
            }

            throw new InvalidOperationException(
                $"Gemini API returned empty text. FinishReason: {finishReason}. " +
                "This may indicate an issue with the prompt or API response format.");
        }

        _logger.LogInformation("Successfully called Gemini using {ApiVersion}/{Model}, generated {Length} characters",
            apiVersion, model, generatedText.Length);

        return generatedText;
    }

    #region Response Models

    private class GeminiApiResponse
    {
        public Candidate[]? Candidates { get; set; }
        public PromptFeedback? PromptFeedback { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
        public string? FinishReason { get; set; }
        public SafetyRating[]? SafetyRatings { get; set; }
    }

    private class Content
    {
        public Part[]? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }

    private class PromptFeedback
    {
        public string? BlockReason { get; set; }
        public SafetyRating[]? SafetyRatings { get; set; }
    }

    private class SafetyRating
    {
        public string? Category { get; set; }
        public string? Probability { get; set; }
    }

    #endregion

    /// <summary>
    /// Helper method to call Gemini API with default parameters (temperature and maxOutputTokens from common config)
    /// </summary>
    public async Task<string> CallGeminiApiWithDefaultsAsync(
        string prompt,
        ServiceGeminiConfiguration serviceConfig,
        CancellationToken cancellationToken = default)
    {
        return await CallGeminiApiAsync(
            prompt,
            serviceConfig,
            temperature: null, // Use default from common config
            maxOutputTokens: null, // Use default from common config
            cancellationToken: cancellationToken);
    }
}
