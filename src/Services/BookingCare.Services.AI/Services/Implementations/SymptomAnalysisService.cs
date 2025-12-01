using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service implementation for symptom analysis with 3-question workflow
/// </summary>
public class SymptomAnalysisService : ISymptomAnalysisService
{
    private readonly IConversationSessionService _sessionService;
    private readonly ILogger<SymptomAnalysisService> _logger;
    private readonly GeminiApiHelper _geminiApiHelper;
    private readonly ServiceGeminiConfiguration _serviceConfig;
    private readonly RecommendationHelper _recommendationHelper;

    private const int MAX_QUESTIONS = 6; // Support extended consultation: 3 initial + 3 additional questions

    public SymptomAnalysisService(
        IConversationSessionService sessionService,
        ILogger<SymptomAnalysisService> logger,
        GeminiApiHelper geminiApiHelper,
        IOptions<GeminiServicesConfiguration> geminiServicesConfig,
        RecommendationHelper recommendationHelper)
    {
        _sessionService = sessionService;
        _logger = logger;
        _geminiApiHelper = geminiApiHelper;
        _serviceConfig = geminiServicesConfig.Value.SymptomAnalysis;
        _recommendationHelper = recommendationHelper;
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
            int totalQuestions = CountAIQuestions(conversationHistory);
            _logger.LogInformation("Total questions asked: {TotalQuestions}", totalQuestions);
            _logger.LogInformation("Conversation history count: {Count}", conversationHistory.Count);

            // Step 3: Determine if we're in conclusion mode
            // Conclusion mode triggers when we've completed 3 questions in a round
            // totalQuestions = 3 (after Q1, Q2, Q3) → first conclusion (Round 1)
            // totalQuestions = 6 (after Q4, Q5, Q6) → second conclusion (Round 2)
            bool isConclusionMode = (totalQuestions % 3) == 0 && totalQuestions > 0 && totalQuestions <= 6;

            // Step 4: Calculate current round and question number
            // For conclusion mode: round is based on COMPLETED rounds
            // For asking mode: round is based on CURRENT round in progress
            int currentRound;
            int questionInRound;

            if (isConclusionMode)
            {
                // At conclusion: totalQuestions = 3 → Round 1, totalQuestions = 6 → Round 2
                currentRound = totalQuestions / 3; // 3/3=1, 6/3=2
                questionInRound = 3; // Always 3 at conclusion
            }
            else
            {
                // During asking: calculate which round and question we're on
                // IMPORTANT: totalQuestions includes both questions AND conclusions
                // Pattern: Q1, Q2, Q3, Conclusion (4 messages per round)
                // We need to subtract conclusions to get actual question count
                int numConclusions = totalQuestions / 4; // 0-3→0, 4-7→1, 8+→2
                int actualQuestions = totalQuestions - numConclusions;

                currentRound = (actualQuestions / 3) + 1; // 0-2→1, 3-5→2
                questionInRound = (actualQuestions % 3) + 1; // 0→1, 1→2, 2→3

                // Examples:
                // totalQuestions=0: numConclusions=0, actualQuestions=0, round=1, question=1 ✓
                // totalQuestions=1: numConclusions=0, actualQuestions=1, round=1, question=2 ✓
                // totalQuestions=2: numConclusions=0, actualQuestions=2, round=1, question=3 ✓
                // totalQuestions=4: numConclusions=1, actualQuestions=3, round=2, question=1 ✓ (after Round 1 conclusion)
                // totalQuestions=5: numConclusions=1, actualQuestions=4, round=2, question=2 ✓
                // totalQuestions=6: numConclusions=1, actualQuestions=5, round=2, question=3 ✓
            }

            _logger.LogInformation(
                "🔍 DEBUG - Round: {Round}, QuestionInRound: {QuestionInRound}, TotalQuestions: {TotalQuestions}, IsConclusionMode: {IsConclusionMode}",
                currentRound, questionInRound, totalQuestions, isConclusionMode);

            // Step 4: Build prompt for Gemini (pre-fetch specialty list for conclusion mode)
            Task<string>? specialtyListTask = null;
            if (isConclusionMode)
            {
                specialtyListTask = _recommendationHelper.GetSpecialtyListTextAsync();
            }

            string prompt = isConclusionMode
                ? await BuildConclusionModePromptAsync(request.Message, conversationHistory, specialtyListTask!)
                : BuildAskingModePrompt(request.Message, conversationHistory);

            // Step 5: Call Gemini API
            string geminiResponse = await CallGeminiApiAsync(prompt);
            _logger.LogDebug("Gemini response: {Response}", geminiResponse);

            // Step 6: Parse response
            SymptomAnalysisResponse response = isConclusionMode
                ? await ParseConclusionModeResponse(
                    geminiResponse,
                    sessionId,
                    currentRound,
                    request.Location)
                : ParseAskingModeResponse(geminiResponse, sessionId, questionInRound);

            // Step 7: Prepare data for saving and return response (save in background)
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

            // Save conversation to database
            // Changed from fire-and-forget to awaited to ensure data is saved properly
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation for session {SessionId}", sessionId);
                // Don't throw - saving conversation failure shouldn't break the flow
            }

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
    private async Task<string> BuildConclusionModePromptAsync(string userMessage, List<ConversationMessage> history, Task<string> specialtyListTask)
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
        promptBuilder.AppendLine(await specialtyListTask);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU:**");
        promptBuilder.AppendLine("1. Xác định bệnh có thể (tên tiếng Việt)");
        promptBuilder.AppendLine("2. Đánh giá độ tin cậy (0-1, ví dụ: 0.85)");
        promptBuilder.AppendLine("3. Giải thích lý do chẩn đoán (2-3 lý do)");
        promptBuilder.AppendLine("4. Đưa ra lời khuyên cụ thể (2-3 lời khuyên)");
        promptBuilder.AppendLine("5. Chọn 1 chuyên khoa phù hợp nhất từ danh sách (phải khớp chính xác tên)");
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
    /// Call Gemini API with retry logic using GeminiApiHelper
    /// </summary>
    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        return await _geminiApiHelper.CallGeminiApiWithDefaultsAsync(
            prompt,
            _serviceConfig);
    }

    #endregion


    #region Response Parsing

    /// <summary>
    /// Parse Gemini response for asking mode
    /// Optimized JSON parsing using JsonDocument for better performance
    /// </summary>
    private SymptomAnalysisResponse ParseAskingModeResponse(string geminiResponse, Guid sessionId, int questionInRound)
    {
        try
        {
            // Extract JSON from response (Gemini might add extra text)
            string jsonText = ExtractJsonFromText(geminiResponse);

            // Use JsonDocument for faster parsing when we only need specific fields
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var question = root.TryGetProperty("question", out var questionProp)
                ? questionProp.GetString()
                : null;
            var purpose = root.TryGetProperty("purpose", out var purposeProp)
                ? purposeProp.GetString()
                : null;
            var priority = root.TryGetProperty("priority", out var priorityProp)
                ? priorityProp.GetString()
                : null;

            if (string.IsNullOrEmpty(question))
            {
                throw new InvalidOperationException("Failed to parse question from Gemini response");
            }

            return new SymptomAnalysisResponse
            {
                SessionId = sessionId,
                Message = question,
                NextQuestions = new List<FollowUpQuestion>
                {
                    new FollowUpQuestion
                    {
                        Question = question,
                        Purpose = purpose ?? "Để xác định chính xác tình trạng của bạn",
                        Priority = priority ?? "MEDIUM"
                    }
                },
                AnalysisComplete = false,
                QuestionCount = questionInRound, // Question number in current round (1-3)
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
                QuestionCount = questionInRound,
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
        int currentRound,
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

            // Build response skeleton
            var response = CreateBaseConclusionResponse(sessionId, currentRound);

            // Set disease conclusion & advice
            SetDiseaseConclusion(response, conclusionData);
            response.GeneralAdvice = conclusionData.Advice ?? new List<string>();

            // Set specialties and get recommendations in parallel với việc xây dựng message
            var recommendationsTask = StartRecommendationTask(conclusionData, response, location);

            // Build message while recommendations are being fetched
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

            // Chờ lấy danh sách gợi ý bác sĩ/bệnh viện
            var (doctors, hospitals) = await recommendationsTask;
            response.RecommendedDoctors = doctors;
            response.RecommendedHospitals = hospitals;

            // Set CanRequestMoreQuestions flag
            SetCanRequestMoreQuestionsFlag(response, currentRound);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error parsing conclusion mode response for session {SessionId}. Response preview: {ResponsePreview}",
                sessionId,
                geminiResponse.Length > 300 ? geminiResponse[..300] + "..." : geminiResponse);

            throw new InvalidOperationException(
                "Failed to parse conclusion mode response from Gemini.",
                ex);
        }
    }

    private static SymptomAnalysisResponse CreateBaseConclusionResponse(Guid sessionId, int currentRound)
    {
        return new SymptomAnalysisResponse
        {
            SessionId = sessionId,
            AnalysisComplete = true,
            QuestionCount = 3, // Always 3 at conclusion (end of round)
            CurrentRound = currentRound, // 1 or 2
            MaxQuestions = 3, // Always 3 questions per round
            Timestamp = DateTime.UtcNow
        };
    }

    private static void SetDiseaseConclusion(SymptomAnalysisResponse response, ConclusionModeResponse? conclusionData)
    {
        if (conclusionData?.Disease == null)
        {
            return;
        }

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

    private Task<(List<DoctorRecommendation> Doctors, List<HospitalRecommendation> Hospitals)>
        StartRecommendationTask(
            ConclusionModeResponse conclusionData,
            SymptomAnalysisResponse response,
            LocationContext? location)
    {
        if (conclusionData.Specialties == null || conclusionData.Specialties.Count == 0)
        {
            // Không có chuyên khoa → trả về danh sách rỗng
            return Task.FromResult(
                (new List<DoctorRecommendation>(), new List<HospitalRecommendation>()));
        }

        var specialtyNames = conclusionData.Specialties
            .Select(s => s.Name ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        response.RecommendedSpecialties = conclusionData.Specialties.Select(s => new SpecialtyMatch
        {
            SpecialtyName = s.Name ?? "",
            Confidence = s.Confidence,
            Reasons = s.Reasons ?? new List<string>()
        }).ToList();

        return specialtyNames.Count > 0
            ? _recommendationHelper.GetRecommendationsAsync(specialtyNames, location)
            : Task.FromResult(
                (new List<DoctorRecommendation>(), new List<HospitalRecommendation>()));
    }

    private void SetCanRequestMoreQuestionsFlag(SymptomAnalysisResponse response, int currentRound)
    {
        // Allow more questions only if:
        // 1. This is the first round conclusion (currentRound == 1)
        // 2. Confidence is below 90% (0.9)
        // 3. Disease conclusion exists
        if (response.Disease == null)
        {
            return;
        }

        response.CanRequestMoreQuestions =
            currentRound == 1 &&
            response.Disease.Confidence < 0.9;

        _logger.LogInformation(
            "Conclusion: Disease={Disease}, Confidence={Confidence}, Round={Round}, CanRequestMore={CanRequestMore}",
            response.Disease.Name,
            response.Disease.Confidence,
            currentRound,
            response.CanRequestMoreQuestions);
    }

    /// <summary>
    /// Extract JSON from text (handles cases where Gemini adds extra text)
    /// Optimized using Span for better performance
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

        // Use Span<char> for better performance on large strings
        return text.AsSpan(startIndex, endIndex - startIndex + 1).ToString();
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
        SymptomAnalysisResponse aiResponse)
    {
        try
        {
            // Save to database
            await _sessionService.SaveConversationHistoryAsync(
                sessionId,
                userMessage,
                aiResponse.Message,
                null, // location
                aiResponse.AnalysisComplete ? new
                {
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
