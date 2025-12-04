using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Services;

/// <summary>
/// Service for Gemini API audio transcription
/// </summary>
public interface IGeminiTranscriptionService
{
    /// <summary>
    /// Transcribe audio file to text using Gemini API
    /// </summary>
    /// <param name="audioFilePath">Path to audio file (webm or wav)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> TranscribeAudioAsync(
        string audioFilePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Transcribe audio bytes to text using Gemini API
    /// </summary>
    /// <param name="audioBytes">Audio file bytes</param>
    /// <param name="mimeType">Audio MIME type (audio/webm or audio/wav)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcribed text</returns>
    Task<string> TranscribeAudioBytesAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default
    );
}

public class GeminiTranscriptionService : IGeminiTranscriptionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiTranscriptionService> _logger;
    private readonly string _apiKey;
    private readonly string _apiEndpoint;

    public GeminiTranscriptionService(
        IHttpClientFactory httpClientFactory,
        ILogger<GeminiTranscriptionService> logger,
        IConfiguration configuration
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Get API key from configuration
        _apiKey =
            configuration["GeminiServices:MedicalSummary:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");

        // Use base endpoint without version - will try multiple endpoints
        _apiEndpoint = "https://generativelanguage.googleapis.com";

        _logger.LogInformation("GeminiTranscriptionService initialized");
    }

    public async Task<string> TranscribeAudioAsync(
        string audioFilePath,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Validate file exists
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
            }

            // Sanitize file path to prevent path traversal
            var fullPath = Path.GetFullPath(audioFilePath);

            _logger.LogInformation("Reading audio file: {FilePath}", fullPath);

            // Read file as byte array
            var audioBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);

            // Determine MIME type from file extension
            var extension = Path.GetExtension(audioFilePath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".webm" => "audio/webm",
                ".wav" => "audio/wav",
                _ => throw new NotSupportedException($"Unsupported audio format: {extension}"),
            };

            _logger.LogInformation(
                "Audio file loaded: Size={Size} bytes, MimeType={MimeType}",
                audioBytes.Length,
                mimeType
            );

            return await TranscribeAudioBytesAsync(audioBytes, mimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading audio file: {FilePath}", audioFilePath);
            throw new InvalidOperationException($"Failed to read audio file: {audioFilePath}", ex);
        }
    }

    public async Task<string> TranscribeAudioBytesAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Starting Gemini audio transcription: Size={Size} bytes",
                audioBytes.Length
            );

            var jsonPayload = BuildTranscriptionRequestPayload(audioBytes, mimeType);
            var httpClient = CreateConfiguredHttpClient();

            var transcript = await TryAllModelCombinationsAsync(
                httpClient,
                jsonPayload,
                cancellationToken
            );

            return transcript;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw our custom exception as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during audio transcription");
            throw new InvalidOperationException(
                "Unexpected error occurred during audio transcription",
                ex
            );
        }
    }

    private string BuildTranscriptionRequestPayload(byte[] audioBytes, string mimeType)
    {
        var base64Audio = Convert.ToBase64String(audioBytes);

        var requestPayload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inline_data = new { mime_type = mimeType, data = base64Audio } },
                        new
                        {
                            text = "Please transcribe this audio file. Provide only the transcription text without any additional commentary.",
                        },
                    },
                },
            },
        };

        var jsonPayload = JsonSerializer.Serialize(
            requestPayload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        _logger.LogDebug("Request payload size: {Size} bytes", jsonPayload.Length);
        return jsonPayload;
    }

    private HttpClient CreateConfiguredHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for audio processing
        return httpClient;
    }

    private async Task<string> TryAllModelCombinationsAsync(
        HttpClient httpClient,
        string jsonPayload,
        CancellationToken cancellationToken
    )
    {
        var modelsToTry = new[]
        {
            "gemini-2.5-flash",
            "gemini-2.0-flash",
            "gemini-2.5-pro",
            "gemini-2.0-flash-lite",
        };

        var apiVersions = new[] { "v1", "v1beta" };
        Exception? lastException = null;

        foreach (var model in modelsToTry)
        {
            foreach (var apiVersion in apiVersions)
            {
                try
                {
                    var transcript = await TryTranscribeWithModelAsync(
                        httpClient,
                        jsonPayload,
                        model,
                        apiVersion,
                        cancellationToken
                    );

                    if (transcript != null)
                        return transcript;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogDebug(
                        ex,
                        "HTTP error with {ApiVersion}/{Model}, trying next",
                        apiVersion,
                        model
                    );
                    lastException = ex;
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(
                        ex,
                        "JSON error with {ApiVersion}/{Model}, trying next",
                        apiVersion,
                        model
                    );
                    lastException = ex;
                }
            }
        }

        _logger.LogError(lastException, "All Gemini API attempts failed for audio transcription");
        throw new InvalidOperationException(
            $"Failed to transcribe audio after trying all available models. Last error: {lastException?.Message}",
            lastException
        );
    }

    private async Task<string?> TryTranscribeWithModelAsync(
        HttpClient httpClient,
        string jsonPayload,
        string model,
        string apiVersion,
        CancellationToken cancellationToken
    )
    {
        var requestUrl =
            $"{_apiEndpoint}/{apiVersion}/models/{model}:generateContent?key={_apiKey}";

        _logger.LogInformation("Trying Gemini API: {ApiVersion}/models/{Model}", apiVersion, model);

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"),
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleUnsuccessfulResponse(response, responseContent, model, apiVersion);
        }

        return ExtractTranscriptFromResponse(responseContent, model, apiVersion);
    }

    private string? HandleUnsuccessfulResponse(
        HttpResponseMessage response,
        string responseContent,
        string model,
        string apiVersion
    )
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug(
                "Model {Model} not found on {ApiVersion}, trying next",
                model,
                apiVersion
            );
            return null;
        }

        _logger.LogWarning(
            "Gemini API failed: {ApiVersion}/{Model} - Status={StatusCode}",
            apiVersion,
            model,
            response.StatusCode
        );

        throw new HttpRequestException(
            $"Gemini API request failed with status {response.StatusCode}: {responseContent}"
        );
    }

    private string? ExtractTranscriptFromResponse(
        string responseContent,
        string model,
        string apiVersion
    )
    {
        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
        {
            _logger.LogWarning("No candidates in response from {Model}", model);
            return null;
        }

        var firstCandidate = geminiResponse.Candidates[0];
        var transcript = firstCandidate.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(transcript))
        {
            _logger.LogWarning("Empty transcript from {Model}", model);
            return null;
        }

        _logger.LogInformation(
            "Transcription successful using {ApiVersion}/{Model}: Length={Length} characters",
            apiVersion,
            model,
            transcript.Length
        );

        return transcript.Trim();
    }
}
