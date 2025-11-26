using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

public class LabResultAnalysisService : ILabResultAnalysisService
{
    private readonly ILogger<LabResultAnalysisService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _geminiConfig;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    private readonly IConversationSessionService _sessionService;
    private readonly string _tesseractDataPath;
    private readonly string _tesseractLanguage;

    private const int MAX_DOCTOR_RECOMMENDATIONS = 10;
    private const int MAX_HOSPITAL_RECOMMENDATIONS = 5;

    public LabResultAnalysisService(
        ILogger<LabResultAnalysisService> logger,
        HttpClient httpClient,
        IOptions<GeminiConfiguration> geminiConfig,
        DoctorService.DoctorServiceClient doctorClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient,
        IConversationSessionService sessionService,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _geminiConfig = geminiConfig.Value;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _sessionService = sessionService;
        _tesseractDataPath = configuration["Tesseract:DataPath"] ?? "tessdata";
        _tesseractLanguage = configuration["Tesseract:Language"] ?? "vie+eng";
    }

    public async Task<LabResultAnalysisResponse> AnalyzeLabResultAsync(
        IFormFile file,
        LocationContext? location,
        Guid? userId,
        Guid? sessionId)
    {
        try
        {
            _logger.LogInformation("Starting lab result analysis for user {UserId}", userId);

            var actualSessionId = await _sessionService.GetOrCreateSessionAsync(sessionId, userId ?? Guid.Empty, location);
            _logger.LogInformation("Using session {SessionId}", actualSessionId);

            var imageUrl = await UploadToS3Async(file, userId);
            _logger.LogInformation("Uploaded image to {ImageUrl}", imageUrl);

            var extractedText = await ExtractTextFromImageAsync(file);
            _logger.LogInformation("Extracted {Length} characters from image", extractedText.Length);

            var aiAnalysis = await AnalyzeWithGeminiAsync(extractedText);
            _logger.LogInformation("Gemini analysis completed");

            var doctors = await GetDoctorRecommendationsAsync(aiAnalysis.Specialties, location);
            var hospitals = await GetHospitalRecommendationsAsync(aiAnalysis.Specialties, location);

            var response = new LabResultAnalysisResponse
            {
                SessionId = actualSessionId,
                ImageUrl = imageUrl,
                ExtractedText = extractedText,
                NormalIndicators = aiAnalysis.NormalIndicators,
                AbnormalIndicators = aiAnalysis.AbnormalIndicators,
                RecommendedDoctors = doctors,
                RecommendedHospitals = hospitals,
                Disclaimer = "Lưu ý: Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.",
                Timestamp = DateTime.UtcNow
            };

            await SaveLabResultAnalysisAsync(actualSessionId, userId, file.FileName, imageUrl, response, location);

            _logger.LogInformation("Lab result analysis completed and saved to session {SessionId}", actualSessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing lab result: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze lab result", ex);
        }
    }

    private async Task<string> UploadToS3Async(IFormFile file, Guid? userId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"lab-results/{userId}/{timestamp}/{file.FileName}";
        
        _logger.LogWarning("S3 upload not implemented yet. Using placeholder URL");
        return $"https://s3.amazonaws.com/booking-care/{fileName}";
    }

    private async Task<string> ExtractTextFromImageAsync(IFormFile file)
    {
        try
        {
            _logger.LogWarning("Tesseract OCR not implemented yet. Using placeholder text.");
            
            await Task.Delay(100);
            
            return @"
KẾT QUẢ XÉT NGHIỆM MÁU
Bệnh viện Đa khoa Trung ương

Họ tên: Nguyễn Văn A
Ngày xét nghiệm: 25/11/2025

CHỈ SỐ HUYẾT HỌC:
- Hồng cầu (RBC): 4.5 x10^12/L (Bình thường: 4.0-5.5)
- Bạch cầu (WBC): 12.5 x10^9/L (Bình thường: 4.0-10.0) *
- Hemoglobin (Hb): 140 g/L (Bình thường: 130-170)
- Tiểu cầu: 250 x10^9/L (Bình thường: 150-400)

CHỈ SỐ SINH HÓA:
- Glucose: 6.8 mmol/L (Bình thường: 3.9-6.1) *
- Cholesterol: 5.2 mmol/L (Bình thường: <5.2)
- Triglyceride: 1.8 mmol/L (Bình thường: <1.7) *

* Chỉ số bất thường
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to extract text from image", ex);
        }
    }

    private async Task<GeminiLabAnalysis> AnalyzeWithGeminiAsync(string extractedText)
    {
        var prompt = BuildAnalysisPrompt(extractedText);
        var geminiResponse = await CallGeminiApiAsync(prompt);
        return ParseGeminiResponse(geminiResponse);
    }

    private string BuildAnalysisPrompt(string extractedText)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("Bạn là bác sĩ AI chuyên phân tích kết quả xét nghiệm. Phân tích kết quả sau:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**KẾT QUẢ XÉT NGHIỆM:**");
        promptBuilder.AppendLine(extractedText);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU:**");
        promptBuilder.AppendLine("1. Phân loại các chỉ số thành bình thường và bất thường");
        promptBuilder.AppendLine("2. Với mỗi chỉ số bất thường:");
        promptBuilder.AppendLine("   - Giải thích ngắn gọn tại sao bất thường");
        promptBuilder.AppendLine("   - Đưa ra lời khuyên");
        promptBuilder.AppendLine("   - Chẩn đoán bệnh có thể");
        promptBuilder.AppendLine("   - **BẮT BUỘC**: Đề xuất 1-2 chuyên khoa phù hợp");
        promptBuilder.AppendLine("3. Đề xuất 1-3 chuyên khoa tổng quát");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON:**");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"normalIndicators\": [{\"name\": \"Hemoglobin\", \"value\": \"140\", \"unit\": \"g/L\", \"referenceRange\": \"130-170\"}],");
        promptBuilder.AppendLine("  \"abnormalIndicators\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"name\": \"Bạch cầu (WBC)\",");
        promptBuilder.AppendLine("      \"value\": \"12.5\",");
        promptBuilder.AppendLine("      \"unit\": \"x10^9/L\",");
        promptBuilder.AppendLine("      \"referenceRange\": \"4.0-10.0\",");
        promptBuilder.AppendLine("      \"explanation\": \"Số lượng bạch cầu cao...\",");
        promptBuilder.AppendLine("      \"advice\": \"Cần theo dõi triệu chứng...\",");
        promptBuilder.AppendLine("      \"possibleDiagnosis\": \"Nhiễm trùng\",");
        promptBuilder.AppendLine("      \"recommendedSpecialties\": [\"Nội tổng quát\", \"Huyết học\"]");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ],");
        promptBuilder.AppendLine("  \"specialties\": [\"Nội tổng quát\", \"Nội tiết\"]");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_geminiConfig.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }

        var modelsToTry = new[] { "gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-pro", "gemini-1.5-flash" };
        var apiVersions = new[] { "v1beta", "v1" };
        Exception? lastException = null;

        foreach (var apiVersion in apiVersions)
        {
            foreach (var model in modelsToTry)
            {
                try
                {
                    var url = $"{_geminiConfig.ApiEndpoint}/{apiVersion}/models/{model}:generateContent?key={_geminiConfig.ApiKey}";
                    
                    var requestBody = new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } },
                        generationConfig = new { temperature = 0.3, maxOutputTokens = 8192, topP = 0.95, topK = 40 }
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, jsonContent);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonDocument.Parse(responseBody);
                    
                    var text = jsonResponse.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    _logger.LogInformation("Successfully called Gemini API with {Model}", model);
                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed with {ApiVersion}/{Model}", apiVersion, model);
                    lastException = ex;
                }
            }
        }

        throw new InvalidOperationException($"Failed to call Gemini API. Last error: {lastException?.Message}", lastException);
    }

    private GeminiLabAnalysis ParseGeminiResponse(string geminiResponse)
    {
        try
        {
            var jsonStart = geminiResponse.IndexOf('{');
            var jsonEnd = geminiResponse.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                throw new InvalidOperationException("No JSON found in Gemini response");
            }

            var jsonText = geminiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var analysis = new GeminiLabAnalysis
            {
                NormalIndicators = new List<LabIndicator>(),
                AbnormalIndicators = new List<AbnormalLabIndicator>(),
                Specialties = new List<string>()
            };

            if (root.TryGetProperty("normalIndicators", out var normalArray))
            {
                foreach (var item in normalArray.EnumerateArray())
                {
                    analysis.NormalIndicators.Add(new LabIndicator
                    {
                        Name = item.GetProperty("name").GetString() ?? "",
                        Value = item.GetProperty("value").GetString() ?? "",
                        Unit = item.GetProperty("unit").GetString() ?? "",
                        ReferenceRange = item.TryGetProperty("referenceRange", out var refRange) ? refRange.GetString() ?? "" : ""
                    });
                }
            }

            if (root.TryGetProperty("abnormalIndicators", out var abnormalArray))
            {
                foreach (var item in abnormalArray.EnumerateArray())
                {
                    var specialtyNames = new List<string>();
                    if (item.TryGetProperty("recommendedSpecialties", out var specArray))
                    {
                        foreach (var spec in specArray.EnumerateArray())
                        {
                            var specialty = spec.GetString();
                            if (!string.IsNullOrEmpty(specialty))
                            {
                                specialtyNames.Add(specialty);
                            }
                        }
                    }

                    var diagnosis = item.TryGetProperty("possibleDiagnosis", out var diagProp) ? diagProp.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";

                    var specialtyMatches = specialtyNames.Count > 0 
                        ? ConvertToSpecialtyMatches(specialtyNames, diagnosis, name)
                        : InferSpecialtiesFromDiagnosis(diagnosis, name);

                    analysis.AbnormalIndicators.Add(new AbnormalLabIndicator
                    {
                        Name = item.GetProperty("name").GetString() ?? "",
                        Value = item.GetProperty("value").GetString() ?? "",
                        Unit = item.GetProperty("unit").GetString() ?? "",
                        ReferenceRange = item.TryGetProperty("referenceRange", out var refRange) ? refRange.GetString() ?? "" : "",
                        Explanation = item.GetProperty("explanation").GetString() ?? "",
                        Advice = item.GetProperty("advice").GetString() ?? "",
                        PossibleDiagnosis = diagnosis,
                        RecommendedSpecialties = specialtyMatches
                    });
                }
            }

            if (root.TryGetProperty("specialties", out var specialtiesArray))
            {
                foreach (var item in specialtiesArray.EnumerateArray())
                {
                    var specialty = item.GetString();
                    if (!string.IsNullOrEmpty(specialty))
                    {
                        analysis.Specialties.Add(specialty);
                    }
                }
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response");
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    private List<SpecialtyMatch> ConvertToSpecialtyMatches(List<string> specialtyNames, string diagnosis, string indicatorName)
    {
        var matches = new List<SpecialtyMatch>();
        var combinedText = $"{diagnosis} {indicatorName}".ToLower();

        foreach (var specialtyName in specialtyNames)
        {
            var confidence = 0.8;
            var urgency = "NORMAL";
            var reasons = new List<string> { $"Phù hợp với chẩn đoán: {diagnosis}" };

            if (combinedText.Contains("nhiễm trùng") || combinedText.Contains("viêm"))
            {
                urgency = "URGENT";
                confidence = 0.9;
            }

            matches.Add(new SpecialtyMatch
            {
                SpecialtyName = specialtyName,
                Confidence = confidence,
                Urgency = urgency,
                Reasons = reasons
            });
        }

        return matches;
    }

    private List<SpecialtyMatch> InferSpecialtiesFromDiagnosis(string diagnosis, string indicatorName)
    {
        var specialties = new List<SpecialtyMatch>();
        var combinedText = $"{diagnosis} {indicatorName}".ToLower();

        if (combinedText.Contains("đái tháo đường") || combinedText.Contains("glucose") || combinedText.Contains("đường huyết"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tiết", Confidence = 0.9, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn chuyển hóa đường", "Cần kiểm tra HbA1c" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.7, Urgency = "NORMAL", Reasons = new List<string> { "Đánh giá tổng quát sức khỏe" } });
        }
        else if (combinedText.Contains("nhiễm trùng") || combinedText.Contains("bạch cầu") || combinedText.Contains("wbc") || combinedText.Contains("viêm"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.85, Urgency = "URGENT", Reasons = new List<string> { "Dấu hiệu nhiễm trùng", "Cần xác định nguyên nhân" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Huyết học", Confidence = 0.7, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn bạch cầu" } });
        }
        else if (combinedText.Contains("cholesterol") || combinedText.Contains("triglyceride") || combinedText.Contains("lipid"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Tim mạch", Confidence = 0.9, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn lipid máu", "Nguy cơ tim mạch" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.75, Urgency = "NORMAL", Reasons = new List<string> { "Quản lý yếu tố nguy cơ" } });
        }
        else if (combinedText.Contains("gan") || combinedText.Contains("alt") || combinedText.Contains("ast"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Tiêu hóa", Confidence = 0.85, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn chức năng gan" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.7, Urgency = "NORMAL", Reasons = new List<string> { "Đánh giá tổng quát" } });
        }
        else if (combinedText.Contains("thận") || combinedText.Contains("creatinine") || combinedText.Contains("ure"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Thận - Tiết niệu", Confidence = 0.9, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn chức năng thận" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.7, Urgency = "NORMAL", Reasons = new List<string> { "Quản lý bệnh thận" } });
        }
        else if (combinedText.Contains("hồng cầu") || combinedText.Contains("hemoglobin") || combinedText.Contains("thiếu máu"))
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Huyết học", Confidence = 0.9, Urgency = "NORMAL", Reasons = new List<string> { "Rối loạn hồng cầu/hemoglobin" } });
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.7, Urgency = "NORMAL", Reasons = new List<string> { "Đánh giá nguyên nhân thiếu máu" } });
        }
        else
        {
            specialties.Add(new SpecialtyMatch { SpecialtyName = "Nội tổng quát", Confidence = 0.6, Urgency = "NORMAL", Reasons = new List<string> { "Đánh giá tổng quát" } });
        }

        return specialties;
    }

    private async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(List<string> specialties, LocationContext? location)
    {
        if (specialties.Count == 0) return new List<DoctorRecommendation>();

        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialties);
            if (specialtyIds.Count == 0) return new List<DoctorRecommendation>();

            var request = new FilterDoctorsForRecommendationRequest { MaxResults = MAX_DOCTOR_RECOMMENDATIONS * 2 };
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

    private async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(List<string> specialties, LocationContext? location)
    {
        if (specialties.Count == 0) return new List<HospitalRecommendation>();

        try
        {
            var specialtyIds = await MatchSpecialtiesToIdsAsync(specialties);
            if (specialtyIds.Count == 0) return new List<HospitalRecommendation>();

            var allHospitals = new List<HospitalReply>();
            
            foreach (var specialtyId in specialtyIds.Take(3))
            {
                try
                {
                    var request = new GetHospitalsBySpecialtyRequest { SpecialtyId = specialtyId.ToString() };
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
                    SpecialtyNames = specialties,
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
        double score = 0.5;
        if (location != null && !string.IsNullOrEmpty(location.DisplayName))
        {
            var address = hospital.Address?.ToLowerInvariant() ?? "";
            var locationName = location.DisplayName.ToLowerInvariant();
            if (address.Contains(locationName)) score += 0.5;
        }
        return score;
    }

    private async Task SaveLabResultAnalysisAsync(Guid sessionId, Guid? userId, string fileName, string imageUrl, LabResultAnalysisResponse response, LocationContext? location)
    {
        try
        {
            var userMessage = $"Đã gửi file xét nghiệm: {fileName}";
            var aiMessage = new StringBuilder();
            aiMessage.AppendLine("**KẾT QUẢ PHÂN TÍCH XÉT NGHIỆM:**");
            aiMessage.AppendLine();

            if (response.NormalIndicators.Count > 0)
            {
                aiMessage.AppendLine("**Các chỉ số bình thường:**");
                foreach (var indicator in response.NormalIndicators)
                {
                    aiMessage.AppendLine($"- {indicator.Name}: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
                }
                aiMessage.AppendLine();
            }

            if (response.AbnormalIndicators.Count > 0)
            {
                aiMessage.AppendLine("**Các chỉ số bất thường:**");
                foreach (var indicator in response.AbnormalIndicators)
                {
                    aiMessage.AppendLine($"- **{indicator.Name}**: {indicator.Value} {indicator.Unit} (Tham chiếu: {indicator.ReferenceRange})");
                    aiMessage.AppendLine($"  - Giải thích: {indicator.Explanation}");
                    aiMessage.AppendLine($"  - Lời khuyên: {indicator.Advice}");
                    if (!string.IsNullOrEmpty(indicator.PossibleDiagnosis))
                    {
                        aiMessage.AppendLine($"  - Chẩn đoán có thể: {indicator.PossibleDiagnosis}");
                        if (indicator.RecommendedSpecialties != null && indicator.RecommendedSpecialties.Count > 0)
                        {
                            var specialtyNames = string.Join(", ", indicator.RecommendedSpecialties.Select(s => s.SpecialtyName));
                            aiMessage.AppendLine($"    - Chuyên khoa phù hợp: {specialtyNames}");
                        }
                    }
                    aiMessage.AppendLine();
                }
            }

            aiMessage.AppendLine(response.Disclaimer);

            var suggestions = new { doctors = response.RecommendedDoctors, hospitals = response.RecommendedHospitals };

            await _sessionService.SaveConversationHistoryAsync(sessionId, userMessage, aiMessage.ToString(), location, suggestions, userId, disease: null, questionCount: 0, analysisComplete: true);

            _logger.LogInformation("Saved lab result analysis to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving lab result analysis for session {SessionId}", sessionId);
        }
    }
}

internal class GeminiLabAnalysis
{
    public List<LabIndicator> NormalIndicators { get; set; } = new();
    public List<AbnormalLabIndicator> AbnormalIndicators { get; set; } = new();
    public List<string> Specialties { get; set; } = new();
}
