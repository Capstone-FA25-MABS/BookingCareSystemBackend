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
    private readonly IDermatologyCacheService _dermatologyCacheService;

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
        FileUploadHelper fileUploadHelper,
        IDermatologyCacheService dermatologyCacheService)
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
        _dermatologyCacheService = dermatologyCacheService;

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

            var (diagnosis, advice, englishName) = await ExtractDiagnosisAsync(root, responseJson);
            if (!string.IsNullOrEmpty(englishName) && advice.Count == 0)
            {
                // Only generate advice if not already cached
                await EnrichAdviceWithGroqAsync(diagnosis, advice, englishName);
            }

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

    private async Task<(SkinConditionDiagnosis Diagnosis, List<string> Advice, string? EnglishName)> ExtractDiagnosisAsync(
        JsonElement root,
        string responseJson)
    {
        var diagnosis = new SkinConditionDiagnosis();
        var advice = new List<string>();
        string? englishName = null;

        if (!IsSuccessResponse(root))
        {
            ThrowAiLabError(root, responseJson);
        }

        if (TryGetResultsEnglish(root, out var resultsEnglish))
        {
            englishName = await PopulateDiagnosisFromResultsAsync(resultsEnglish, diagnosis, advice);
        }
        else
        {
            SetUnknownDiagnosis(diagnosis, "No results_english found in AILabTools response data");
        }

        return (diagnosis, advice, englishName);
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

    private async Task<string?> PopulateDiagnosisFromResultsAsync(
        JsonElement resultsEnglish,
        SkinConditionDiagnosis diagnosis,
        List<string> advice)
    {
        var (topDiseaseName, topConfidence) = FindTopDisease(resultsEnglish);

        if (string.IsNullOrEmpty(topDiseaseName))
        {
            SetUnknownDiagnosis(diagnosis, "No disease predictions found in AILabTools response");
            return null;
        }

        diagnosis.Confidence = topConfidence;
        ApplySeverityRules(topDiseaseName.ToLowerInvariant(), diagnosis, advice);

        // Check cache for this English disease name
        var cachedDisease = await _dermatologyCacheService.FindByEnglishNameAsync(topDiseaseName);

        if (cachedDisease != null)
        {
            // Use cached Vietnamese name and advice
            await _dermatologyCacheService.IncrementUsageAsync(cachedDisease.Id);
            // Clean Vietnamese name to remove any XML/HTML tags
            diagnosis.ConditionName = TextHelper.CleanVietnameseName(cachedDisease.VietnameseName);

            // Load cached advice for this severity
            var severity = diagnosis.Severity ?? "Nhẹ";
            try
            {
                var adviceBySeverity = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                    cachedDisease.AdviceJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (adviceBySeverity != null && adviceBySeverity.TryGetValue(severity, out var cachedAdviceList))
                {
                    advice.AddRange(cachedAdviceList);
                    _logger.LogInformation(
                        "✅ Using cached advice for '{English}' (severity: {Severity})",
                        topDiseaseName,
                        severity);
                    return topDiseaseName; // Return English name, skip Groq call
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached advice, will generate new advice");
            }
        }
        else
        {
            // No cache, translate name
            diagnosis.ConditionName = await MapDiseaseNameToVietnamese(topDiseaseName);
        }

        return topDiseaseName; // Return English name for advice generation
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

    private async Task EnrichAdviceWithGroqAsync(SkinConditionDiagnosis diagnosis, List<string> advice, string englishName)
    {
        if (string.IsNullOrEmpty(diagnosis.ConditionName))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Generating general advice for: {DiseaseName} (English: {EnglishName})",
                diagnosis.ConditionName, englishName);

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

                // Save to cache: English name, Vietnamese name, and advice by severity
                var adviceBySeverity = new Dictionary<string, List<string>>
                {
                    [diagnosis.Severity ?? "Nhẹ"] = generatedAdvice
                };

                // Check if we already have this disease in cache
                var cachedDisease = await _dermatologyCacheService.FindByEnglishNameAsync(englishName);
                if (cachedDisease == null)
                {
                    // Save new entry (reasons can be added later if needed)
                    await _dermatologyCacheService.SaveDiseaseAsync(
                        englishName,
                        diagnosis.ConditionName,
                        adviceBySeverity,
                        reasons: null, // Can be added later
                        "GROQ");
                }
                else
                {
                    // Update existing entry with new severity advice
                    // Note: For simplicity, we're only saving one severity per disease
                    // In production, you might want to merge multiple severities
                    _logger.LogDebug("Disease already in cache, skipping save");
                }
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
            // Check database cache first
            var cachedDisease = await _dermatologyCacheService.FindByEnglishNameAsync(englishName);
            if (cachedDisease != null)
            {
                await _dermatologyCacheService.IncrementUsageAsync(cachedDisease.Id);
                _logger.LogInformation("✅ Using cached Vietnamese name for: {EnglishName}", englishName);
                return cachedDisease.VietnameseName;
            }

            // Check memory cache as fallback
            var cacheKey = $"{CACHE_KEY_PREFIX}{englishName.ToLower()}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedTranslation))
            {
                _logger.LogInformation("Using memory cached translation for: {DiseaseName}", englishName);
                return cachedTranslation!;
            }

            // Use Groq AI to translate disease name
            var vietnameseName = await TranslateDiseaseNameAsync(englishName);

            // Save to database cache (will be saved with advice later in EnrichAdviceWithGroqAsync)
            // For now, just cache the translation in memory
            _cache.Set(cacheKey, vietnameseName, TranslationCacheDuration);

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

Tên bệnh (tiếng Anh): {englishDiseaseName}

Yêu cầu:
- Nếu là tên bệnh chuyên môn, dùng thuật ngữ y khoa tiếng Việt chính xác
- Ưu tiên thuật ngữ y khoa tiếng Việt phổ biến

⚠️ BẮT BUỘC: Trả về CHỈ JSON format sau, KHÔNG có text khác, KHÔNG giải thích:
{{
  ""vietnameseName"": ""Tên bệnh bằng tiếng Việt""
}}";

            var response = await GenerateTextAsync(prompt, temperature: 0.1, cancellationToken);

            // Parse JSON response
            var translation = ParseVietnameseNameFromJson(response);

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

Yêu cầu:
- Viết bằng tiếng Việt, KHÔNG dùng tiếng Anh
- Mỗi lời khuyên chi tiết, cụ thể và dễ hiểu
- Phù hợp với mức độ nghiêm trọng {severity}
- Không đưa ra lời khuyên về thuốc cụ thể

⚠️ BẮT BUỘC: Trả về CHỈ JSON format sau, KHÔNG có text khác, KHÔNG giải thích:
{{
  ""advice"": [
    ""Lời khuyên 1"",
    ""Lời khuyên 2"",
    ""Lời khuyên 3""
  ]
}}";

            var response = await GenerateTextAsync(prompt, temperature: 0.3, cancellationToken);

            // Parse JSON response
            var advice = ParseAdviceFromJson(response);

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

    #region JSON Parsing Methods

    /// <summary>
    /// Parse Vietnamese name from JSON response
    /// </summary>
    private string ParseVietnameseNameFromJson(string response)
    {
        try
        {
            // Extract JSON from response
            var jsonText = ExtractJsonFromText(response);

            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            if (root.TryGetProperty("vietnameseName", out var nameProp))
            {
                var name = nameProp.GetString() ?? "";
                // Simple cleanup: remove XML/HTML tags if any
                return CleanXmlTags(name);
            }

            // Fallback: try to extract from text
            return CleanXmlTags(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response, using fallback");
            // Fallback: simple cleanup
            return CleanXmlTags(response);
        }
    }

    /// <summary>
    /// Parse advice from JSON response
    /// </summary>
    private string ParseAdviceFromJson(string response)
    {
        try
        {
            // Extract JSON from response
            var jsonText = ExtractJsonFromText(response);

            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            if (root.TryGetProperty("advice", out var adviceArray))
            {
                var adviceList = new List<string>();
                foreach (var item in adviceArray.EnumerateArray())
                {
                    var adviceItem = item.GetString();
                    if (!string.IsNullOrWhiteSpace(adviceItem))
                    {
                        adviceList.Add($"- {CleanXmlTags(adviceItem)}");
                    }
                }

                return string.Join("\n", adviceList);
            }

            // Fallback: try to extract from text
            return CleanXmlTags(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response, using fallback");
            // Fallback: simple cleanup
            return CleanXmlTags(response);
        }
    }

    /// <summary>
    /// Extract JSON from text (handles cases where Groq adds extra text)
    /// </summary>
    private string ExtractJsonFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Try to find JSON object in the text
        int startIndex = text.IndexOf('{');
        if (startIndex < 0)
            return text;

        int endIndex = text.LastIndexOf('}');
        if (endIndex <= startIndex)
            return text;

        return text.Substring(startIndex, endIndex - startIndex + 1);
    }

    /// <summary>
    /// Simple cleanup: remove XML/HTML tags only
    /// </summary>
    private string CleanXmlTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Remove XML/HTML tags
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"</?[^>]+>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));

        return cleaned.Trim();
    }

    #endregion

    #region Text Cleaning Methods (Legacy - kept for fallback)

    /// <summary>
    /// Clean Vietnamese translation - remove English text, explanations, reasoning, and XML/HTML tags
    /// </summary>
    private string CleanVietnameseTranslation(string translation)
    {
        if (string.IsNullOrWhiteSpace(translation))
            return translation;

        // Remove XML/HTML tags first (like </think>, <think>, etc.)
        translation = System.Text.RegularExpressions.Regex.Replace(
            translation,
            @"</?[^>]+>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));

        // Remove common unwanted prefixes/suffixes
        translation = translation
            .Replace("</think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("</think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<think>", "", StringComparison.OrdinalIgnoreCase);

        // Remove common English phrases that Groq might add
        var englishPhrases = new[]
        {
            "Okay, let's see",
            "First, I need to",
            "Let me think",
            "I remember that",
            "Looking further",
            "However",
            "But",
            "Since",
            "Therefore",
            "The answer should be",
            "The correct translation",
            "In Vietnamese",
            "In medical terminology"
        };

        var lines = translation.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Skip lines that start with English phrases
            var isEnglishLine = englishPhrases.Any(phrase =>
                trimmedLine.StartsWith(phrase, StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains(phrase, StringComparison.OrdinalIgnoreCase));

            if (isEnglishLine)
                continue;

            // Skip lines that are mostly English (more than 50% English words)
            var words = trimmedLine.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            var englishWordCount = words.Count(w => IsEnglishWord(w));
            if (words.Length > 0 && (double)englishWordCount / words.Length > 0.5)
                continue;

            cleanedLines.Add(trimmedLine);
        }

        var result = string.Join("\n", cleanedLines).Trim();

        // Remove quotes, extra whitespace, etc.
        result = result.Trim().Trim('"', '\'', '.', ',', ':', ';');

        // If result is empty or still contains too much English, try to extract Vietnamese part
        if (string.IsNullOrWhiteSpace(result) || IsMostlyEnglish(result))
        {
            var extractedVietnamese = TextHelper.ExtractVietnameseText(translation);
            if (!string.IsNullOrWhiteSpace(extractedVietnamese))
            {
                result = extractedVietnamese;
            }
        }

        return result;
    }


    /// <summary>
    /// Clean Vietnamese advice - remove English text and explanations
    /// </summary>
    private string CleanVietnameseAdvice(string advice)
    {
        if (string.IsNullOrWhiteSpace(advice))
            return advice;

        var lines = advice.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Skip lines that are mostly English
            if (IsMostlyEnglish(trimmedLine))
                continue;

            // Only keep lines that start with "-" (bullet points) or contain Vietnamese characters
            if (trimmedLine.StartsWith("-") || ContainsVietnameseCharacters(trimmedLine))
            {
                // Remove leading "-" if already present, we'll add it back
                var cleanLine = trimmedLine.TrimStart('-', '*', '•', ' ').Trim();
                if (!string.IsNullOrWhiteSpace(cleanLine) && !IsMostlyEnglish(cleanLine))
                {
                    cleanedLines.Add($"- {cleanLine}");
                }
            }
        }

        return string.Join("\n", cleanedLines);
    }

    /// <summary>
    /// Check if text is mostly English (more than 50% English words)
    /// </summary>
    private bool IsMostlyEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return false;

        var englishWordCount = words.Count(w => IsEnglishWord(w));
        return (double)englishWordCount / words.Length > 0.5;
    }

    /// <summary>
    /// Check if word is likely English (contains only ASCII letters, no Vietnamese diacritics)
    /// </summary>
    private bool IsEnglishWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        // Vietnamese characters are in range \u00C0-\u1EF9
        // If word contains Vietnamese characters, it's not English
        if (word.Any(c => c >= 0x00C0 && c <= 0x1EF9))
            return false;

        // If word is mostly ASCII letters, it might be English
        var asciiLetters = word.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        return asciiLetters > word.Length * 0.7;
    }

    /// <summary>
    /// Check if text contains Vietnamese characters
    /// </summary>
    private bool ContainsVietnameseCharacters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Vietnamese characters are in range \u00C0-\u1EF9
        return text.Any(c => c >= 0x00C0 && c <= 0x1EF9);
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
