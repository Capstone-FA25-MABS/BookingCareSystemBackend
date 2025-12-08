using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class tối ưu cho việc gọi Groq API
/// Optimized helper for Groq API calls
/// </summary>
public class GroqApiHelper
{
    private readonly ILogger<GroqApiHelper> _logger;
    private readonly HttpClient _httpClient;
    private readonly GroqConfiguration _config;

    public GroqApiHelper(
        ILogger<GroqApiHelper> logger,
        HttpClient httpClient,
        IOptions<GroqConfiguration> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;

        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <summary>
    /// Gọi Groq API với model cho asking mode
    /// Call Groq API with asking mode model
    /// </summary>
    /// <param name="prompt">Prompt để gửi đến Groq</param>
    /// <param name="temperature">Temperature (optional, mặc định dùng từ config)</param>
    /// <param name="maxTokens">Max tokens (optional, mặc định dùng từ config)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text từ Groq</returns>
    public async Task<string> CallAskingModeAsync(
        string prompt,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        return await CallGroqApiAsync(
            prompt,
            _config.AskingModel,
            temperature,
            maxTokens,
            cancellationToken);
    }

    /// <summary>
    /// Gọi Groq API với model cho conclusion mode
    /// Call Groq API with conclusion mode model
    /// </summary>
    /// <param name="prompt">Prompt để gửi đến Groq</param>
    /// <param name="temperature">Temperature (optional, mặc định dùng từ config)</param>
    /// <param name="maxTokens">Max tokens (optional, mặc định dùng từ config)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text từ Groq</returns>
    public async Task<string> CallConclusionModeAsync(
        string prompt,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        return await CallGroqApiAsync(
            prompt,
            _config.ConclusionModel,
            temperature,
            maxTokens,
            cancellationToken);
    }

    /// <summary>
    /// Gọi Groq API với ServiceGroqConfiguration (hỗ trợ model fallback tự động)
    /// Call Groq API with ServiceGroqConfiguration (supports automatic model fallback)
    /// </summary>
    /// <param name="prompt">Prompt để gửi đến Groq</param>
    /// <param name="serviceConfig">Cấu hình service (API key, models)</param>
    /// <param name="temperature">Temperature (optional, mặc định dùng từ config)</param>
    /// <param name="maxTokens">Max tokens (optional, mặc định dùng từ config)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text từ Groq</returns>
    public async Task<string> CallGroqApiAsync(
        string prompt,
        ServiceGroqConfiguration serviceConfig,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceConfig.ApiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured for this service");
        }

        var actualTemperature = temperature ?? serviceConfig.Temperature ?? _config.Temperature;
        var actualMaxTokens = maxTokens ?? serviceConfig.MaxTokens ?? _config.MaxTokens;
        var actualMaxRetries = serviceConfig.MaxRetries ?? _config.MaxRetries;
        var actualTimeoutSeconds = serviceConfig.TimeoutSeconds ?? _config.TimeoutSeconds;

        // Thử primary model trước, sau đó fallback
        // Try primary model first, then fallback
        var modelsToTry = new[]
        {
            serviceConfig.PrimaryModel,
            serviceConfig.FallbackModel
        };

        Exception? lastException = null;

        foreach (var model in modelsToTry)
        {
            try
            {
                return await CallGroqApiWithModelAsync(
                    prompt,
                    model,
                    serviceConfig.ApiKey,
                    actualTemperature,
                    actualMaxTokens,
                    actualMaxRetries,
                    actualTimeoutSeconds,
                    cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("Model not found"))
            {
                _logger.LogDebug(ex, "Model {Model} not found, trying next model", model);
                lastException = ex;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "Timeout calling Groq API with model {Model}, trying next model", model);
                lastException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed with model {Model}, trying next", model);
                lastException = ex;
            }
        }

        throw new InvalidOperationException(
            $"Failed to call Groq API with all models (tried: {string.Join(", ", modelsToTry)}). Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Helper method to call Groq API with default parameters (temperature and maxTokens from common config)
    /// </summary>
    public async Task<string> CallGroqApiWithDefaultsAsync(
        string prompt,
        ServiceGroqConfiguration serviceConfig,
        CancellationToken cancellationToken = default)
    {
        return await CallGroqApiAsync(
            prompt,
            serviceConfig,
            temperature: null, // Use default from config
            maxTokens: null, // Use default from config
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gọi Groq API với model cụ thể (sử dụng common config API key)
    /// Call Groq API with specific model (uses common config API key)
    /// </summary>
    private async Task<string> CallGroqApiAsync(
        string prompt,
        string model,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Groq API is not enabled");
        }

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured");
        }

        return await CallGroqApiWithModelAsync(
            prompt,
            model,
            _config.ApiKey,
            temperature ?? _config.Temperature,
            maxTokens ?? _config.MaxTokens,
            _config.MaxRetries,
            _config.TimeoutSeconds,
            cancellationToken);
    }

    /// <summary>
    /// Gọi Groq API với model cụ thể và API key cụ thể
    /// Call Groq API with specific model and API key
    /// </summary>
    private async Task<string> CallGroqApiWithModelAsync(
        string prompt,
        string model,
        string apiKey,
        double temperature,
        int maxTokens,
        int maxRetries,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        ValidateApiConfiguration(apiKey);

        var url = $"{_config.ApiEndpoint}/openai/v1/chat/completions";
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await ExecuteApiCall(prompt, model, apiKey, temperature, maxTokens, timeoutSeconds, url, attempt, maxRetries, cancellationToken);
            }
            catch (HttpRequestException ex) when (IsRateLimitException(ex))
            {
                lastException = await HandleRateLimitException(ex, model, attempt, maxRetries, cancellationToken);
            }
            catch (TaskCanceledException ex) when (IsTimeoutException(ex, cancellationToken))
            {
                lastException = await HandleTimeoutException(ex, model, timeoutSeconds, attempt, maxRetries);
            }
            catch (Exception ex)
            {
                lastException = await HandleGenericException(ex, model, attempt, maxRetries, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Failed to call Groq API with model {model} after {maxRetries} attempts. Last error: {lastException?.Message}",
            lastException);
    }

    private void ValidateApiConfiguration(string apiKey)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Groq API is not enabled");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured");
        }
    }

    private async Task<string> ExecuteApiCall(
        string prompt,
        string model,
        string apiKey,
        double temperature,
        int maxTokens,
        int timeoutSeconds,
        string url,
        int attempt,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var request = CreateHttpRequest(prompt, model, apiKey, temperature, maxTokens, url);
        _logger.LogInformation("Calling Groq API: {Model} (attempt {Attempt}/{MaxRetries})", model, attempt + 1, maxRetries);

        var response = await _httpClient.SendAsync(request, timeoutCts.Token);
        var responseContent = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            HandleErrorResponse(response, responseContent);
        }

        var groqResponse = ParseGroqResponse(responseContent);
        var generatedText = ExtractGeneratedTextOrThrow(groqResponse, responseContent);

        _logger.LogInformation(
            "Successfully called Groq using {Model}, generated {Length} characters",
            model,
            generatedText.Length);

        return generatedText;
    }

    private HttpRequestMessage CreateHttpRequest(
        string prompt,
        string model,
        string apiKey,
        double temperature,
        int maxTokens,
        string url)
    {
        var requestBody = BuildRequestBody(prompt, model, temperature, maxTokens);
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = httpContent
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        return request;
    }

    private static bool IsRateLimitException(HttpRequestException ex)
    {
        return ex.Message.Contains("429") || ex.Message.Contains("quota") || ex.Message.Contains("rate limit");
    }

    private static bool IsTimeoutException(TaskCanceledException ex, CancellationToken cancellationToken)
    {
        return ex.CancellationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
    }

    private async Task<Exception> HandleRateLimitException(
        HttpRequestException ex,
        string model,
        int attempt,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(ex, "Groq API rate limit exceeded for model {Model}", model);

        if (attempt >= maxRetries - 1)
        {
            throw new InvalidOperationException(
                $"Groq API rate limit exceeded for model {model}. Please wait and try again later.",
                ex);
        }

        var delay = (attempt + 1) * 1000; // Exponential backoff
        await Task.Delay(delay, cancellationToken);
        return ex;
    }

    private Task<Exception> HandleTimeoutException(
        TaskCanceledException ex,
        string model,
        int timeoutSeconds,
        int attempt,
        int maxRetries)
    {
        _logger.LogWarning(ex, "Timeout calling Groq API with model {Model} ({Timeout}s)", model, timeoutSeconds);

        if (attempt >= maxRetries - 1)
        {
            throw new InvalidOperationException(
                $"Timeout calling Groq API with model {model} after {maxRetries} attempts.",
                ex);
        }

        return Task.FromResult<Exception>(ex);
    }

    private async Task<Exception> HandleGenericException(
        Exception ex,
        string model,
        int attempt,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(ex, "Failed to call Groq API with model {Model} (attempt {Attempt}/{MaxRetries})", model, attempt + 1, maxRetries);

        if (attempt >= maxRetries - 1)
        {
            throw new InvalidOperationException(
                $"Failed to call Groq API with model {model} after {maxRetries} attempts. Error: {ex.Message}",
                ex);
        }

        var delay = (attempt + 1) * 500;
        await Task.Delay(delay, cancellationToken);
        return ex;
    }

    private object BuildRequestBody(string prompt, string model, double temperature, int maxTokens)
    {
        return new
        {
            model = model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = temperature,
            max_tokens = maxTokens
        };
    }

    private void HandleErrorResponse(
        HttpResponseMessage response,
        string responseContent)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Groq API rate limit exceeded. Response: {Response}", responseContent);
            throw new HttpRequestException(
                "Groq API rate limit exceeded. Please wait and try again later.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Groq API unauthorized. Check your API key.");
            throw new HttpRequestException("Groq API unauthorized. Please check your API key.");
        }

        _logger.LogWarning(
            "Groq API failed: {StatusCode}, Response: {Response}",
            response.StatusCode,
            responseContent);

        throw new HttpRequestException($"Groq API returned error: {response.StatusCode}");
    }

    private GroqApiResponse ParseGroqResponse(string responseContent)
    {
        return JsonSerializer.Deserialize<GroqApiResponse>(
                   responseContent,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new GroqApiResponse();
    }

    private string ExtractGeneratedTextOrThrow(GroqApiResponse groqResponse, string responseContent)
    {
        var generatedText = groqResponse.Choices?[0]?.Message?.Content;

        if (!string.IsNullOrEmpty(generatedText))
        {
            return generatedText;
        }

        var finishReason = groqResponse.Choices?[0]?.FinishReason ?? "unknown";
        _logger.LogWarning(
            "Groq API returned empty text. FinishReason: {FinishReason}. Full response: {Response}",
            finishReason,
            responseContent);

        throw new InvalidOperationException(
            $"Groq API returned empty text. FinishReason: {finishReason}. " +
            "This may indicate an issue with the prompt or API response format.");
    }

    #region Response Models

    private class GroqApiResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    #endregion
}

