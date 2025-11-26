using System.Net.Http.Headers;
using System.Runtime.InteropServices;
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
    private const string SafetyThreshold = "BLOCK_MEDIUM_AND_ABOVE";

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
    private static string BuildMedicalSummaryPrompt(GenerateMedicalSummaryRequest request)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine(
            "Bạn là một trợ lý y tế AI chuyên nghiệp. Nhiệm vụ của bạn là tóm tắt cuộc trò chuyện khám bệnh giữa bác sĩ và bệnh nhân thành một hồ sơ y tế có cấu trúc, đẹp mắt và chuyên nghiệp."
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**THÔNG TIN CUỘC HẸN:**");

        if (!string.IsNullOrEmpty(request.DoctorName))
            promptBuilder.AppendLine($"- Bác sĩ: {request.DoctorName}");

        if (!string.IsNullOrEmpty(request.PatientName))
            promptBuilder.AppendLine($"- Bệnh nhân: {request.PatientName}");

        if (request.AppointmentDate.HasValue)
        {
            promptBuilder.AppendLine(
                $"- Ngày khám: {request.AppointmentDate.Value:dd/MM/yyyy HH:mm}"
            );
        }
        else
        {
            // Use Vietnam current time when appointment date is not provided
            promptBuilder.AppendLine($"- Ngày khám: {GetVietnamNow():dd/MM/yyyy HH:mm}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**CUỘC TRÒ CHUYỆN:**");
        promptBuilder.AppendLine(request.Transcript);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU TÓM TẮT:**");
        promptBuilder.AppendLine(
            "Hãy tóm tắt cuộc trò chuyện trên theo định dạng hồ sơ bệnh án chuyên nghiệp với template đẹp mắt như sau:"
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("                    📋 HỒ SƠ KHÁM BỆNH");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 🩺 TRIỆU CHỨNG");
        promptBuilder.AppendLine("Mô tả các triệu chứng chính mà bệnh nhân trình bày:");
        promptBuilder.AppendLine("• [Liệt kê từng triệu chứng với bullet points]");
        promptBuilder.AppendLine("• Bao gồm thời gian xuất hiện, mức độ nghiêm trọng nếu có");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("───────────────────────────────────────────────────────────");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 📜 TIỀN SỬ BỆNH");
        promptBuilder.AppendLine("Thông tin về tiền sử bệnh lý có liên quan:");
        promptBuilder.AppendLine("• [Liệt kê các bệnh lý đã có]");
        promptBuilder.AppendLine("• Thuốc đang sử dụng (nếu có)");
        promptBuilder.AppendLine("• Dị ứng thuốc (nếu có)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("───────────────────────────────────────────────────────────");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 🔍 KHÁM LÂM SÀNG");
        promptBuilder.AppendLine("Kết quả khám lâm sàng:");
        promptBuilder.AppendLine("• Các chỉ số sinh tồn (huyết áp, mạch, nhiệt độ nếu có)");
        promptBuilder.AppendLine("• Kết quả khám chi tiết theo từng cơ quan/hệ thống");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("───────────────────────────────────────────────────────────");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 💊 CHẨN ĐOÁN SƠ BỘ");
        promptBuilder.AppendLine("**Chẩn đoán:** [Ghi rõ chẩn đoán của bác sĩ]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Đánh giá:** [Mức độ nghiêm trọng, tiên lượng]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("───────────────────────────────────────────────────────────");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 💉 HƯỚNG XỬ TRÍ");
        promptBuilder.AppendLine("### Đơn thuốc:");
        promptBuilder.AppendLine("1. [Tên thuốc] - [Liều lượng] - [Cách dùng]");
        promptBuilder.AppendLine("2. [Tên thuốc] - [Liều lượng] - [Cách dùng]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Hướng dẫn điều trị:");
        promptBuilder.AppendLine("• [Các hướng dẫn chăm sóc tại nhà]");
        promptBuilder.AppendLine("• [Chế độ ăn uống, sinh hoạt]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("───────────────────────────────────────────────────────────");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## ⚠️ LƯU Ý ĐẶC BIỆT");
        promptBuilder.AppendLine("• [Các triệu chứng cần theo dõi]");
        promptBuilder.AppendLine("• [Khi nào cần tái khám]");
        promptBuilder.AppendLine("• [Các lưu ý quan trọng khác]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**HƯỚNG DẪN ĐỊNH DẠNG:**");
        promptBuilder.AppendLine("- Sử dụng CHÍNH XÁC template trên với các ký hiệu đường kẻ (═, ─)");
        promptBuilder.AppendLine("- Giữ nguyên các emoji (🩺, 📜, 🔍, 💊, 💉, ⚠️, 📋) để tạo điểm nhấn");
        promptBuilder.AppendLine("- Sử dụng bullet points (•) cho các danh sách");
        promptBuilder.AppendLine("- Sử dụng số thứ tự (1., 2., 3.) cho đơn thuốc");
        promptBuilder.AppendLine("- Giữ khoảng cách và căn lề đẹp mắt");
        promptBuilder.AppendLine("- Sử dụng **bold** cho tiêu đề quan trọng");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**QUY TẮC NỘI DUNG:**");
        promptBuilder.AppendLine("- Chỉ tóm tắt thông tin có trong cuộc trò chuyện, KHÔNG tự thêm thông tin");
        promptBuilder.AppendLine("- Sử dụng ngôn ngữ y tế chuyên nghiệp nhưng dễ hiểu");
        promptBuilder.AppendLine("- Nếu thiếu thông tin cho mục nào, ghi '*Không có thông tin*' với font chữ nghiêng");
        promptBuilder.AppendLine("- Giữ tóm tắt ngắn gọn, súc tích nhưng đầy đủ thông tin");
        promptBuilder.AppendLine("- Sử dụng tiếng Việt có dấu chính xác");
        promptBuilder.AppendLine("- Thể hiện sự chuyên nghiệp và tỉ mỉ trong từng chi tiết");

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
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = SafetyThreshold },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = SafetyThreshold },
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

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
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

    private sealed class GeminiApiResponseesponse
    {
        public Candidate[]? Candidates { get; set; } = Array.Empty<Candidate>();
    }

    private sealed class Candidatendidate
    {
        public Content? Content { get; set; } = new Content();
    }

    private sealed class ContentContent
    {
        public Part[]? Parts { get; set; } = Array.Empty<Part>();
    }

    private sealed class Part
    {
        public string? Text { get; set; } = string.Empty;
    }

    #endregion


    // Helper: return current time in Vietnam timezone (handles Windows and Linux time zone ids)
    private static DateTime GetVietnamNow()
    {
        try
        {
            var tz = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") // Windows
                : TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux/macOS (IANA)

            // Convert from UTC to target timezone
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            // Fallback: add +7 hours to UTC
            return DateTime.UtcNow.AddHours(7);
        }
    }
}
