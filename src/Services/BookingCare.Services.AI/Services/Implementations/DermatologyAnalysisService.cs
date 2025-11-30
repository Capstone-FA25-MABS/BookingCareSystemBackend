using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
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
    private readonly GeminiApiHelper _geminiApiHelper;
    private readonly ServiceGeminiConfiguration _serviceConfig;
    private readonly IMemoryCache _cache;
    private readonly RecommendationHelper _recommendationHelper;
    private readonly FileUploadHelper _fileUploadHelper;

    private const string CACHE_KEY_PREFIX = "gemini_disease_translation_";
    private static readonly TimeSpan TranslationCacheDuration = TimeSpan.FromDays(30);
    private static readonly TimeSpan ConclusionCacheDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan AdviceCacheDuration = TimeSpan.FromDays(7);

    public DermatologyAnalysisService(
        ILogger<DermatologyAnalysisService> logger,
        HttpClient httpClient,
        IOptions<AILabToolsConfiguration> aiLabToolsConfig,
        IConversationSessionService sessionService,
        GeminiApiHelper geminiApiHelper,
        IOptions<GeminiServicesConfiguration> geminiServicesConfig,
        IMemoryCache cache,
        RecommendationHelper recommendationHelper,
        FileUploadHelper fileUploadHelper)
    {
        _logger = logger;
        _httpClient = httpClient;
        _aiLabToolsConfig = aiLabToolsConfig.Value;
        _sessionService = sessionService;
        _geminiApiHelper = geminiApiHelper;
        _serviceConfig = geminiServicesConfig.Value.DermatologyAnalysis;
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
            _logger.LogError(ex, "Error calling AILabTools API");
            throw;
        }
    }

    private async Task<AILabToolsAnalysisResult> ParseAILabToolsResponseAsync(string responseJson)
    {
        try
        {
            _logger.LogInformation("Parsing AILabTools response: {Response}", responseJson);

            var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            // Parse diagnosis information
            var diagnosis = new SkinConditionDiagnosis();
            var malignancyRisk = new MalignancyAssessment();
            var advice = new List<string>();
            var biopsyRecommended = false;
            var biopsyReason = string.Empty;

            // AILabTools actual response structure: { "error_code": 0, "data": { "results_english": { "disease_name": confidence, ... } } }
            // Check error_code (not "code")
            if (root.TryGetProperty("error_code", out var errorCode) && errorCode.GetInt32() == 0)
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("results_english", out var resultsEnglish))
                {
                    // results_english is an object with disease names as keys and confidence as values
                    // Find the disease with highest confidence
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

                    if (!string.IsNullOrEmpty(topDiseaseName))
                    {
                        // Map English disease name to Vietnamese
                        diagnosis.ConditionName = await MapDiseaseNameToVietnamese(topDiseaseName);
                        diagnosis.Confidence = topConfidence;

                        // Determine malignancy risk based on disease name and confidence
                        var diseaseNameLower = topDiseaseName.ToLower();

                        // High-risk conditions (melanoma, carcinoma, etc.)
                        if (diseaseNameLower.Contains("melanoma") ||
                            diseaseNameLower.Contains("carcinoma") ||
                            diseaseNameLower.Contains("cancer") ||
                            diseaseNameLower.Contains("malignant"))
                        {
                            malignancyRisk.SuspicionLevel = 0.8;
                            malignancyRisk.RiskCategory = "Cao";
                            malignancyRisk.UrgencyLevel = "URGENT";
                            malignancyRisk.RiskFactors.Add("Tổn thương nghi ngờ ác tính");
                            biopsyRecommended = true;
                            biopsyReason = "Phát hiện tổn thương nghi ngờ ác tính. Cần sinh thiết NGAY để xác định chính xác.";
                            advice.Add("Cần đến gặp bác sĩ da liễu NGAY để được thăm khám và sinh thiết");
                            diagnosis.Severity = "Nặng";
                        }
                        // Medium-risk conditions
                        else if (diseaseNameLower.Contains("keratosis") ||
                                 diseaseNameLower.Contains("nevus") ||
                                 diseaseNameLower.Contains("mole") ||
                                 diseaseNameLower.Contains("wart"))
                        {
                            malignancyRisk.SuspicionLevel = 0.5;
                            malignancyRisk.RiskCategory = "Trung bình";
                            malignancyRisk.UrgencyLevel = "NORMAL";
                            malignancyRisk.RiskFactors.Add("Tổn thương cần theo dõi");
                            biopsyRecommended = true;
                            biopsyReason = "Nên sinh thiết để loại trừ khả năng ác tính.";
                            advice.Add("Nên đến gặp bác sĩ da liễu trong vòng 1-2 tuần để được đánh giá");
                            diagnosis.Severity = "Trung bình";
                        }
                        // Low-risk conditions (fungal infections, dermatitis, etc.)
                        else
                        {
                            malignancyRisk.SuspicionLevel = 0.2;
                            malignancyRisk.RiskCategory = "Thấp";
                            malignancyRisk.UrgencyLevel = "NORMAL";
                            advice.Add("Theo dõi tổn thương da và đến gặp bác sĩ nếu có thay đổi");
                            diagnosis.Severity = "Nhẹ";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No disease predictions found in AILabTools response");
                        diagnosis.ConditionName = "Không xác định";
                        diagnosis.Confidence = 0;
                        diagnosis.Severity = "Unknown";
                    }
                }
                else
                {
                    _logger.LogWarning("No results_english found in AILabTools response data");
                    diagnosis.ConditionName = "Không xác định";
                    diagnosis.Confidence = 0;
                    diagnosis.Severity = "Unknown";
                }
            }
            else
            {
                // Extract error message from response
                var errorMessage = "Unknown error";
                if (root.TryGetProperty("error_detail", out var errorDetail))
                {
                    if (errorDetail.TryGetProperty("message", out var msg))
                    {
                        errorMessage = msg.GetString() ?? errorMessage;
                    }
                }

                var actualErrorCode = root.TryGetProperty("error_code", out var errorCodeProp)
                    ? errorCodeProp.GetInt32()
                    : -1;

                _logger.LogError("AILabTools API returned error. Code: {Code}, Message: {Message}, Full Response: {Response}",
                    actualErrorCode, errorMessage, responseJson);

                throw new InvalidOperationException($"AILabTools API returned error (code: {actualErrorCode}): {errorMessage}");
            }

            // Generate general advice using Gemini AI (specific to the disease)
            try
            {
                if (!string.IsNullOrEmpty(diagnosis.ConditionName))
                {
                    _logger.LogInformation("Generating general advice for: {DiseaseName}", diagnosis.ConditionName);
                    var adviceText = await GenerateGeneralAdviceAsync(
                        diagnosis.ConditionName,
                        diagnosis.Severity ?? "Nhẹ"
                    );

                    // Parse advice text into list (split by newlines or bullet points)
                    var generatedAdvice = adviceText
                        .Split('\n')
                        .Select(line => line.Trim().TrimStart('-', '*', '•').Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Distinct() // Remove duplicates
                        .Take(5) // Limit to 5 advice items
                        .ToList();

                    if (generatedAdvice.Count > 0)
                    {
                        advice.AddRange(generatedAdvice);
                        _logger.LogInformation("Added {Count} advice items for {DiseaseName}", generatedAdvice.Count, diagnosis.ConditionName);
                    }
                    else
                    {
                        _logger.LogWarning("No advice generated from Gemini, using fallback");
                        AddFallbackAdvice(advice, diagnosis.ConditionName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate general advice for {DiseaseName}, using fallback", diagnosis.ConditionName);
                AddFallbackAdvice(advice, diagnosis.ConditionName);
            }

            // Generate detailed conclusion using Gemini AI
            string? detailedConclusion = null;
            try
            {
                if (!string.IsNullOrEmpty(diagnosis.ConditionName) &&
                    !string.IsNullOrEmpty(diagnosis.Severity) &&
                    !string.IsNullOrEmpty(malignancyRisk.RiskCategory))
                {
                    _logger.LogInformation("Generating detailed conclusion for: {DiseaseName}", diagnosis.ConditionName);
                    detailedConclusion = await GenerateDermatologyConclusionAsync(
                        diagnosis.ConditionName,
                        diagnosis.Confidence,
                        diagnosis.Severity,
                        malignancyRisk.RiskCategory
                    );

                    _logger.LogInformation("Successfully generated detailed conclusion. Length: {Length} characters",
                        detailedConclusion?.Length ?? 0);
                }
                else
                {
                    _logger.LogWarning("Cannot generate detailed conclusion: Missing diagnosis info. ConditionName={ConditionName}, Severity={Severity}, RiskCategory={RiskCategory}",
                        diagnosis.ConditionName, diagnosis.Severity, malignancyRisk.RiskCategory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate detailed conclusion for {DiseaseName}. Will use fallback.", diagnosis.ConditionName);

                // Use fallback conclusion
                detailedConclusion = $@"## Thông tin về {diagnosis.ConditionName}

Dựa trên phân tích hình ảnh, tổn thương da có khả năng là **{diagnosis.ConditionName}** với độ tin cậy **{(diagnosis.Confidence * 100):F0}%**.

### Mức độ nghiêm trọng
Tổn thương được đánh giá ở mức độ **{diagnosis.Severity}** với nguy cơ ác tính **{malignancyRisk.RiskCategory}**.

### Khuyến nghị
Để có chẩn đoán chính xác và phương pháp điều trị phù hợp, bạn cần đến gặp bác sĩ da liễu để được:
- Khám lâm sàng chi tiết
- Đánh giá toàn diện tình trạng da
- Chỉ định các xét nghiệm cần thiết (nếu cần)
- Đưa ra phương án điều trị phù hợp

**Lưu ý:** Đây chỉ là kết quả phân tích sơ bộ từ hình ảnh. Vui lòng không tự ý điều trị mà hãy tìm đến các cơ sở y tế uy tín để được tư vấn và điều trị đúng cách.";
            }

            return new AILabToolsAnalysisResult
            {
                Diagnosis = diagnosis,
                MalignancyRisk = malignancyRisk,
                GeneralAdvice = advice,
                BiopsyRecommended = biopsyRecommended,
                BiopsyReason = biopsyReason,
                DetailedConclusion = detailedConclusion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AILabTools response");
            throw new InvalidOperationException("Failed to parse AILabTools API response", ex);
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
            MalignancyRisk = aiLabToolsResult.MalignancyRisk,
            GeneralAdvice = aiLabToolsResult.GeneralAdvice,
            BiopsyRecommended = aiLabToolsResult.BiopsyRecommended,
            BiopsyReason = aiLabToolsResult.BiopsyReason,
            DetailedConclusion = aiLabToolsResult.DetailedConclusion,
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
                hospitals = response.RecommendedHospitals
            };

            var disease = response.Diagnosis != null ? new
            {
                Name = response.Diagnosis.ConditionName,
                Confidence = response.Diagnosis.Confidence,
                Severity = response.Diagnosis.Severity,
                MalignancyRisk = response.MalignancyRisk?.RiskCategory,
                BiopsyRecommended = response.BiopsyRecommended
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

            if (!string.IsNullOrEmpty(response.Diagnosis.Severity))
            {
                messageBuilder.AppendLine($"**Mức độ nghiêm trọng:** {response.Diagnosis.Severity}");
            }
            messageBuilder.AppendLine();
        }

        // Don't show malignancy risk for low-risk conditions
        // if (response.MalignancyRisk != null)
        // {
        //     messageBuilder.AppendLine($"**Đánh giá nguy cơ ác tính:** {response.MalignancyRisk.RiskCategory}");
        //     messageBuilder.AppendLine($"**Mức độ nghi ngờ:** {response.MalignancyRisk.SuspicionLevel:P0}");
        //     messageBuilder.AppendLine();
        // }

        if (response.BiopsyRecommended)
        {
            messageBuilder.AppendLine($"**Khuyến nghị sinh thiết:** Có");
            if (!string.IsNullOrEmpty(response.BiopsyReason))
            {
                messageBuilder.AppendLine($"**Lý do:** {response.BiopsyReason}");
            }
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

        if (!string.IsNullOrEmpty(response.DetailedConclusion))
        {
            messageBuilder.AppendLine("---");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("**KẾT LUẬN CHI TIẾT:**");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(response.DetailedConclusion);
            messageBuilder.AppendLine();
        }

        messageBuilder.AppendLine(response.Disclaimer);

        return messageBuilder.ToString();
    }

    #endregion

    #region Helper Methods

    private void AddFallbackAdvice(List<string> advice, string? diseaseName)
    {
        // Generic fallback advice
        advice.Add("Giữ vệ sinh da sạch sẽ");
        advice.Add("Tránh gãi hoặc chạm vào vùng da bị tổn thương");
        advice.Add("Đến gặp bác sĩ da liễu để được thăm khám và điều trị đúng cách");
    }

    private async Task<string> MapDiseaseNameToVietnamese(string englishName)
    {
        try
        {
            // Use Gemini AI to translate disease name
            var vietnameseName = await TranslateDiseaseNameAsync(englishName);
            return vietnameseName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to translate disease name using Gemini AI: {DiseaseName}", englishName);

            // Fallback: return the English name with proper capitalization
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                englishName.ToLower().Replace("_", " ")
            );
        }
    }

    #region Gemini AI Helper Methods

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
    /// Generate detailed medical conclusion for dermatology diagnosis (500-800 words)
    /// </summary>
    private async Task<string> GenerateDermatologyConclusionAsync(
        string diseaseName,
        double confidence,
        string severity,
        string riskCategory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"gemini_conclusion_{diseaseName.ToLower()}_{severity}_{riskCategory}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedConclusion))
            {
                _logger.LogInformation("Using cached conclusion for: {DiseaseName}", diseaseName);
                return cachedConclusion!;
            }

            _logger.LogInformation("Generating detailed conclusion for: {DiseaseName}", diseaseName);

            var confidencePercent = (confidence * 100).ToString("F0");

            var prompt = $@"Bạn là một bác sĩ da liễu chuyên nghiệp. Hãy viết một kết luận y khoa chi tiết về bệnh da liễu sau đây.

THÔNG TIN CHẨN ĐOÁN:
- Tên bệnh: {diseaseName}
- Độ tin cậy: {confidencePercent}%
- Mức độ nghiêm trọng: {severity}
- Nguy cơ ác tính: {riskCategory}

YÊU CẦU:
1. Viết bằng tiếng Việt, dùng thuật ngữ y khoa chính xác nhưng dễ hiểu
2. Độ dài: 500-800 từ
3. Chia thành các sections sau (dùng markdown headers):

## Mô tả bệnh
- Giải thích bệnh là gì
- Đặc điểm nhận dạng trên da
- Tần suất gặp

## Nguyên nhân
- Các nguyên nhân chính gây bệnh
- Yếu tố nguy cơ
- Cơ chế bệnh sinh (nếu có)

## Triệu chứng
- Các triệu chứng điển hình
- Dấu hiệu cần chú ý
- Biến chứng có thể xảy ra

## Điều trị
- Phương pháp điều trị chính
- Thuốc thường dùng (nếu có)
- Thời gian điều trị dự kiến
- Lưu ý khi điều trị

## Tiên lượng
- Khả năng khỏi bệnh
- Nguy cơ tái phát
- Các biện pháp phòng ngừa

LƯU Ý:
- Không đưa ra chẩn đoán chắc chắn, chỉ cung cấp thông tin tham khảo
- Nhấn mạnh cần đến gặp bác sĩ da liễu để được thăm khám trực tiếp
- Viết theo phong cách chuyên nghiệp nhưng dễ hiểu cho người bệnh
- Không dùng bullet points quá nhiều, ưu tiên viết thành đoạn văn

Hãy viết kết luận chi tiết:";

            var conclusion = await GenerateTextAsync(prompt, temperature: 0.4, cancellationToken);

            // Cache the conclusion for 7 days
            _cache.Set(cacheKey, conclusion, ConclusionCacheDuration);

            _logger.LogInformation("Generated conclusion for '{DiseaseName}': Length={Length} characters",
                diseaseName, conclusion.Length);

            return conclusion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dermatology conclusion for: {DiseaseName}", diseaseName);

            // Fallback: return a basic conclusion
            return $@"## Thông tin về {diseaseName}

Dựa trên phân tích hình ảnh, tổn thương da có khả năng là {diseaseName} với độ tin cậy {(confidence * 100):F0}%.

**Lưu ý quan trọng:** Đây chỉ là kết quả phân tích sơ bộ từ hình ảnh. Để có chẩn đoán chính xác và phương pháp điều trị phù hợp, bạn cần đến gặp bác sĩ da liễu để được thăm khám trực tiếp.

Bác sĩ sẽ:
- Khám lâm sàng chi tiết
- Đánh giá toàn diện tình trạng da
- Có thể chỉ định các xét nghiệm cần thiết
- Đưa ra phương án điều trị phù hợp với tình trạng cụ thể của bạn

Vui lòng không tự ý điều trị mà hãy tìm đến các cơ sở y tế uy tín để được tư vấn và điều trị đúng cách.";
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
            var cacheKey = $"gemini_advice_{diseaseName.ToLower()}_{severity}";
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
            return @"- Theo dõi tổn thương da và đến gặp bác sĩ nếu có thay đổi
- Giữ vệ sinh da sạch sẽ
- Tránh tiếp xúc trực tiếp với ánh nắng mặt trời";
        }
    }

    /// <summary>
    /// Generate text using Gemini AI with custom prompt
    /// </summary>
    private async Task<string> GenerateTextAsync(
        string prompt,
        double temperature = 0.3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _geminiApiHelper.CallGeminiApiAsync(
                prompt,
                _serviceConfig,
                temperature: temperature,
                maxOutputTokens: null, // Use default from common config
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    #endregion

    #endregion

    #region Helper Classes

    private class AILabToolsAnalysisResult
    {
        public SkinConditionDiagnosis Diagnosis { get; set; } = new();
        public MalignancyAssessment MalignancyRisk { get; set; } = new();
        public List<string> GeneralAdvice { get; set; } = new();
        public bool BiopsyRecommended { get; set; }
        public string? BiopsyReason { get; set; }
        public string? DetailedConclusion { get; set; }
    }

    #endregion
}
