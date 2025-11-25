using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service implementation for symptom analysis with 3-question workflow
/// </summary>
public class SymptomAnalysisService : ISymptomAnalysisService
{
    private readonly IConversationSessionService _sessionService;
    private readonly ILogger<SymptomAnalysisService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _geminiConfig;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    
    private const int MAX_QUESTIONS = 3;
    private const int MAX_DOCTOR_RECOMMENDATIONS = 10;
    private const int MAX_HOSPITAL_RECOMMENDATIONS = 5;
    private const string SafetyThreshold = "BLOCK_MEDIUM_AND_ABOVE";

    public SymptomAnalysisService(
        IConversationSessionService sessionService,
        ILogger<SymptomAnalysisService> logger,
        HttpClient httpClient,
        IOptions<GeminiConfiguration> geminiConfig,
        DoctorService.DoctorServiceClient doctorClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient)
    {
        _sessionService = sessionService;
        _logger = logger;
        _httpClient = httpClient;
        _geminiConfig = geminiConfig.Value;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
    }

    public async Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Starting symptom analysis for user {UserId}, session {SessionId}", 
                request.UserId, request.SessionId);

            // Step 1: Get or create session in database
            var sessionId = await _sessionService.GetOrCreateSessionAsync(
                request.SessionId, 
                request.UserId ?? Guid.Empty, 
                request.Location);
            
            var conversationHistory = request.ConversationHistory ?? new List<ConversationMessage>();

            // Step 2: Count how many questions AI has asked so far
            int questionCount = CountAIQuestions(conversationHistory);
            _logger.LogInformation("Question count: {QuestionCount}/{MaxQuestions}", questionCount, MAX_QUESTIONS);

            // Step 3: Determine mode (asking or conclusion)
            bool isAskingMode = questionCount < MAX_QUESTIONS;

            // Step 4: Build prompt for Gemini
            string prompt = isAskingMode 
                ? BuildAskingModePrompt(request.Message, conversationHistory)
                : BuildConclusionModePrompt(request.Message, conversationHistory);

            // Step 5: Call Gemini API
            string geminiResponse = await CallGeminiApiAsync(prompt);
            _logger.LogDebug("Gemini response: {Response}", geminiResponse);

            // Step 6: Parse response
            SymptomAnalysisResponse response;
            if (isAskingMode)
            {
                response = ParseAskingModeResponse(geminiResponse, sessionId, questionCount);
            }
            else
            {
                response = await ParseConclusionModeResponse(geminiResponse, sessionId, questionCount, request.Location);
            }

            // Step 7: Save conversation to database
            object? suggestions = null;
            object? disease = null;

            if (response.AnalysisComplete && response.Disease != null)
            {
                disease = new
                {
                    Name = response.Disease.Name,
                    Confidence = response.Disease.Confidence,
                    Reasons = response.Disease.Reasons
                };

                if (response.RecommendedDoctors?.Count > 0 || response.RecommendedHospitals?.Count > 0)
                {
                    suggestions = new
                    {
                        doctors = response.RecommendedDoctors,
                        hospitals = response.RecommendedHospitals
                    };
                }
            }

            await _sessionService.SaveConversationHistoryAsync(
                sessionId: sessionId,
                userMessage: request.Message,
                aiMessage: response.Message,
                location: request.Location,
                suggestions: suggestions,
                userId: request.UserId,
                disease: disease,
                questionCount: response.QuestionCount,
                analysisComplete: response.AnalysisComplete
            );

            _logger.LogInformation("Symptom analysis completed successfully for session {SessionId}", sessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze symptoms", ex);
        }
    }

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(Guid sessionId)
    {
        return await _sessionService.LoadConversationHistoryAsync(sessionId);
    }

    public async Task<List<SessionSummary>> GetUserSessionsAsync(Guid userId)
    {
        var entities = await _sessionService.GetUserSessionsAsync(userId);
        return entities.Select(e => new SessionSummary
        {
            SessionId = e.Id, // SessionSummaryEntity uses Id, not SessionId
            UserId = e.UserId,
            Title = e.Title,
            LastMessage = e.LastMessage,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            MessageCount = e.MessageCount
        }).ToList();
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId)
    {
        return await _sessionService.DeleteSessionAsync(sessionId, userId);
    }

    #region Private Helper Methods

    /// <summary>
    /// Count how many questions AI has asked (count all AI messages in history)
    /// </summary>
    private int CountAIQuestions(List<ConversationMessage> history)
    {
        // Count all AI messages (frontend doesn't send suggestions in history)
        return history.Count(m => 
            m.Role.Equals("ai", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Build prompt for asking mode (< 3 questions)
    /// </summary>
    private string BuildAskingModePrompt(string userMessage, List<ConversationMessage> history)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("Bạn là bác sĩ AI chuyên nghiệp. Nhiệm vụ của bạn là hỏi 1 câu hỏi để làm rõ triệu chứng của bệnh nhân.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**LỊCH SỬ HỘI THOẠI:**");
        
        foreach (var msg in history)
        {
            promptBuilder.AppendLine($"{msg.Role.ToUpper()}: {msg.Content}");
        }
        
        promptBuilder.AppendLine($"USER: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU:**");
        promptBuilder.AppendLine("1. Phân tích triệu chứng đã có");
        promptBuilder.AppendLine("2. Đưa ra 1 câu hỏi quan trọng nhất để khoanh vùng bệnh");
        promptBuilder.AppendLine("3. Giải thích tại sao cần hỏi câu này");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON (chỉ JSON, không có text khác):**");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"question\": \"Câu hỏi của bạn?\",");
        promptBuilder.AppendLine("  \"purpose\": \"Lý do hỏi câu này\",");
        promptBuilder.AppendLine("  \"priority\": \"HIGH\"");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Build prompt for conclusion mode (= 3 questions)
    /// </summary>
    private string BuildConclusionModePrompt(string userMessage, List<ConversationMessage> history)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("Bạn là bác sĩ AI chuyên nghiệp. Dựa trên 3 câu hỏi và câu trả lời, hãy đưa ra kết luận.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**LỊCH SỬ HỘI THOẠI:**");
        
        foreach (var msg in history)
        {
            promptBuilder.AppendLine($"{msg.Role.ToUpper()}: {msg.Content}");
        }
        
        promptBuilder.AppendLine($"USER: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**DANH SÁCH CHUYÊN KHOA CÓ SẴN:**");
        promptBuilder.AppendLine(GetSpecialtyListText());
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU:**");
        promptBuilder.AppendLine("1. Xác định bệnh có thể (tên tiếng Việt)");
        promptBuilder.AppendLine("2. Đánh giá độ tin cậy (0-1, ví dụ: 0.85)");
        promptBuilder.AppendLine("3. Giải thích lý do chẩn đoán (2-3 lý do)");
        promptBuilder.AppendLine("4. Đưa ra lời khuyên cụ thể (2-3 lời khuyên)");
        promptBuilder.AppendLine("5. Chọn 1-3 chuyên khoa phù hợp nhất từ danh sách (phải khớp chính xác tên)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**TRẢ VỀ JSON (chỉ JSON, không có text khác):**");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"disease\": {");
        promptBuilder.AppendLine("    \"name\": \"Tên bệnh\",");
        promptBuilder.AppendLine("    \"confidence\": 0.85,");
        promptBuilder.AppendLine("    \"reasons\": [\"Lý do 1\", \"Lý do 2\"]");
        promptBuilder.AppendLine("  },");
        promptBuilder.AppendLine("  \"advice\": [\"Lời khuyên 1\", \"Lời khuyên 2\"],");
        promptBuilder.AppendLine("  \"specialties\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"name\": \"Tên chuyên khoa (phải khớp với danh sách)\",");
        promptBuilder.AppendLine("      \"confidence\": 0.9,");
        promptBuilder.AppendLine("      \"reasons\": [\"Lý do chọn\"]");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ]");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Get specialty list text for prompt
    /// </summary>
    private string GetSpecialtyListText()
    {
        // Common Vietnamese medical specialties
        var specialties = new[]
        {
            "Nội khoa", "Ngoại khoa", "Sản phụ khoa", "Nhi khoa", "Tim mạch",
            "Hô hấp", "Tiêu hóa", "Thần kinh", "Cơ xương khớp", "Da liễu",
            "Tai mũi họng", "Mắt", "Răng hàm mặt", "Tâm thần", "Nội tiết",
            "Thận - Tiết niệu", "Ung bướu", "Chấn thương chỉnh hình", "Y học cổ truyền"
        };
        
        return string.Join(", ", specialties);
    }

    /// <summary>
    /// Call Gemini API with retry logic
    /// </summary>
    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_geminiConfig.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }

        // Try multiple models and API versions for compatibility
        // v1beta supports more models than v1
        var modelsToTry = new[]
        {
            "gemini-2.5-pro",      // Latest pro model (works on v1beta)
            "gemini-2.5-flash",    // Latest flash model
            "gemini-2.0-flash",    // Fallback flash
            "gemini-1.5-pro",      // Stable pro
            "gemini-1.5-flash",    // Stable flash
        };

        var apiVersions = new[] { "v1beta", "v1" }; // Try v1beta first
        Exception? lastException = null;

        foreach (var apiVersion in apiVersions)
        {
            foreach (var model in modelsToTry)
            {
                try
                {
                    var url = $"{_geminiConfig.ApiEndpoint}/{apiVersion}/models/{model}:generateContent?key={_geminiConfig.ApiKey}";
                    var result = await CallGeminiApiWithUrlAsync(url, prompt, model, apiVersion);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed with {ApiVersion}/{Model}, trying next", apiVersion, model);
                    lastException = ex;
                }
            }
        }

        throw new InvalidOperationException($"Failed to call Gemini API with any model. Last error: {lastException?.Message}", lastException);
    }

    /// <summary>
    /// Call Gemini API with specific URL
    /// </summary>
    private async Task<string> CallGeminiApiWithUrlAsync(string url, string prompt, string model, string apiVersion)
    {
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 8192, // Increased for longer conclusion responses
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

        _logger.LogInformation("Calling Gemini API: {ApiVersion}/models/{Model}", apiVersion, model);

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // If quota exceeded (429), throw specific error
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Gemini API quota exceeded. Response: {Response}", responseContent);
                throw new InvalidOperationException(
                    "Gemini API quota exceeded. Please check your billing plan or wait for quota reset. " +
                    "Visit https://ai.google.dev/gemini-api/docs/rate-limits for more information."
                );
            }
            
            // If model not found (404), just log and continue to next model
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Model {Model} not found on {ApiVersion}, trying next", model, apiVersion);
                throw new HttpRequestException($"Model not found: {model}");
            }
            
            _logger.LogWarning("Gemini API failed: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
            throw new HttpRequestException($"Gemini API returned error: {response.StatusCode}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
        {
            _logger.LogWarning("Gemini API returned no candidates. Response: {Response}", responseContent);
            throw new InvalidOperationException("Gemini API returned no candidates");
        }

        var generatedText = geminiResponse.Candidates[0]?.Content?.Parts?[0]?.Text;

        if (string.IsNullOrEmpty(generatedText))
        {
            _logger.LogWarning("Gemini API returned empty text. Full response: {Response}", responseContent);
            throw new InvalidOperationException("Gemini API returned empty text");
        }

        _logger.LogInformation("Successfully called Gemini using {ApiVersion}/{Model}", apiVersion, model);
        return generatedText;
    }

    #endregion

    #region Gemini Response Models

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

    #region Response Parsing

    /// <summary>
    /// Parse Gemini response for asking mode
    /// </summary>
    private SymptomAnalysisResponse ParseAskingModeResponse(string geminiResponse, Guid sessionId, int questionCount)
    {
        try
        {
            // Extract JSON from response (Gemini might add extra text)
            string jsonText = ExtractJsonFromText(geminiResponse);
            
            var questionData = JsonSerializer.Deserialize<AskingModeResponse>(jsonText, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (questionData == null || string.IsNullOrEmpty(questionData.Question))
            {
                throw new InvalidOperationException("Failed to parse question from Gemini response");
            }

            return new SymptomAnalysisResponse
            {
                SessionId = sessionId,
                Message = questionData.Question,
                NextQuestions = new List<FollowUpQuestion>
                {
                    new FollowUpQuestion
                    {
                        Question = questionData.Question,
                        Purpose = questionData.Purpose ?? "Để xác định chính xác tình trạng của bạn",
                        Priority = questionData.Priority ?? "MEDIUM"
                    }
                },
                AnalysisComplete = false,
                QuestionCount = questionCount + 1,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing asking mode response: {Response}", geminiResponse);
            
            // Fallback: create a generic question
            return new SymptomAnalysisResponse
            {
                SessionId = sessionId,
                Message = "Bạn có thể mô tả chi tiết hơn về triệu chứng của mình không?",
                NextQuestions = new List<FollowUpQuestion>
                {
                    new FollowUpQuestion
                    {
                        Question = "Bạn có thể mô tả chi tiết hơn về triệu chứng của mình không?",
                        Purpose = "Để hiểu rõ hơn tình trạng của bạn",
                        Priority = "MEDIUM"
                    }
                },
                AnalysisComplete = false,
                QuestionCount = questionCount + 1,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Parse Gemini response for conclusion mode
    /// </summary>
    private async Task<SymptomAnalysisResponse> ParseConclusionModeResponse(
        string geminiResponse, 
        Guid sessionId, 
        int questionCount,
        LocationContext? location)
    {
        try
        {
            // Extract JSON from response
            string jsonText = ExtractJsonFromText(geminiResponse);
            
            var conclusionData = JsonSerializer.Deserialize<ConclusionModeResponse>(jsonText, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (conclusionData == null)
            {
                throw new InvalidOperationException("Failed to parse conclusion from Gemini response");
            }

            // Build response
            var response = new SymptomAnalysisResponse
            {
                SessionId = sessionId,
                AnalysisComplete = true,
                QuestionCount = MAX_QUESTIONS,
                Timestamp = DateTime.UtcNow
            };

            // Set disease conclusion
            if (conclusionData.Disease != null)
            {
                response.Disease = new DiseaseConclusion
                {
                    Name = conclusionData.Disease.Name ?? "Chưa xác định",
                    Confidence = conclusionData.Disease.Confidence,
                    Reasons = conclusionData.Disease.Reasons ?? new List<string>()
                };

                response.PossibleDiseases = new List<DiseaseMatch>
                {
                    new DiseaseMatch
                    {
                        Name = conclusionData.Disease.Name ?? "Chưa xác định",
                        Confidence = conclusionData.Disease.Confidence,
                        Description = string.Join(". ", conclusionData.Disease.Reasons ?? new List<string>())
                    }
                };
            }

            // Set advice
            response.GeneralAdvice = conclusionData.Advice ?? new List<string>();

            // Set specialties and get recommendations
            if (conclusionData.Specialties != null && conclusionData.Specialties.Count > 0)
            {
                var specialtyIds = await MatchSpecialtiesToIds(conclusionData.Specialties);
                
                response.RecommendedSpecialties = conclusionData.Specialties.Select(s => new SpecialtyMatch
                {
                    SpecialtyName = s.Name ?? "",
                    Confidence = s.Confidence,
                    Reasons = s.Reasons ?? new List<string>()
                }).ToList();

                // Get doctor and hospital recommendations
                if (specialtyIds.Count > 0)
                {
                    response.RecommendedDoctors = await GetDoctorRecommendations(specialtyIds, location);
                    response.RecommendedHospitals = await GetHospitalRecommendations(
                        specialtyIds, 
                        conclusionData.Specialties.Select(s => s.Name ?? "").ToList(), 
                        location);
                }
            }

            // Build message
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Dựa trên các triệu chứng bạn mô tả, có thể bạn đang gặp vấn đề về **{response.Disease?.Name ?? "sức khỏe"}**.");
            messageBuilder.AppendLine();
            
            if (response.GeneralAdvice.Count > 0)
            {
                messageBuilder.AppendLine("**Lời khuyên:**");
                foreach (var advice in response.GeneralAdvice)
                {
                    messageBuilder.AppendLine($"- {advice}");
                }
                messageBuilder.AppendLine();
            }

            if (response.RecommendedSpecialties.Count > 0)
            {
                messageBuilder.AppendLine($"Bạn nên đến khám chuyên khoa: **{string.Join(", ", response.RecommendedSpecialties.Select(s => s.SpecialtyName))}**");
                messageBuilder.AppendLine();
            }

            // Add disclaimer
            messageBuilder.AppendLine("Lưu ý: Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.");

            response.Message = messageBuilder.ToString();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing conclusion mode response: {Response}", geminiResponse);
            throw;
        }
    }

    /// <summary>
    /// Extract JSON from text (handles cases where Gemini adds extra text)
    /// </summary>
    private string ExtractJsonFromText(string text)
    {
        // Try to find JSON object in the text
        int startIndex = text.IndexOf('{');
        int endIndex = text.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }

        return text;
    }

    #endregion

    #region Specialty Matching

    /// <summary>
    /// Match specialty names from Gemini to actual specialty IDs in database
    /// </summary>
    private async Task<List<Guid>> MatchSpecialtiesToIds(List<SpecialtyData> specialties)
    {
        var specialtyIds = new List<Guid>();

        try
        {
            // Call Doctor service to get all specialties
            var request = new GetAllSpecialtiesRequest();
            var response = await _doctorClient.GetAllSpecialtiesAsync(request);

            foreach (var specialty in specialties)
            {
                // Try exact match first
                var match = response.Specialties.FirstOrDefault(s => 
                    s.Name.Equals(specialty.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    specialtyIds.Add(Guid.Parse(match.Id));
                    continue;
                }

                // Try fuzzy match (contains)
                match = response.Specialties.FirstOrDefault(s => 
                    s.Name.Contains(specialty.Name ?? "", StringComparison.OrdinalIgnoreCase) ||
                    (specialty.Name ?? "").Contains(s.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    specialtyIds.Add(Guid.Parse(match.Id));
                }
            }

            _logger.LogInformation("Matched {Count} specialties to IDs", specialtyIds.Count);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error calling Doctor service to get specialties");
        }

        return specialtyIds;
    }

    #endregion

    #region Doctor and Hospital Recommendations

    /// <summary>
    /// Get doctor recommendations with location-based ranking
    /// </summary>
    private async Task<List<DoctorRecommendation>> GetDoctorRecommendations(
        List<Guid> specialtyIds, 
        LocationContext? location)
    {
        try
        {
            var request = new FilterDoctorsForRecommendationRequest
            {
                MaxResults = MAX_DOCTOR_RECOMMENDATIONS * 2 // Get more for better ranking
            };

            request.SpecialtyIds.AddRange(specialtyIds.Select(id => id.ToString()));

            if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
            {
                request.ProvinceId = location.ProvinceId;
                request.DistrictId = location.DistrictId ?? "";
            }

            var response = await _doctorClient.FilterDoctorsForRecommendationAsync(request);

            // Rank doctors
            var rankedDoctors = response.Doctors
                .Select(d => new
                {
                    Doctor = d,
                    Score = CalculateDoctorScore(d, location)
                })
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

            _logger.LogInformation("Found {Count} doctor recommendations", rankedDoctors.Count);
            return rankedDoctors;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error calling Doctor service for recommendations");
            return new List<DoctorRecommendation>();
        }
    }

    /// <summary>
    /// Calculate doctor recommendation score
    /// Priority: Location (60%) > Rating (30%) > Experience (10%)
    /// </summary>
    private double CalculateDoctorScore(DoctorRecommendationInfo doctor, LocationContext? location)
    {
        double score = 0;

        // 1. Location match (60% weight)
        if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
        {
            // Note: DoctorRecommendationInfo doesn't have location fields in proto
            // We rely on the gRPC service to filter by location already
            // So we give base score for doctors returned
            score += 0.4;
        }

        // 2. Rating (30% weight)
        score += (doctor.Rating / 5.0) * 0.3;

        // 3. Experience (10% weight)
        score += Math.Min(doctor.YearsOfExperience / 20.0, 1.0) * 0.1;

        return score;
    }

    /// <summary>
    /// Get hospital recommendations with location-based ranking
    /// </summary>
    private async Task<List<HospitalRecommendation>> GetHospitalRecommendations(
        List<Guid> specialtyIds,
        List<string> specialtyNames,
        LocationContext? location)
    {
        try
        {
            // For now, use GetHospitalsBySpecialty for each specialty
            // TODO: Add FilterHospitalsBySpecialty gRPC method for better performance
            var allHospitals = new List<HospitalReply>();
            
            foreach (var specialtyId in specialtyIds.Take(3)) // Limit to 3 specialties
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
                catch (RpcException ex)
                {
                    _logger.LogWarning(ex, "Error getting hospitals for specialty {SpecialtyId}", specialtyId);
                }
            }

            // Remove duplicates and rank
            var uniqueHospitals = allHospitals
                .GroupBy(h => h.Id)
                .Select(g => g.First())
                .Select(h => new HospitalRecommendation
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    SpecialtyNames = specialtyNames, // Use specialty names from Gemini response
                    ImageUrl = h.AvatarUrl,
                    RecommendationScore = CalculateHospitalScoreBasic(h, location)
                })
                .OrderByDescending(h => h.RecommendationScore)
                .Take(MAX_HOSPITAL_RECOMMENDATIONS)
                .ToList();

            _logger.LogInformation("Found {Count} hospital recommendations", uniqueHospitals.Count);
            return uniqueHospitals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Hospital service for recommendations");
            return new List<HospitalRecommendation>();
        }
    }

    /// <summary>
    /// Calculate hospital recommendation score (basic version)
    /// Priority: Location matching based on address string
    /// </summary>
    private double CalculateHospitalScoreBasic(HospitalReply hospital, LocationContext? location)
    {
        double score = 0.5; // Base score

        // Simple location matching based on address string
        if (location != null && !string.IsNullOrEmpty(location.DisplayName))
        {
            var address = hospital.Address?.ToLowerInvariant() ?? "";
            var locationName = location.DisplayName.ToLowerInvariant();
            
            if (address.Contains(locationName))
            {
                score += 0.5;
            }
        }

        return score;
    }

    #endregion

    #region Conversation Saving

    /// <summary>
    /// Save conversation to database
    /// </summary>
    private async Task SaveConversationAsync(
        Guid sessionId,
        Guid? userId,
        string userMessage,
        SymptomAnalysisResponse aiResponse,
        List<ConversationMessage> previousHistory)
    {
        try
        {
            // Save to database
            await _sessionService.SaveConversationHistoryAsync(
                sessionId,
                userMessage,
                aiResponse.Message,
                null, // location
                aiResponse.AnalysisComplete ? new { 
                    doctors = aiResponse.RecommendedDoctors,
                    hospitals = aiResponse.RecommendedHospitals
                } : null,
                userId
            );

            _logger.LogInformation("Saved conversation for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation for session {SessionId}", sessionId);
            // Don't throw - saving conversation failure shouldn't break the flow
        }
    }

    #endregion

    #region Response Data Models

    private class AskingModeResponse
    {
        public string? Question { get; set; }
        public string? Purpose { get; set; }
        public string? Priority { get; set; }
    }

    private class ConclusionModeResponse
    {
        public DiseaseData? Disease { get; set; }
        public List<string>? Advice { get; set; }
        public List<SpecialtyData>? Specialties { get; set; }
    }

    private class DiseaseData
    {
        public string? Name { get; set; }
        public double Confidence { get; set; }
        public List<string>? Reasons { get; set; }
    }

    private class SpecialtyData
    {
        public string? Name { get; set; }
        public double Confidence { get; set; }
        public List<string>? Reasons { get; set; }
    }

    #endregion
}
