using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for analyzing dermatology images using AILabTools Detect Skin Disease API
/// </summary>
public class DermatologyAnalysisService : IDermatologyAnalysisService
{
    private readonly ILogger<DermatologyAnalysisService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AILabToolsConfiguration _aiLabToolsConfig;
    private readonly IConversationSessionService _sessionService;
    private readonly GroqApiHelper _groqApiHelper;
    private readonly ServiceGroqConfiguration _serviceConfig;
    private readonly IMemoryCache _cache;
    private readonly RecommendationHelper _recommendationHelper;
    private readonly FileUploadHelper _fileUploadHelper;

    private const string CACHE_KEY_PREFIX = "groq_disease_translation_";
    private static readonly TimeSpan TranslationCacheDuration = TimeSpan.FromDays(30);
    private static readonly TimeSpan AdviceCacheDuration = TimeSpan.FromDays(7);

    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "Constructor injects required dependencies for dermatology analysis; grouping them would complicate DI configuration without improving readability.")]
    public DermatologyAnalysisService(
        ILogger<DermatologyAnalysisService> logger,
        HttpClient httpClient,
        IOptions<AILabToolsConfiguration> aiLabToolsConfig,
        IConversationSessionService sessionService,
        GroqApiHelper groqApiHelper,
        IOptions<GroqServicesConfiguration> groqServicesConfig,
        IMemoryCache cache,
        RecommendationHelper recommendationHelper,
        FileUploadHelper fileUploadHelper)
    {
        _logger = logger;
        _httpClient = httpClient;
        _aiLabToolsConfig = aiLabToolsConfig.Value;
        _sessionService = sessionService;
        _groqApiHelper = groqApiHelper;
        _serviceConfig = groqServicesConfig.Value.DermatologyAnalysis;
        _cache = cache;
        _recommendationHelper = recommendationHelper;
        _fileUploadHelper = fileUploadHelper;

        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_aiLabToolsConfig.RequestTimeoutSeconds);
    }

    public async Task<DermatologyAnalysisResponse> AnalyzeSkinImageAsync(
        IFormFile file,
        LocationContext? location,
        Guid? userId,
        Guid? sessionId)
    {
        try
        {
            _logger.LogInformation("Starting dermatology analysis for user {UserId}", userId);

            // Step 1: Get or create session
            var actualSessionId = await _sessionService.GetOrCreateSessionAsync(
                sessionId,
                userId ?? Guid.Empty,
                location);
            _logger.LogInformation("Using session {SessionId}", actualSessionId);

            // Step 2: Upload image to S3
            var imageUrl = await _fileUploadHelper.UploadToS3Async(file, userId, "dermatology");
            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);

            // Step 3: Analyze with AILabTools API
            var aiLabToolsResult = await AnalyzeWithAILabToolsAsync(file);
            _logger.LogInformation("AILabTools analysis completed");

            // Step 4: Map to response model
            var response = MapToResponse(actualSessionId, imageUrl, aiLabToolsResult);

            // Step 5: Get doctor and hospital recommendations (parallel)
            var dermatologySpecialty = new List<string> { "Da liễu", "Dermatology" };
            var (doctors, hospitals) = await _recommendationHelper.GetRecommendationsAsync(dermatologySpecialty, location);
            response.RecommendedDoctors = doctors;
            response.RecommendedHospitals = hospitals;

            // Step 6: Save to session
            await SaveDermatologyAnalysisAsync(actualSessionId, userId, file.FileName, imageUrl, response, location);

            _logger.LogInformation("Dermatology analysis completed and saved to session {SessionId}", actualSessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing dermatology image: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze dermatology image", ex);
        }
    }

    #region AILabTools API Integration

    private async Task<AILabToolsAnalysisResult> AnalyzeWithAILabToolsAsync(IFormFile file)
    {
        try
        {
            // Validate API key
            if (string.IsNullOrEmpty(_aiLabToolsConfig.ApiKey))
            {
                throw new InvalidOperationException("AILabTools API key is not configured");
            }

            // Call AILabTools API
            var url = $"{_aiLabToolsConfig.ApiBaseUrl}/api/portrait/analysis/skin-disease-detection";

            using var formData = new MultipartFormDataContent();

            // Add image file
            using var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            formData.Add(fileContent, "image", file.FileName);

            // Set API key header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("ailabapi-api-key", _aiLabToolsConfig.ApiKey);

            _logger.LogInformation("Calling AILabTools API: {Url}", url);
            var response = await _httpClient.PostAsync(url, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AILabTools API error: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"AILabTools API returned error: {response.StatusCode}");
            }

            return await ParseAILabToolsResponseAsync(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calling AILabTools API for file '{FileName}' with content type {ContentType}",
                file.FileName,
                file.ContentType);

            throw new InvalidOperationException(
                $"Error calling AILabTools API for file '{file.FileName}'.",
                ex);
        }
    }

    private async Task<AILabToolsAnalysisResult> ParseAILabToolsResponseAsync(string responseJson)
    {
        try
        {
            _logger.LogInformation("Parsing AILabTools response: {Response}", responseJson);

            using var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            var (diagnosis, advice) = await ExtractDiagnosisAsync(root, responseJson);
            await EnrichAdviceWithGroqAsync(diagnosis, advice);

            return new AILabToolsAnalysisResult
            {
                Diagnosis = diagnosis,
                GeneralAdvice = advice,
                DetailedConclusion = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AILabTools response");
            throw new InvalidOperationException("Failed to parse AILabTools API response", ex);
        }
    }

    private async Task<(SkinConditionDiagnosis Diagnosis, List<string> Advice)> ExtractDiagnosisAsync(
        JsonElement root,
        string responseJson)
    {
        var diagnosis = new SkinConditionDiagnosis();
        var advice = new List<string>();

        if (!IsSuccessResponse(root))
        {
            ThrowAiLabError(root, responseJson);
        }

        if (TryGetResultsEnglish(root, out var resultsEnglish))
        {
            await PopulateDiagnosisFromResultsAsync(resultsEnglish, diagnosis, advice);
        }
        else
        {
            SetUnknownDiagnosis(diagnosis, "No results_english found in AILabTools response data");
        }

        return (diagnosis, advice);
    }

    private static bool IsSuccessResponse(JsonElement root)
    {
        return root.TryGetProperty("error_code", out var errorCode) && errorCode.GetInt32() == 0;
    }

    private static bool TryGetResultsEnglish(JsonElement root, out JsonElement resultsEnglish)
    {
        resultsEnglish = default;
        if (!root.TryGetProperty("data", out var data))
        {
            return false;
        }

        return data.TryGetProperty("results_english", out resultsEnglish);
    }

    private async Task PopulateDiagnosisFromResultsAsync(
        JsonElement resultsEnglish,
        SkinConditionDiagnosis diagnosis,
        List<string> advice)
    {
        var (topDiseaseName, topConfidence) = FindTopDisease(resultsEnglish);

        if (string.IsNullOrEmpty(topDiseaseName))
        {
            SetUnknownDiagnosis(diagnosis, "No disease predictions found in AILabTools response");
            return;
        }

        diagnosis.ConditionName = await MapDiseaseNameToVietnamese(topDiseaseName);
        diagnosis.Confidence = topConfidence;

        ApplySeverityRules(topDiseaseName.ToLowerInvariant(), diagnosis, advice);
    }

    private static (string? Name, double Confidence) FindTopDisease(JsonElement resultsEnglish)
    {
        string? topDiseaseName = null;
        double topConfidence = 0;

        foreach (var property in resultsEnglish.EnumerateObject())
        {
            var confidence = property.Value.GetDouble();
            if (confidence > topConfidence)
            {
                topConfidence = confidence;
                topDiseaseName = property.Name;
            }
        }

        return (topDiseaseName, topConfidence);
    }

    private void ApplySeverityRules(
        string diseaseNameLower,
        SkinConditionDiagnosis diagnosis,
        List<string> advice)
    {
        if (IsHighRiskDisease(diseaseNameLower))
        {
            advice.Add("Cần đến gặp bác sĩ da liễu NGAY để được thăm khám và sinh thiết");
            diagnosis.Severity = "Nặng";
            return;
        }

        if (IsMediumRiskDisease(diseaseNameLower))
        {
            advice.Add("Nên đến gặp bác sĩ da liễu trong vòng 1-2 tuần để được đánh giá");
            diagnosis.Severity = "Trung bình";
            return;
        }

        diagnosis.Severity = "Nhẹ";
    }

    private static bool IsHighRiskDisease(string diseaseNameLower)
    {
        return diseaseNameLower.Contains("melanoma") ||
               diseaseNameLower.Contains("carcinoma") ||
               diseaseNameLower.Contains("cancer") ||
               diseaseNameLower.Contains("malignant");
    }

    private static bool IsMediumRiskDisease(string diseaseNameLower)
    {
        return diseaseNameLower.Contains("keratosis") ||
               diseaseNameLower.Contains("nevus") ||
               diseaseNameLower.Contains("mole") ||
               diseaseNameLower.Contains("wart");
    }

    private void SetUnknownDiagnosis(SkinConditionDiagnosis diagnosis, string logMessage)
    {
        _logger.LogWarning(logMessage);
        diagnosis.ConditionName = "Không xác định";
        diagnosis.Confidence = 0;
        diagnosis.Severity = "Unknown";
    }

    private void ThrowAiLabError(JsonElement root, string responseJson)
    {
        var errorMessage = "Unknown error";
        if (root.TryGetProperty("error_detail", out var errorDetail) &&
            errorDetail.TryGetProperty("message", out var msg))
        {
            errorMessage = msg.GetString() ?? errorMessage;
        }

        var actualErrorCode = root.TryGetProperty("error_code", out var errorCodeProp)
            ? errorCodeProp.GetInt32()
            : -1;

        _logger.LogError(
            "AILabTools API returned error. Code: {Code}, Message: {Message}, Full Response: {Response}",
            actualErrorCode,
            errorMessage,
            responseJson);

        throw new InvalidOperationException(
            $"AILabTools API returned error (code: {actualErrorCode}): {errorMessage}");
    }

    private async Task EnrichAdviceWithGroqAsync(SkinConditionDiagnosis diagnosis, List<string> advice)
    {
        if (string.IsNullOrEmpty(diagnosis.ConditionName))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Generating general advice for: {DiseaseName}", diagnosis.ConditionName);
            var adviceText = await GenerateGeneralAdviceAsync(
                diagnosis.ConditionName,
                diagnosis.Severity ?? "Nhẹ");

            var generatedAdvice = adviceText
                .Split('\n')
                .Select(line => line.Trim().TrimStart('-', '*', '•').Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .Take(5)
                .ToList();

            if (generatedAdvice.Count > 0)
            {
                advice.AddRange(generatedAdvice);
                _logger.LogInformation(
                    "Added {Count} advice items for {DiseaseName}",
                    generatedAdvice.Count,
                    diagnosis.ConditionName);
            }
            else
            {
                _logger.LogWarning("No advice generated from Groq, using fallback");
                AddFallbackAdvice(advice, diagnosis.ConditionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate general advice for {DiseaseName}, using fallback",
                diagnosis.ConditionName);
            AddFallbackAdvice(advice, diagnosis.ConditionName);
        }
    }

    #endregion

    #region Response Mapping

    private DermatologyAnalysisResponse MapToResponse(
        Guid sessionId,
        string imageUrl,
        AILabToolsAnalysisResult aiLabToolsResult)
    {
        return new DermatologyAnalysisResponse
        {
            SessionId = sessionId,
            ImageUrl = imageUrl,
            Diagnosis = aiLabToolsResult.Diagnosis,
            GeneralAdvice = aiLabToolsResult.GeneralAdvice,
            Disclaimer = "Lưu ý: Đây chỉ là công cụ hỗ trợ chẩn đoán, không thay thế khám lâm sàng của bác sĩ chuyên khoa Da liễu. Vui lòng đến cơ sở y tế để được thăm khám và điều trị chính xác.",
            Timestamp = DateTime.UtcNow
        };
    }



    #endregion

    #region Session Persistence

    private async Task SaveDermatologyAnalysisAsync(
        Guid sessionId,
        Guid? userId,
        string fileName,
        string imageUrl,
        DermatologyAnalysisResponse response,
        LocationContext? location)
    {
        try
        {
            var userMessage = $"Phân tích ảnh da: {fileName}";
            var aiMessage = BuildAIMessage(response);

            var suggestions = new
            {
                doctors = response.RecommendedDoctors,
                hospitals = response.RecommendedHospitals,
                imageUrl
            };

            var disease = response.Diagnosis != null ? new
            {
                Name = response.Diagnosis.ConditionName,
                Confidence = response.Diagnosis.Confidence,
                Severity = response.Diagnosis.Severity
            } : null;

            await _sessionService.SaveConversationHistoryAsync(
                sessionId: sessionId,
                userMessage: userMessage,
                aiMessage: aiMessage,
                location: location,
                suggestions: suggestions,
                userId: userId,
                disease: disease,
                questionCount: null,
                analysisComplete: true
            );

            _logger.LogInformation("Successfully saved dermatology analysis to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Failed to save dermatology analysis to session {SessionId}. User will lose history on reload!", sessionId);
            // Don't throw - we still want to return the analysis result to user
            // But log as ERROR so it's visible in logs
        }
    }

    private string BuildAIMessage(DermatologyAnalysisResponse response)
    {
        var messageBuilder = new StringBuilder();

        if (response.Diagnosis != null)
        {
            messageBuilder.AppendLine($"**Chẩn đoán khả năng:** {response.Diagnosis.ConditionName}");
            messageBuilder.AppendLine($"**Độ tin cậy:** {response.Diagnosis.Confidence:P0}");
            messageBuilder.AppendLine();
        }

        if (response.GeneralAdvice.Count > 0)
        {
            messageBuilder.AppendLine("**Lời khuyên:**");
            foreach (var advice in response.GeneralAdvice)
            {
                messageBuilder.AppendLine($"- {advice}");
            }
            messageBuilder.AppendLine();
        }

        messageBuilder.AppendLine(response.Disclaimer);

        return messageBuilder.ToString();
    }

    #endregion

    #region Helper Methods

    private void AddFallbackAdvice(List<string> advice, string? diseaseName)
    {
        var conditionText = string.IsNullOrWhiteSpace(diseaseName)
            ? "tình trạng da"
            : $"tình trạng **{diseaseName}**";

        // Generic fallback advice, slightly tailored with disease name (if available)
        advice.Add($"Giữ vệ sinh da sạch sẽ để hạn chế làm nặng thêm {conditionText}");
        advice.Add("Tránh gãi hoặc chạm vào vùng da bị tổn thương");
        advice.Add("Đến gặp bác sĩ da liễu để được thăm khám và điều trị đúng cách");
    }

    private async Task<string> MapDiseaseNameToVietnamese(string englishName)
    {
        try
        {
            // Use Groq AI to translate disease name
            var vietnameseName = await TranslateDiseaseNameAsync(englishName);
            return vietnameseName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to translate disease name using Groq AI: {DiseaseName}", englishName);

            // Fallback: return the English name with proper capitalization
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                englishName.ToLower().Replace("_", " ")
            );
        }
    }

    #region Groq AI Helper Methods

    /// <summary>
    /// Translate disease name from English to Vietnamese with medical context
    /// </summary>
    private async Task<string> TranslateDiseaseNameAsync(
        string englishDiseaseName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"{CACHE_KEY_PREFIX}{englishDiseaseName.ToLower()}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedTranslation))
            {
                _logger.LogInformation("Using cached translation for: {DiseaseName}", englishDiseaseName);
                return cachedTranslation!;
            }

            _logger.LogInformation("Translating disease name: {DiseaseName}", englishDiseaseName);

            var prompt = $@"Bạn là một chuyên gia y khoa. Hãy dịch tên bệnh da liễu sau từ tiếng Anh sang tiếng Việt.
Chỉ trả về tên bệnh bằng tiếng Việt, không giải thích thêm.

Tên bệnh (tiếng Anh): {englishDiseaseName}

Lưu ý:
- Nếu là tên bệnh chuyên môn, hãy dùng thuật ngữ y khoa tiếng Việt chính xác
- Nếu không có thuật ngữ tiếng Việt phổ biến, hãy giữ nguyên tên tiếng Anh
- Trả về ngắn gọn, chỉ tên bệnh

Tên bệnh (tiếng Việt):";

            var translation = await GenerateTextAsync(prompt, temperature: 0.1, cancellationToken);

            // Clean up the translation (remove quotes, extra whitespace, etc.)
            translation = translation.Trim().Trim('"', '\'', '.', ',');

            // Cache the translation
            _cache.Set(cacheKey, translation, TranslationCacheDuration);

            _logger.LogInformation("Translated '{English}' to '{Vietnamese}'", englishDiseaseName, translation);

            return translation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating disease name: {DiseaseName}", englishDiseaseName);

            // Fallback: return the English name with proper capitalization
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                englishDiseaseName.ToLower().Replace("_", " ")
            );
        }
    }

    /// <summary>
    /// Generate general advice for a specific skin condition (3-5 bullet points)
    /// </summary>
    private async Task<string> GenerateGeneralAdviceAsync(
        string diseaseName,
        string severity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"groq_advice_{diseaseName.ToLower()}_{severity}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedAdvice))
            {
                _logger.LogInformation("Using cached advice for: {DiseaseName}", diseaseName);
                return cachedAdvice!;
            }

            _logger.LogInformation("Generating general advice for: {DiseaseName}", diseaseName);

            var prompt = $@"Bạn là một bác sĩ da liễu. Hãy đưa ra 3-5 lời khuyên chăm sóc da cụ thể cho bệnh nhân bị {diseaseName} (mức độ: {severity}).

YÊU CẦU:
- Viết bằng tiếng Việt
- Mỗi lời khuyên là một câu ngắn gọn, dễ hiểu
- Tập trung vào: vệ sinh da, chế độ ăn uống, sinh hoạt, điều cần tránh
- Phù hợp với mức độ nghiêm trọng {severity}
- Không đưa ra lời khuyên về thuốc cụ thể
- Mỗi lời khuyên trên một dòng, bắt đầu bằng dấu gạch ngang (-)

Ví dụ format:
- Giữ vệ sinh da sạch sẽ, rửa mặt 2 lần/ngày
- Tránh chạm tay vào vùng da bị tổn thương
- Sử dụng kem chống nắng SPF 30+ khi ra ngoài

Hãy đưa ra 3-5 lời khuyên cho {diseaseName}:";

            var advice = await GenerateTextAsync(prompt, temperature: 0.3, cancellationToken);

            // Cache the advice for 7 days
            _cache.Set(cacheKey, advice, AdviceCacheDuration);

            _logger.LogInformation("Generated advice for '{DiseaseName}'", diseaseName);

            return advice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating general advice for: {DiseaseName}", diseaseName);

            // Fallback: return generic advice
            return @"- Giữ vệ sinh da sạch sẽ
- Tránh tiếp xúc trực tiếp với ánh nắng mặt trời";
        }
    }

    /// <summary>
    /// Generate text using Groq AI with custom prompt
    /// </summary>
    private async Task<string> GenerateTextAsync(
        string prompt,
        double temperature = 0.3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _serviceConfig,
                temperature: temperature,
                maxTokens: null, // Use default from config
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var promptPreview = prompt.Length > 200 ? prompt[..200] + "..." : prompt;

            _logger.LogError(
                ex,
                "Error calling Groq API for dermatology service. Temperature={Temperature}, PromptPreview={PromptPreview}",
                temperature,
                promptPreview);

            throw new InvalidOperationException(
                "Failed to generate dermatology-related text using Groq API.",
                ex);
        }
    }

    #endregion

    #endregion

    #region Helper Classes

    private class AILabToolsAnalysisResult
    {
        public SkinConditionDiagnosis Diagnosis { get; set; } = new();
        public List<string> GeneralAdvice { get; set; } = new();
        public string? DetailedConclusion { get; set; }
    }

    #endregion
}
