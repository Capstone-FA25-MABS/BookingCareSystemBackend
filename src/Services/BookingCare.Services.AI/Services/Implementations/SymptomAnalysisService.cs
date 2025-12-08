using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;


namespace BookingCare.Services.AI.Services.Implementations;


/// <summary>
/// Service implementation for symptom analysis with 3-question workflow
/// </summary>
public class SymptomAnalysisService : ISymptomAnalysisService
{
    private readonly IConversationSessionService _sessionService;
    private readonly ILogger<SymptomAnalysisService> _logger;
    private readonly GroqApiHelper _groqApiHelper;
    private readonly RecommendationHelper _recommendationHelper;
    private readonly IContextKeywordExtractor _contextExtractor;
    private readonly IQuestionCacheService _questionCacheService;


    private const int MAX_QUESTIONS = 6; // Support extended consultation: 3 initial + 3 additional questions


    public SymptomAnalysisService(
        IConversationSessionService sessionService,
        ILogger<SymptomAnalysisService> logger,
        GroqApiHelper groqApiHelper,
        RecommendationHelper recommendationHelper,
        IContextKeywordExtractor contextExtractor,
        IQuestionCacheService questionCacheService)
    {
        _sessionService = sessionService;
        _logger = logger;
        _groqApiHelper = groqApiHelper;
        _recommendationHelper = recommendationHelper;
        _contextExtractor = contextExtractor;
        _questionCacheService = questionCacheService;
    }


