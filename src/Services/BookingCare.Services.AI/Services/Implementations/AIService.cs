using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service implementation for AI operations using Google Gemini API
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _geminiConfig;
    private readonly ILogger<AIService> _logger;

    public AIService(
        HttpClient httpClient,
        IOptions<GeminiConfiguration> geminiConfig,
        ILogger<AIService> logger
    )
    {
        _httpClient = httpClient;
        _geminiConfig = geminiConfig.Value;
        _logger = logger;
    }

    public async Task<MedicalSummaryResponse> GenerateMedicalSummaryAsync(
        GenerateMedicalSummaryRequest request
    )
    {
        try
        {
            _logger.LogInformation(
                "Generating medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            // Build the prompt for Gemini
            var prompt = BuildMedicalSummaryPrompt(request);

            // Call Gemini API
            var geminiResponse = await CallGeminiApiAsync(prompt);

            _logger.LogInformation(
                "Successfully generated medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            return new MedicalSummaryResponse
            {
                Summary = geminiResponse,
                AppointmentId = request.AppointmentId,
                GeneratedAt = DateTime.UtcNow,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            return new MedicalSummaryResponse
            {
                Summary = string.Empty,
                AppointmentId = request.AppointmentId,
                GeneratedAt = DateTime.UtcNow,
                Success = false,
                ErrorMessage = $"Lỗi khi tạo tóm tắt: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Build medical summary prompt for Gemini AI
    /// </summary>
    private string BuildMedicalSummaryPrompt(GenerateMedicalSummaryRequest request)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine(
            "Bạn là một trợ lý y tế AI chuyên nghiệp. Nhiệm vụ của bạn là tóm tắt cuộc trò chuyện khám bệnh giữa bác sĩ và bệnh nhân thành một hồ sơ y tế có cấu trúc."
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**THÔNG TIN CUỘC HẸN:**");

        if (!string.IsNullOrEmpty(request.DoctorName))
            promptBuilder.AppendLine($"- Bác sĩ: {request.DoctorName}");

        if (!string.IsNullOrEmpty(request.PatientName))
            promptBuilder.AppendLine($"- Bệnh nhân: {request.PatientName}");

        if (request.AppointmentDate.HasValue)
            promptBuilder.AppendLine(
                $"- Ngày khám: {request.AppointmentDate.Value:dd/MM/yyyy HH:mm}"
            );

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**CUỘC TRÒ CHUYỆN:**");
        promptBuilder.AppendLine(request.Transcript);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU TÓM TẮT:**");
        promptBuilder.AppendLine(
            "Hãy tóm tắt cuộc trò chuyện trên theo định dạng hồ sơ bệnh án chuyên nghiệp với các mục sau:"
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(
            "1. **TRIỆU CHỨNG (Symptoms):** Các triệu chứng mà bệnh nhân trình bày"
        );
        promptBuilder.AppendLine(
            "2. **TIỀN SỬ BỆNH (Medical History):** Tiền sử bệnh lý có liên quan được đề cập"
        );
        promptBuilder.AppendLine(
            "3. **KHÁM LÂM SÀNG (Clinical Examination):** Kết quả khám nếu có"
        );
        promptBuilder.AppendLine(
            "4. **CHẨN ĐOÁN SƠ BỘ (Preliminary Diagnosis):** Chẩn đoán hoặc đánh giá của bác sĩ"
        );
        promptBuilder.AppendLine(
            "5. **HƯỚNG XỬ TRÍ (Treatment Plan):** Đề xuất điều trị, toa thuốc, hoặc hướng dẫn"
        );
        promptBuilder.AppendLine(
            "6. **LƯU Ý (Notes):** Các lưu ý khác cho bệnh nhân hoặc theo dõi tiếp"
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**LƯU Ý:**");
        promptBuilder.AppendLine(
            "- Chỉ tóm tắt thông tin có trong cuộc trò chuyện, KHÔNG tự thêm thông tin"
        );
        promptBuilder.AppendLine("- Sử dụng ngôn ngữ y tế chuyên nghiệp nhưng dễ hiểu");
        promptBuilder.AppendLine("- Nếu thiếu thông tin cho mục nào, ghi 'Không có thông tin'");
        promptBuilder.AppendLine("- Giữ tóm tắt ngắn gọn, súc tích (khoảng 200-400 từ)");
        promptBuilder.AppendLine("- Sử dụng tiếng Việt có dấu");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Call Gemini API to generate content with fallback support
    /// </summary>
    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_geminiConfig.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }

        // Try multiple models and API versions for compatibility (same as GeminiTranscriptionService)
        var modelsToTry = new[]
        {
            "gemini-2.5-flash", // Latest fast model for audio transcription
            "gemini-2.0-flash", // Fallback fast model
            "gemini-2.5-pro", // High quality model
            "gemini-2.0-flash-lite", // Lite version
        };

        var apiVersions = new[] { "v1", "v1beta" };

        Exception? lastException = null;

        // Try each combination of API version and model
        foreach (var apiVersion in apiVersions)
        {
            foreach (var model in modelsToTry)
            {
                try
                {
                    var url =
                        $"{_geminiConfig.ApiEndpoint}/{apiVersion}/models/{model}:generateContent?key={_geminiConfig.ApiKey}";

                    var result = await CallGeminiApiWithUrlAsync(url, prompt, model, apiVersion);
                    return result;
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
                    continue;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogDebug(
                        ex,
                        "Operation error with {ApiVersion}/{Model}, trying next",
                        apiVersion,
                        model
                    );
                    lastException = ex;
                    continue;
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
                    continue;
                }
            }
        }

        // If all attempts failed, throw the last exception
        throw new InvalidOperationException(
            $"Failed to generate content with any available model. Last error: {lastException?.Message}",
            lastException
        );
    }

    /// <summary>
    /// Call Gemini API with specific URL and model
    /// </summary>
    private async Task<string> CallGeminiApiWithUrlAsync(
        string url,
        string prompt,
        string model,
        string apiVersion
    )
    {
        // Gemini API endpoint: /{version}/models/{model}:generateContent

        // Build request body according to Gemini API format
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = _geminiConfig.Temperature,
                maxOutputTokens = _geminiConfig.MaxTokens,
                topP = 0.95,
                topK = 40,
            },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new
                {
                    category = "HARM_CATEGORY_HATE_SPEECH",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE",
                },
                new
                {
                    category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE",
                },
                new
                {
                    category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE",
                },
            },
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogInformation("Trying Gemini API: {ApiVersion}/models/{Model}", apiVersion, model);

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // If 404, let caller retry with different model
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "Model {Model} not found on {ApiVersion}, trying next",
                    model,
                    apiVersion
                );
                throw new HttpRequestException(
                    $"Model not found: {response.StatusCode} - {responseContent}"
                );
            }

            _logger.LogWarning(
                "Gemini API failed: {ApiVersion}/{Model} - Status={StatusCode}, Response={Response}",
                apiVersion,
                model,
                response.StatusCode,
                responseContent
            );
            throw new HttpRequestException(
                $"Gemini API returned error: {response.StatusCode} - {responseContent}"
            );
        }

        // Parse Gemini response
        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
        {
            _logger.LogWarning(
                "No candidates in response from {ApiVersion}/{Model}",
                apiVersion,
                model
            );
            throw new InvalidOperationException(
                $"Gemini API returned no candidates (model may not support this content type)"
            );
        }

        var generatedText = geminiResponse.Candidates[0]?.Content?.Parts?[0]?.Text;

        if (string.IsNullOrEmpty(generatedText))
        {
            _logger.LogWarning("Empty text from {ApiVersion}/{Model}", apiVersion, model);
            throw new InvalidOperationException("Gemini API returned empty text");
        }

        _logger.LogInformation(
            "Successfully generated content using {ApiVersion}/{Model}: Length={Length} characters",
            apiVersion,
            model,
            generatedText.Length
        );

        return generatedText;
    }

    #region Gemini API Response Models

    private class GeminiApiResponse
    {
        public Candidate[]? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public Part[]? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }

    #endregion
}
