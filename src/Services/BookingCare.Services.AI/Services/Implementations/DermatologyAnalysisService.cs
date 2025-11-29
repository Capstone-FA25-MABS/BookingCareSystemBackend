using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
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
    private readonly GeminiConfiguration _geminiConfig;
    private readonly IMemoryCache _cache;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly HospitalService.HospitalServiceClient _hospitalClient;
    private readonly IConversationSessionService _sessionService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IGeminiService _geminiService;

    private const int MAX_DOCTOR_RECOMMENDATIONS = 10;
    private const int MAX_HOSPITAL_RECOMMENDATIONS = 5;

    public DermatologyAnalysisService(
        ILogger<DermatologyAnalysisService> logger,
        HttpClient httpClient,
        IOptions<AILabToolsConfiguration> aiLabToolsConfig,
        IOptions<GeminiConfiguration> geminiConfig,
        IMemoryCache cache,
        DoctorService.DoctorServiceClient doctorClient,
        HospitalService.HospitalServiceClient hospitalClient,
        IConversationSessionService sessionService,
        IFileUploadService fileUploadService,
        IGeminiService geminiService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _aiLabToolsConfig = aiLabToolsConfig.Value;
        _geminiConfig = geminiConfig.Value;
        _cache = cache;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _sessionService = sessionService;
        _fileUploadService = fileUploadService;
        _geminiService = geminiService;

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
            var imageUrl = await UploadToS3Async(file, userId);
            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);

            // Step 3: Analyze with AILabTools API
            var aiLabToolsResult = await AnalyzeWithAILabToolsAsync(file);
            _logger.LogInformation("AILabTools analysis completed");

            // Step 4: Map to response model
            var response = MapToResponse(actualSessionId, imageUrl, aiLabToolsResult);

            // Step 5: Get doctor and hospital recommendations
            var dermatologySpecialty = new List<string> { "Da liễu", "Dermatology" };
            response.RecommendedDoctors = await GetDoctorRecommendationsAsync(dermatologySpecialty, location);
            response.RecommendedHospitals = await GetHospitalRecommendationsAsync(dermatologySpecialty, location);

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

    #region S3 Upload

    private async Task<string> UploadToS3Async(IFormFile file, Guid? userId)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var folder = $"uploads/ai/dermatology/{userId}/{timestamp}";

            using var stream = file.OpenReadStream();
            var uploadRequest = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = stream,
                ContentType = file.ContentType,
                Folder = folder,
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    { "user-id", userId?.ToString() ?? "anonymous" },
                    { "upload-timestamp", timestamp },
                    { "file-type", "dermatology" }
                }
            };

            var result = await _fileUploadService.UploadFileAsync(uploadRequest);

            if (result.Success)
            {
                _logger.LogInformation("File uploaded successfully to S3. CloudFront URL: {Url}", result.CloudFrontUrl);
                return result.CloudFrontUrl ?? result.FileUrl ?? string.Empty;
            }

            _logger.LogError("Failed to upload file to S3: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Failed to upload file to S3: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3");
            throw;
        }
    }

    #endregion

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
                    var adviceText = await _geminiService.GenerateGeneralAdviceAsync(
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
                    detailedConclusion = await _geminiService.GenerateDermatologyConclusionAsync(
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

    #region Doctor and Hospital Recommendations

    private async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(
        List<string> specialtyNames,
        LocationContext? location)
    {
        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialtyNames);
            if (specialtyIds.Count == 0)
            {
                _logger.LogWarning("No specialty IDs found for dermatology");
                return new List<DoctorRecommendation>();
            }

            var request = new FilterDoctorsForRecommendationRequest
            {
                MaxResults = MAX_DOCTOR_RECOMMENDATIONS * 2
            };
            request.SpecialtyIds.AddRange(specialtyIds.Select(id => id.ToString()));

            if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
            {
                request.ProvinceId = location.ProvinceId;
            }

            var response = await _doctorClient.FilterDoctorsForRecommendationAsync(request);

            return response.Doctors
                .Select(x => new { Doctor = x, Score = CalculateDoctorScore(x, location) })
                .OrderByDescending(x => x.Score)
                .Take(MAX_DOCTOR_RECOMMENDATIONS)
                .Select(x => new DoctorRecommendation
                {
                    Id = x.Doctor.Id,
                    Name = x.Doctor.FullName,
                    SpecialtyName = x.Doctor.SpecialtyName,
                    HospitalName = x.Doctor.HospitalName,
                    Rating = x.Doctor.Rating,
                    YearOfExperience = x.Doctor.YearsOfExperience,
                    ServiceTypeName = x.Doctor.ServiceTypeName,
                    Price = x.Doctor.ConsultationFee > 0 ? $"{x.Doctor.ConsultationFee:N0} VNĐ" : null,
                    AvatarUrl = x.Doctor.AvatarUrl,
                    RecommendationScore = x.Score
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor recommendations");
            return new List<DoctorRecommendation>();
        }
    }

    private async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(
        List<string> specialtyNames,
        LocationContext? location)
    {
        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialtyNames);
            if (specialtyIds.Count == 0) return new List<HospitalRecommendation>();

            var allHospitals = new List<HospitalReply>();

            foreach (var specialtyId in specialtyIds.Take(3))
            {
                try
                {
                    var request = new GetHospitalsBySpecialtyRequest
                    {
                        SpecialtyId = specialtyId.ToString()
                    };
                    var response = await _hospitalClient.GetHospitalsBySpecialtyAsync(request);
                    allHospitals.AddRange(response.Hospitals);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting hospitals for specialty {SpecialtyId}", specialtyId);
                }
            }

            return allHospitals
                .GroupBy(h => h.Id)
                .Select(g => g.First())
                .Select(h => new HospitalRecommendation
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    SpecialtyNames = specialtyNames,
                    ImageUrl = h.AvatarUrl,
                    RecommendationScore = CalculateHospitalScore(h, location)
                })
                .OrderByDescending(h => h.RecommendationScore)
                .Take(MAX_HOSPITAL_RECOMMENDATIONS)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospital recommendations");
            return new List<HospitalRecommendation>();
        }
    }

    private async Task<List<Guid>> MatchSpecialtiesToIdsAsync(List<string> specialtyNames)
    {
        var specialtyIds = new List<Guid>();

        try
        {
            var request = new GetAllSpecialtiesRequest();
            var response = await _doctorClient.GetAllSpecialtiesAsync(request);

            foreach (var specialtyName in specialtyNames)
            {
                var match = response.Specialties.FirstOrDefault(s =>
                    s.Name.Equals(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(specialtyName, StringComparison.OrdinalIgnoreCase) ||
                    specialtyName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    specialtyIds.Add(Guid.Parse(match.Id));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching specialties to IDs");
        }

        return specialtyIds;
    }

    private double CalculateDoctorScore(DoctorRecommendationInfo doctor, LocationContext? location)
    {
        double score = 0;
        if (location != null && !string.IsNullOrEmpty(location.ProvinceId)) score += 0.4;
        score += (doctor.Rating / 5.0) * 0.3;
        score += Math.Min(doctor.YearsOfExperience / 20.0, 1.0) * 0.1;
        return score;
    }

    private double CalculateHospitalScore(HospitalReply hospital, LocationContext? location)
    {
        double score = 0.5; // Base score
        // Can be enhanced with location matching, ratings, etc.
        return score;
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
            var vietnameseName = await _geminiService.TranslateDiseaseNameAsync(englishName);
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