    public async Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Starting symptom analysis for user {UserId}, session {SessionId}",
                request.UserId, request.SessionId);

            var sessionId = await _sessionService.GetOrCreateSessionAsync(
                request.SessionId,
                request.UserId ?? Guid.Empty,
                request.Location);

            var conversationHistory = request.ConversationHistory ?? new List<ConversationMessage>();
            int totalQuestions = CountAIQuestions(conversationHistory);
            _logger.LogInformation("Total questions asked: {TotalQuestions}", totalQuestions);
            _logger.LogInformation("Conversation history count: {Count}", conversationHistory.Count);

            var (isConclusionMode, currentRound, questionInRound) = CalculateRoundAndQuestion(totalQuestions);
            _logger.LogInformation(
                "🔍 DEBUG - Round: {Round}, QuestionInRound: {QuestionInRound}, TotalQuestions: {TotalQuestions}, IsConclusionMode: {IsConclusionMode}",
                currentRound, questionInRound, totalQuestions, isConclusionMode);

            var cachedResponse = await TryGetCachedResponseIfApplicable(
                isConclusionMode,
                request.Message,
                conversationHistory,
                sessionId,
                questionInRound,
                request.Location,
                request.UserId);

            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            var response = await ProcessAnalysisAsync(
                request,
                conversationHistory,
                sessionId,
                isConclusionMode,
                currentRound,
                questionInRound);

            await SaveQuestionToCacheIfNeeded(
                isConclusionMode,
                request.Message,
                conversationHistory,
                questionInRound,
                response);

            await SaveConversationAsync(
                sessionId,
                request.Message,
                response,
                request.Location,
                request.UserId);

            _logger.LogInformation("Symptom analysis completed successfully for session {SessionId}", sessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Message}", ex.Message);
            throw new SymptomAnalysisException("Failed to analyze symptoms", ex);
        }
    }

    private (bool isConclusionMode, int currentRound, int questionInRound) CalculateRoundAndQuestion(int totalQuestions)
    {
        bool isConclusionMode = (totalQuestions % 3) == 0 && totalQuestions > 0 && totalQuestions <= 6;

        if (isConclusionMode)
        {
            int currentRound = totalQuestions / 3;
            int questionInRound = 3;
            return (isConclusionMode, currentRound, questionInRound);
        }

        int numConclusions = totalQuestions / 4;
        int actualQuestions = totalQuestions - numConclusions;
        int currentRound = (actualQuestions / 3) + 1;
        int questionInRound = (actualQuestions % 3) + 1;

        return (isConclusionMode, currentRound, questionInRound);
    }

    private async Task<SymptomAnalysisResponse?> TryGetCachedResponseIfApplicable(
        bool isConclusionMode,
        string userMessage,
        List<ConversationMessage> conversationHistory,
        Guid sessionId,
        int questionInRound,
        LocationContext? location,
        Guid? userId)
    {
        if (isConclusionMode)
        {
            return null;
        }

        var cachedResponse = await TryGetCachedQuestionAsync(
            userMessage,
            conversationHistory,
            sessionId,
            questionInRound);

        if (cachedResponse == null)
        {
            return null;
        }

        await SaveCachedConversationAsync(sessionId, userMessage, cachedResponse, location, userId);
        return cachedResponse;
    }

    private async Task SaveCachedConversationAsync(
        Guid sessionId,
        string userMessage,
        SymptomAnalysisResponse cachedResponse,
        LocationContext? location,
        Guid? userId)
    {
        try
        {
            await _sessionService.SaveConversationHistoryAsync(
                sessionId: sessionId,
                userMessage: userMessage,
                aiMessage: cachedResponse.Message,
                location: location,
                suggestions: null,
                userId: userId,
                disease: null,
                questionCount: cachedResponse.QuestionCount,
                analysisComplete: false);

            _logger.LogDebug("Successfully saved cached question to conversation history for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cached conversation for session {SessionId}", sessionId);
        }
    }

    private async Task<SymptomAnalysisResponse> ProcessAnalysisAsync(
        SymptomAnalysisRequest request,
        List<ConversationMessage> conversationHistory,
        Guid sessionId,
        bool isConclusionMode,
        int currentRound,
        int questionInRound)
    {
        string prompt = await BuildPromptAsync(
            request.Message,
            conversationHistory,
            isConclusionMode);

        string groqResponse = await CallGroqApiAsync(prompt, isConclusionMode);
        _logger.LogDebug("Groq response: {Response}", groqResponse);

        return isConclusionMode
            ? await ParseConclusionModeResponse(groqResponse, sessionId, currentRound, request.Location)
            : ParseAskingModeResponse(groqResponse, sessionId, questionInRound);
    }

    private async Task<string> BuildPromptAsync(
        string userMessage,
        List<ConversationMessage> conversationHistory,
        bool isConclusionMode)
    {
        if (isConclusionMode)
        {
            var specialtyListTask = _recommendationHelper.GetSpecialtyListTextAsync();
            return await BuildConclusionModePromptAsync(userMessage, conversationHistory, specialtyListTask);
        }

        return BuildAskingModePrompt(userMessage, conversationHistory);
    }

    private async Task SaveQuestionToCacheIfNeeded(
        bool isConclusionMode,
        string userMessage,
        List<ConversationMessage> conversationHistory,
        int questionInRound,
        SymptomAnalysisResponse response)
    {
        if (isConclusionMode || response.NextQuestions?.Count == 0)
        {
            return;
        }

        await SaveQuestionToCacheAsync(
            userMessage,
            conversationHistory,
            questionInRound,
            response);
    }

    private async Task SaveConversationAsync(
        Guid sessionId,
        string userMessage,
        SymptomAnalysisResponse response,
        LocationContext? location,
        Guid? userId)
    {
        var (suggestions, disease) = PrepareSaveData(response);

        try
        {
            await _sessionService.SaveConversationHistoryAsync(
                sessionId: sessionId,
                userMessage: userMessage,
                aiMessage: response.Message,
                location: location,
                suggestions: suggestions,
                userId: userId,
                disease: disease,
                questionCount: response.QuestionCount,
                analysisComplete: response.AnalysisComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation for session {SessionId}", sessionId);
        }
    }

    private static (object? suggestions, object? disease) PrepareSaveData(SymptomAnalysisResponse response)
    {
        if (!response.AnalysisComplete || response.Disease == null)
        {
            return (null, null);
        }

        object disease = new
        {
            Name = response.Disease.Name,
            Confidence = response.Disease.Confidence,
            Reasons = response.Disease.Reasons
        };

        object? suggestions = null;
        if (response.RecommendedDoctors?.Count > 0 || response.RecommendedHospitals?.Count > 0)
        {
            suggestions = new
            {
                doctors = response.RecommendedDoctors,
                hospitals = response.RecommendedHospitals
            };
        }

        return (suggestions, disease);
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
        promptBuilder.AppendLine("3. Giải thích lý do chi tiết về chẩn đoán (3-4 lý do)");
        promptBuilder.AppendLine("4. Đưa ra lời khuyên chi tiết, cụ thể (3-4 lời khuyên)");
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
    /// Call Groq API with retry logic using GroqApiHelper
    /// </summary>
    private async Task<string> CallGroqApiAsync(string prompt, bool isConclusionMode)
    {
        if (isConclusionMode)
        {
            return await _groqApiHelper.CallConclusionModeAsync(prompt);
        }
        else
        {
            return await _groqApiHelper.CallAskingModeAsync(prompt);
        }
    }


    #endregion




    #region Response Parsing


    /// <summary>
    /// Parse Groq response for asking mode
    /// Optimized JSON parsing using JsonDocument for better performance
    /// </summary>
    private SymptomAnalysisResponse ParseAskingModeResponse(string groqResponse, Guid sessionId, int questionInRound)
    {
        try
        {
            // Extract JSON from response (Groq might add extra text)
            string jsonText = ExtractJsonFromText(groqResponse);


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
                throw new InvalidOperationException("Failed to parse question from Groq response");
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
            _logger.LogError(ex, "Error parsing asking mode response: {Response}", groqResponse);


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
    /// Parse Groq response for conclusion mode
    /// </summary>
    private async Task<SymptomAnalysisResponse> ParseConclusionModeResponse(
        string groqResponse,
        Guid sessionId,
        int currentRound,
        LocationContext? location)
    {
        try
        {
            // Extract JSON from response
            string jsonText = ExtractJsonFromText(groqResponse);


            var conclusionData = JsonSerializer.Deserialize<ConclusionModeResponse>(jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


            if (conclusionData == null)
            {
                throw new InvalidOperationException("Failed to parse conclusion from Groq response");
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
                groqResponse.Length > 300 ? groqResponse[..300] + "..." : groqResponse);


            throw new InvalidOperationException(
                "Failed to parse conclusion mode response from Groq.",
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
    /// Extract JSON from text (handles cases where Groq adds extra text)
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


    /// <summary>
    /// Try to get cached question using 3-tier lookup (exact message → exact keywords → fuzzy)
    /// </summary>
    private async Task<SymptomAnalysisResponse?> TryGetCachedQuestionAsync(
        string userMessage,
        List<ConversationMessage> history,
        Guid sessionId,
        int questionNumber)
    {
        try
        {
            // Tier 0: Exact normalized message match (fastest, most accurate)
            var normalizedMessage = _contextExtractor.NormalizeMessage(userMessage);
            if (!string.IsNullOrWhiteSpace(normalizedMessage))
            {
                var exactMessageMatch = await _questionCacheService.FindExactMessageMatchAsync(
                    normalizedMessage,
                    questionNumber);

                if (exactMessageMatch != null)
                {
                    await _questionCacheService.IncrementUsageAsync(exactMessageMatch.Id);
                    _logger.LogInformation(
                        "✅ Tier 0 Cache Hit: Exact message match for '{Message}' Q{Number}",
                        normalizedMessage,
                        questionNumber);
                    return CreateResponseFromCache(exactMessageMatch, sessionId, questionNumber);
                }
            }

            // Extract keywords with full conversation context
            var keywords = _contextExtractor.ExtractKeywordsWithContext(userMessage, history);

            if (string.IsNullOrEmpty(keywords))
            {
                _logger.LogDebug("No keywords extracted, skipping cache lookup");
                return null;
            }

            _logger.LogDebug(
                "Cache lookup: Message='{Message}', Keywords='{Keywords}', QuestionNumber={Number}",
                normalizedMessage,
                keywords,
                questionNumber);

            // Tier 1: Exact keywords match (~50ms)
            var exactMatch = await _questionCacheService.FindExactMatchAsync(keywords, questionNumber);

            if (exactMatch != null)
            {
                await _questionCacheService.IncrementUsageAsync(exactMatch.Id);
                return CreateResponseFromCache(exactMatch, sessionId, questionNumber);
            }

            // Tier 2: Fuzzy keywords match (~100ms)
            var fuzzyMatch = await _questionCacheService.FindFuzzyMatchAsync(
                keywords,
                questionNumber,
                threshold: 0.75); // 75% similarity

            if (fuzzyMatch != null)
            {
                await _questionCacheService.IncrementUsageAsync(fuzzyMatch.Id);
                return CreateResponseFromCache(fuzzyMatch, sessionId, questionNumber);
            }

            // Cache miss
            _logger.LogInformation(
                "❌ Cache miss for Message='{Message}', Keywords='{Keywords}' Q{Number} → Will call Groq",
                normalizedMessage,
                keywords,
                questionNumber);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache lookup, falling back to Groq");
            return null;
        }
    }

    /// <summary>
    /// Create response from cached question
    /// </summary>
    private SymptomAnalysisResponse CreateResponseFromCache(
        Models.Entities.SymptomQuestionCacheEntity cached,
        Guid sessionId,
        int questionNumber)
    {
        return new SymptomAnalysisResponse
        {
            SessionId = sessionId,
            Message = cached.Question,
            NextQuestions = new List<FollowUpQuestion>
            {
                new FollowUpQuestion
                {
                    Question = cached.Question,
                    Purpose = cached.Purpose ?? "Để xác định chính xác tình trạng của bạn",
                    Priority = cached.Priority ?? "MEDIUM"
                }
            },
            AnalysisComplete = false,
            QuestionCount = questionNumber,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Save question to cache (synchronous to avoid DbContext issues)
    /// Only saves if not already in cache
    /// </summary>
    private async Task SaveQuestionToCacheAsync(
        string userMessage,
        List<ConversationMessage> history,
        int questionNumber,
        SymptomAnalysisResponse response)
    {
        try
        {
            // Extract components
            var initialSymptom = _contextExtractor.ExtractInitialSymptom(
                history.FirstOrDefault()?.Content ?? userMessage);

            var contextKeywords = _contextExtractor.ExtractContextFromAnswers(history);
            var conversationContext = string.Join(", ", contextKeywords);

            var fullKeywords = _contextExtractor.ExtractKeywordsWithContext(
                userMessage,
                history);

            // Normalize message for Tier 0 exact matching
            var normalizedMessage = _contextExtractor.NormalizeMessage(userMessage);

            // Check if already exists in cache (check both message and keywords)
            var existingCache = !string.IsNullOrWhiteSpace(normalizedMessage)
                ? await _questionCacheService.FindExactMessageMatchAsync(normalizedMessage, questionNumber)
                : null;

            if (existingCache == null)
            {
                existingCache = await _questionCacheService.FindExactMatchAsync(fullKeywords, questionNumber);
            }

            if (existingCache != null)
            {
                _logger.LogDebug(
                    "⏭️ Skipping cache save: Question already exists for Message='{Message}', Keywords='{Keywords}' Q{Number}",
                    normalizedMessage,
                    fullKeywords,
                    questionNumber);
                return;
            }

            // Save to cache (only if not exists)
            await _questionCacheService.SaveQuestionAsync(
                initialSymptom: initialSymptom,
                conversationContext: conversationContext,
                normalizedKeywords: fullKeywords,
                normalizedMessage: normalizedMessage,
                questionNumber: questionNumber,
                question: response.Message,
                purpose: response.NextQuestions?.FirstOrDefault()?.Purpose,
                priority: response.NextQuestions?.FirstOrDefault()?.Priority,
                createdBy: "GROQ");

            _logger.LogInformation(
                "💾 Saved to cache: Message='{Message}', Initial='{Initial}', Context='{Context}', Q{Number}",
                normalizedMessage,
                initialSymptom,
                conversationContext,
                questionNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save question to cache (non-critical)");
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


    #region Cache Helper Methods


    /// <summary>
    /// Save conversation to database in background (fire-and-forget)
    /// </summary>
    private Task SaveConversationInBackgroundAsync(
        Guid sessionId,
        string userMessage,
        string aiMessage,
        LocationContext? location,
        Guid? userId,
        object? suggestions,
        object? disease,
        int questionCount,
        bool analysisComplete)
    {
        return Task.Run(async () =>
        {
            try
            {
                await _sessionService.SaveConversationHistoryAsync(
                    sessionId: sessionId,
                    userMessage: userMessage,
                    aiMessage: aiMessage,
                    location: location,
                    suggestions: suggestions,
                    userId: userId,
                    disease: disease,
                    questionCount: questionCount,
                    analysisComplete: analysisComplete);

                _logger.LogDebug("Successfully saved conversation for session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation for session {SessionId}", sessionId);
            }
        });
    }

    #endregion
}



