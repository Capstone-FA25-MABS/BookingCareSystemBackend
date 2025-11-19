using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Shared.Common.Models;
using Grpc.Core;

namespace BookingCare.Services.AI.Services.Implementations;

internal static class AiConversationRules
{
    public const int MaxQuestionsPerSession = 3;
}

/// <summary>
/// Main service for symptom analysis and doctor/hospital recommendations
/// </summary>
public class SymptomAnalysisService : ISymptomAnalysisService
{
    private readonly IGeminiService _geminiService;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    private readonly ILogger<SymptomAnalysisService> _logger;
    private readonly IConversationSessionService _conversationSessionService;

    // Cache for specialty mapping to avoid multiple API calls
    private static Dictionary<string, Guid>? _specialtyNameMapCache = null;
    private static List<SpecialtyDto>? _allSpecialtiesCache = null;
    private static Dictionary<string, SpecialtyDto>? _exactMatchDictCache = null;
    private static Dictionary<string, SpecialtyDto>? _normalizedMatchDictCache = null;
    private static DateTime _cacheLastUpdated = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30); // Cache for 30 minutes
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// Update dictionary caches (static helper)
    /// </summary>
    private static void UpdateDictionaryCaches(
        Dictionary<string, SpecialtyDto> exactMatchDict,
        Dictionary<string, SpecialtyDto> normalizedMatchDict)
    {
        _exactMatchDictCache = exactMatchDict;
        _normalizedMatchDictCache = normalizedMatchDict;
    }

    /// <summary>
    /// Update specialty cache (static helper)
    /// </summary>
    private static void UpdateSpecialtyCache(List<SpecialtyDto> specialties)
    {
        _allSpecialtiesCache = specialties;
        _cacheLastUpdated = DateTime.UtcNow;
        _exactMatchDictCache = null;
        _normalizedMatchDictCache = null;
    }

    public SymptomAnalysisService(
        IGeminiService geminiService,
        DoctorService.DoctorServiceClient doctorClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient,
        ILogger<SymptomAnalysisService> logger,
        IConversationSessionService conversationSessionService)
    {
        _geminiService = geminiService;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _logger = logger;
        _conversationSessionService = conversationSessionService;
    }

    /// <summary>
    /// Calculate maximum confidence from specialties and diseases
    /// </summary>
    private (double topConfidence, double maxConfidence) CalculateConfidenceValues(GeminiAnalysisResult result)
    {
        var topConfidence = result.RecommendedSpecialties
            .OrderByDescending(s => s.Confidence)
            .FirstOrDefault()?.Confidence ?? 0;

        var topDiseaseConfidence = result.PossibleDiseases
            .OrderByDescending(d => d.Confidence)
            .FirstOrDefault()?.Confidence ?? 0;

        var maxConfidence = Math.Max(topConfidence, topDiseaseConfidence);
        return (topConfidence, maxConfidence);
    }

    /// <summary>
    /// Add default question if missing
    /// </summary>
    private void AddDefaultQuestionIfMissing(GeminiAnalysisResult result, string question, string purpose)
    {
        if (!result.NextQuestions.Any())
        {
            result.NextQuestions.Add(new GeminiQuestion
            {
                Question = question,
                Purpose = purpose,
                Priority = "HIGH"
            });
        }
    }

    /// <summary>
    /// Rule 0: Validate first question - block early conclusion
    /// </summary>
    private bool ValidateFirstQuestion(GeminiAnalysisResult result, double maxConfidence, int questionsAskedCount)
    {
        if (questionsAskedCount != 0 || !result.AnalysisComplete)
        {
            return false;
        }

        _logger.LogWarning(
            "⛔ AI tried to conclude on FIRST question with confidence {Confidence}. BLOCKING! Forcing to ask more.",
            maxConfidence);

        result.AnalysisComplete = false;
        result.PossibleDiseases.Clear();
        result.RecommendedSpecialties.Clear();
        result.GeneralAdvice.Clear();

        AddDefaultQuestionIfMissing(result,
            "Để tư vấn chính xác, bạn có thể cho biết thêm về: vị trí chính xác, thời gian xuất hiện và mức độ của triệu chứng?",
            "Thu thập thông tin cơ bản để đánh giá");

        _logger.LogWarning("⛔ BLOCKED early conclusion on first question. Confidence: {Conf}", maxConfidence);
        return true;
    }

    /// <summary>
    /// Rule 1: Force to ask more if low confidence and questions < 3
    /// </summary>
    private void ForceAskMoreIfLowConfidence(GeminiAnalysisResult result, double maxConfidence, int questionsAskedCount)
    {
        if (!result.AnalysisComplete || maxConfidence >= 0.8 ||
            questionsAskedCount >= AiConversationRules.MaxQuestionsPerSession)
        {
            return;
        }

        _logger.LogWarning(
            "AI tried to conclude early with low confidence. MaxConfidence: {Confidence}, Questions: {Count}. Forcing to ask more.",
            maxConfidence, questionsAskedCount);

        result.AnalysisComplete = false;

        if (maxConfidence < 0.5)
        {
            result.PossibleDiseases.Clear();
            result.RecommendedSpecialties.Clear();
            result.GeneralAdvice.Clear();
        }

        AddDefaultQuestionIfMissing(result,
            "Bạn có thể mô tả thêm về thời gian xuất hiện và mức độ của triệu chứng không?",
            "Tăng độ tin cậy chẩn đoán");

        _logger.LogInformation("Forced AI to ask follow-up question. Question count: {Count}/{Limit}",
            questionsAskedCount,
            AiConversationRules.MaxQuestionsPerSession);
    }

    /// <summary>
    /// Rule 2: Add default question if analysis incomplete and no questions
    /// </summary>
    private void EnsureQuestionWhenIncomplete(GeminiAnalysisResult result)
    {
        if (result.AnalysisComplete || result.NextQuestions.Any())
        {
            return;
        }

        _logger.LogWarning("AI set analysisComplete = false but provided no nextQuestions. Adding default question.");

        AddDefaultQuestionIfMissing(result,
            "Bạn có thể cho biết thêm về các triệu chứng đi kèm hoặc yếu tố làm tăng/giảm triệu chứng không?",
            "Thu thập thêm thông tin để đánh giá chính xác");
    }

    /// <summary>
    /// Rule 3: Block conclusion if very low confidence
    /// </summary>
    private void BlockConclusionIfVeryLowConfidence(GeminiAnalysisResult result, double topConfidence, int questionsAskedCount)
    {
        if (topConfidence >= 0.5 ||
            questionsAskedCount >= AiConversationRules.MaxQuestionsPerSession ||
            !result.AnalysisComplete)
        {
            return;
        }

        _logger.LogWarning(
            "AI tried to conclude with very low confidence ({Confidence}) and only {Count} questions. Forcing to ask more.",
            topConfidence, questionsAskedCount);

        result.AnalysisComplete = false;

        AddDefaultQuestionIfMissing(result,
            "Để tư vấn chính xác hơn, bạn có thể cho biết triệu chứng xuất hiện khi nào và có yếu tố gì liên quan không?",
            "Khoanh vùng chính xác hơn");
    }

    /// <summary>
    /// Rule 4: Validate completeness when analysis is complete
    /// </summary>
    private void ValidateCompleteness(GeminiAnalysisResult result, int questionsAskedCount)
    {
        if (!result.AnalysisComplete)
        {
            return;
        }

        var hasEnoughDiseases = result.PossibleDiseases.Count >= 1;
        var hasEnoughAdvice = result.GeneralAdvice.Count >= 2;
        var hasSpecialties = result.RecommendedSpecialties.Any();

        _logger.LogInformation(
            "✅ Checking completion components: Diseases={DiseaseCount}, Advice={AdviceCount}, Specialties={SpecialtyCount}",
            result.PossibleDiseases.Count, result.GeneralAdvice.Count, result.RecommendedSpecialties.Count);

        if (hasEnoughDiseases && hasEnoughAdvice && hasSpecialties)
        {
            return;
        }

        _logger.LogWarning(
            "⚠️ AI marked as complete but missing components. Diseases: {Diseases}/{MinDiseases}, Advice: {Advice}/{MinAdvice}, Specialties: {Specialties}",
            result.PossibleDiseases.Count, 1, result.GeneralAdvice.Count, 2, hasSpecialties ? "YES" : "NO");

        if (questionsAskedCount < AiConversationRules.MaxQuestionsPerSession)
        {
            EnforceIncompleteComponents(result, hasEnoughDiseases, hasEnoughAdvice, hasSpecialties);
        }
        else
        {
            _logger.LogWarning("⚠️ Allowing incomplete conclusion because questions >= {Limit}. User may have vague symptoms.",
                AiConversationRules.MaxQuestionsPerSession);
        }
    }

    /// <summary>
    /// Enforce incomplete components by clearing data and adding question
    /// </summary>
    private void EnforceIncompleteComponents(GeminiAnalysisResult result, bool hasEnoughDiseases, bool hasEnoughAdvice, bool hasSpecialties)
    {
        _logger.LogWarning("⛔ ENFORCING: Incomplete components + questions < {Limit}. FORCING to ask more!",
            AiConversationRules.MaxQuestionsPerSession);
        result.AnalysisComplete = false;

        if (!hasEnoughDiseases) result.PossibleDiseases.Clear();
        if (!hasEnoughAdvice) result.GeneralAdvice.Clear();
        if (!hasSpecialties) result.RecommendedSpecialties.Clear();

        AddDefaultQuestionIfMissing(result,
            "Để đưa ra tư vấn đầy đủ, bạn có thể mô tả rõ hơn về: vị trí chính xác, thời gian xuất hiện và các triệu chứng đi kèm?",
            "Thu thập đủ thông tin để đưa ra kết luận đầy đủ");
    }

    /// <summary>
    /// Validate and fix AI response to ensure quality and consistency
    /// </summary>
    private void ValidateAndFixAIResponse(GeminiAnalysisResult result, int questionsAskedCount)
    {
        var (topConfidence, maxConfidence) = CalculateConfidenceValues(result);

        _logger.LogInformation(
            "Validating AI response: MaxConfidence={MaxConf}, QuestionsAsked={QCount}, AnalysisComplete={Complete}",
            maxConfidence, questionsAskedCount, result.AnalysisComplete);

        if (ValidateFirstQuestion(result, maxConfidence, questionsAskedCount))
        {
            return;
        }

        ForceAskMoreIfLowConfidence(result, maxConfidence, questionsAskedCount);
        EnsureQuestionWhenIncomplete(result);
        BlockConclusionIfVeryLowConfidence(result, topConfidence, questionsAskedCount);
        ValidateCompleteness(result, questionsAskedCount);

        _logger.LogInformation(
            "AI response validation completed. AnalysisComplete: {Complete}, Confidence: {Confidence}, Questions asked: {Count}/{Limit}, Next questions: {NextCount}",
            result.AnalysisComplete,
            topConfidence,
            questionsAskedCount,
            AiConversationRules.MaxQuestionsPerSession,
            result.NextQuestions.Count);
    }

    /// <summary>
    /// Load or create session and conversation history
    /// </summary>
    private async Task<(Guid sessionId, List<ConversationMessage> history)> LoadSessionAndHistoryAsync(SymptomAnalysisRequest request)
    {
        try
        {
            var sessionId = await _conversationSessionService.GetOrCreateSessionAsync(
                request.SessionId,
                request.UserId!.Value,
                request.Location
            );

            var history = await _conversationSessionService.LoadConversationHistoryAsync(
                sessionId,
                request.UserId!.Value);
            _logger.LogInformation("Session loaded: {SessionId}, History count: {Count}", sessionId, history.Count);

            return (sessionId, history);
        }
        catch (Exception dbEx)
        {
            _logger.LogWarning(dbEx, "Failed to load/create session from database, using fallback session ID");

            var sessionId = request.SessionId ?? Guid.NewGuid();
            var history = request.ConversationHistory?.Select(msg =>
            {
                // Timestamp is already a DateTime value type with default value, no need to check for null
                return new ConversationMessage
                {
                    Role = msg.Role,
                    Content = msg.Content,
                    Timestamp = msg.Timestamp
                };
            }).ToList() ?? new List<ConversationMessage>();

            return (sessionId, history);
        }
    }

    /// <summary>
    /// Add recommendations to response if analysis is complete
    /// </summary>
    private async Task AddRecommendationsIfNeededAsync(
        SymptomAnalysisResponse response,
        bool analysisComplete,
        List<SpecialtyMatch> specialtyMatches,
        LocationContext? location)
    {
        var shouldRecommend = analysisComplete && specialtyMatches.Any(s => s.SpecialtyId != null);
        if (!shouldRecommend)
        {
            response.Message = RemoveUnwantedSuggestionsQuestion(response.Message);
            return;
        }

        var specialtyIds = specialtyMatches
            .Where(s => s.SpecialtyId != null)
            .Select(s => s.SpecialtyId!.Value)
            .ToList();

        var doctorTask = GetDoctorRecommendationsAsync(specialtyIds, location, specialtyMatches);
        var hospitalTask = GetHospitalRecommendationsAsync(specialtyIds, location);

        await Task.WhenAll(doctorTask, hospitalTask);

        response.RecommendedDoctors = doctorTask.Result;
        response.RecommendedHospitals = hospitalTask.Result;

        UpdateMessageWithRecommendations(response);
    }

    /// <summary>
    /// Update response message with recommendations disclaimer
    /// </summary>
    private void UpdateMessageWithRecommendations(SymptomAnalysisResponse response)
    {
        response.Message = RemoveUnwantedSuggestionsQuestion(response.Message);

        if (response.RecommendedDoctors.Any() || response.RecommendedHospitals.Any())
        {
            response.Message += "\n\nLưu ý:\n\n*Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.*";
            response.Message += "\n\nDưới đây là gợi ý của mình về các bác sĩ và bệnh viện:";
        }
    }

    /// <summary>
    /// Save conversation history safely (non-blocking)
    /// </summary>
    private async Task SaveConversationHistorySafelyAsync(
        Guid sessionId,
        SymptomAnalysisRequest request,
        SymptomAnalysisResponse response)
    {
        try
        {
            object? suggestionsData = BuildSuggestionsData(response);
            await _conversationSessionService.SaveConversationHistoryAsync(
                sessionId,
                request.UserId!.Value,
                request.Message,
                response.Message,
                request.Location,
                suggestionsData
            );
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "Failed to save conversation history, but continuing with response");
        }
    }

    /// <summary>
    /// Build suggestions data for saving
    /// </summary>
    private object? BuildSuggestionsData(SymptomAnalysisResponse response)
    {
        if (!response.RecommendedDoctors.Any() && !response.RecommendedHospitals.Any())
        {
            return null;
        }

        return new
        {
            doctors = response.RecommendedDoctors.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                specialtyName = d.SpecialtyName,
                hospitalName = d.HospitalName,
                rating = d.Rating,
                yearOfExperience = d.YearOfExperience,
                serviceTypeName = d.ServiceTypeName,
                price = d.Price,
                avatarUrl = d.AvatarUrl
            }).ToList(),
            hospitals = response.RecommendedHospitals.Select(h => new
            {
                id = h.Id,
                name = h.Name,
                address = h.Address,
                specialtyNames = h.SpecialtyNames,
                imageUrl = h.ImageUrl
            }).ToList()
        };
    }

    /// <summary>
    /// Count questions (not conclusions) in conversation history
    /// </summary>
    private int CountQuestionsInHistory(List<ConversationMessage> history)
    {
        return history
            .Count(m => m.Role?.ToLower() == "ai" &&
                       m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) != true &&
                       m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) != true &&
                       m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) != true &&
                       (m.Content?.Contains("?") == true ||
                        m.Content?.Contains("cho tôi biết") == true ||
                        m.Content?.Contains("bạn có thể") == true));
    }

    /// <summary>
    /// Find last "tư vấn thêm" request index
    /// </summary>
    private int FindLastConsultMoreIndex(List<ConversationMessage> history)
    {
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if ((history[i].Role?.ToLower() == "patient" ||
                 history[i].Role?.ToLower() == "guest") &&
                (history[i].Content?.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true ||
                 history[i].Content?.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Count follow-up questions after "tư vấn thêm"
    /// </summary>
    private int CountFollowUpQuestions(List<ConversationMessage> history, int lastConsultMoreIndex)
    {
        if (lastConsultMoreIndex < 0)
        {
            return 0;
        }

        return history
            .Skip(lastConsultMoreIndex + 1)
            .Count(m => m.Role?.ToLower() == "ai" &&
                       m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) != true &&
                       m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) != true &&
                       m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) != true &&
                       (m.Content?.Contains("?") == true ||
                        m.Content?.Contains("cho tôi biết") == true ||
                        m.Content?.Contains("bạn có thể") == true));
    }

    /// <summary>
    /// Check if user is requesting more consultation
    /// </summary>
    private (bool isConsultMore, bool isFollowUp) CheckConsultationRequest(
        string message,
        List<ConversationMessage> history)
    {
        var hasPreviousSuggestions = history
            .Any(m => m.Role?.ToLower() == "ai" && m.Suggestions != null);

        var isConsultMore = hasPreviousSuggestions && (
            message.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase)
        );

        var isFollowUp = hasPreviousSuggestions && (
            message.Contains("hỏi thêm", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("giải thích thêm", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("tại sao", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("như thế nào", StringComparison.OrdinalIgnoreCase)
        );

        return (isConsultMore, isFollowUp);
    }

    /// <summary>
    /// Process follow-up questions logic
    /// </summary>
    private (bool analysisComplete, bool shouldAskMoreQuestions) ProcessFollowUpLogic(
        bool isConsultMoreRequest,
        bool isFollowUpQuestion,
        int followUpQuestionsCount,
        int questionsAskedCount,
        bool initialAnalysisComplete,
        GeminiAnalysisResult geminiResult)
    {
        var analysisComplete = initialAnalysisComplete;
        var shouldAskMoreQuestions = false;

        if (isConsultMoreRequest || isFollowUpQuestion)
        {
            if (followUpQuestionsCount >= AiConversationRules.MaxQuestionsPerSession)
            {
                _logger.LogInformation("Already asked {Count} follow-up questions, forcing analysis complete", followUpQuestionsCount);
                return (true, false);
            }

            _logger.LogInformation("User is asking follow-up questions ({Count}/{Limit})",
                followUpQuestionsCount,
                AiConversationRules.MaxQuestionsPerSession);

            if (!geminiResult.NextQuestions.Any() && isConsultMoreRequest)
            {
                _logger.LogInformation("User requested more consultation but AI has no questions");
            }

            return (false, true);
        }

        if (questionsAskedCount >= AiConversationRules.MaxQuestionsPerSession)
        {
            _logger.LogInformation("Already asked {Count} questions, forcing analysis complete", questionsAskedCount);
            return (true, false);
        }

        return (analysisComplete, shouldAskMoreQuestions);
    }

    /// <summary>
    /// Validate analysis completeness
    /// </summary>
    private (bool isComplete, List<string> missingComponents) ValidateAnalysisCompleteness(
        GeminiAnalysisResult geminiResult,
        List<GeminiSpecialty> filteredSpecialties)
    {
        var missingComponents = new List<string>();

        if (!geminiResult.PossibleDiseases.Any())
            missingComponents.Add("possibleDiseases");
        if (!filteredSpecialties.Any())
            missingComponents.Add("recommendedSpecialties");
        if (!geminiResult.GeneralAdvice.Any())
            missingComponents.Add("generalAdvice");

        return (!missingComponents.Any(), missingComponents);
    }

    /// <summary>
    /// Add default follow-up question if needed
    /// </summary>
    private void EnsureFollowUpQuestion(GeminiAnalysisResult geminiResult)
    {
        if (!geminiResult.NextQuestions.Any())
        {
            geminiResult.NextQuestions.Add(new GeminiQuestion
            {
                Question = "Bạn có thể mô tả thêm chi tiết về triệu chứng của mình không?",
                Purpose = "Thu thập thêm thông tin để đưa ra kết luận chính xác",
                Priority = "HIGH"
            });
        }
    }

    /// <summary>
    /// Build response message
    /// </summary>
    private string BuildResponseMessage(
        bool shouldAskMoreQuestions,
        GeminiAnalysisResult geminiResult)
    {
        string message;

        // CRITICAL: Check analysisComplete first (after validation, this is the source of truth)
        // If analysis is NOT complete, we MUST ask questions, not give conclusions
        if (!geminiResult.AnalysisComplete || shouldAskMoreQuestions)
        {
            // Return ONLY the question, no conclusions
            if (geminiResult.NextQuestions.Any())
            {
                message = geminiResult.NextQuestions.First().Question;
            }
            else
            {
                // Fallback: If validation should have added a question but didn't
                _logger.LogWarning("analysisComplete = false but no nextQuestions! Returning default question.");
                message = "Bạn có thể cho biết thêm về vị trí, thời gian xuất hiện và mức độ của triệu chứng không?";
            }
        }
        else
        {
            // Only build full conclusion if analysisComplete = true
            message = BuildAIMessage(geminiResult);
        }

        // Normalize line breaks before returning (to match DB format and avoid extra spacing)
        return NormalizeLineBreaks(message);
    }

    /// <summary>
    /// Get next questions based on analysis state
    /// </summary>
    private List<FollowUpQuestion> GetNextQuestions(bool shouldAskMoreQuestions, GeminiAnalysisResult geminiResult)
    {
        if (!shouldAskMoreQuestions || !geminiResult.NextQuestions.Any())
        {
            return new List<FollowUpQuestion>();
        }

        // Extract priority calculation to avoid nested ternary
        var prioritizedQuestions = geminiResult.NextQuestions
            .OrderByDescending(q => GetQuestionPriority(q.Priority))
            .Take(1)
            .Select(q => new FollowUpQuestion
            {
                Question = q.Question,
                Purpose = q.Purpose,
                Priority = q.Priority
            })
            .ToList();

        return prioritizedQuestions;
    }

    /// <summary>
    /// Get numeric priority value for question priority
    /// </summary>
    private int GetQuestionPriority(string priority)
    {
        return priority switch
        {
            "HIGH" => 3,
            "MEDIUM" => 2,
            _ => 1
        };
    }

    public async Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Starting symptom analysis for message: {Message}", request.Message);

            // Validate authentication
            if (!request.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated to use AI chat service.");
            }

            // Step 0: Load session and history
            var (sessionId, conversationHistory) = await LoadSessionAndHistoryAsync(request);

            // Step 1: Call Gemini AI
            var geminiResponse = await _geminiService.AnalyzeSymptomsAsync(request.Message, conversationHistory);
            var geminiResult = _geminiService.ParseGeminiResponse(geminiResponse);

            // Step 2: Validate and fix AI response nếu cần
            var questionsAskedBeforeValidation = CountQuestionsInHistory(conversationHistory);
            ValidateAndFixAIResponse(geminiResult, questionsAskedBeforeValidation);

            // Step 3: Analyze conversation state
            var topSpecialtyConfidence = geminiResult.RecommendedSpecialties
                .OrderByDescending(s => s.Confidence)
                .FirstOrDefault()?.Confidence ?? 0;

            var questionsAskedCount = questionsAskedBeforeValidation;
            var (isConsultMoreRequest, isFollowUpQuestion) =
                CheckConsultationRequest(request.Message, conversationHistory);

            var lastConsultMoreIndex = FindLastConsultMoreIndex(conversationHistory);
            var followUpQuestionsCount = CountFollowUpQuestions(conversationHistory, lastConsultMoreIndex);

            // Determine initial analysis state
            var initialAnalysisComplete = ApplyConfidenceThresholds(
                geminiResult,
                topSpecialtyConfidence,
                out var filteredSpecialties,
                out var shouldAskMoreQuestions
            );

            // Process follow-up logic
            var (analysisComplete, shouldAskMore) = ProcessFollowUpLogic(
                isConsultMoreRequest,
                isFollowUpQuestion,
                followUpQuestionsCount,
                questionsAskedCount,
                initialAnalysisComplete,
                geminiResult
            );
            shouldAskMoreQuestions = shouldAskMore;

            if (questionsAskedCount < AiConversationRules.MaxQuestionsPerSession)
            {
                analysisComplete = false;
                shouldAskMoreQuestions = true;
                EnsureFollowUpQuestion(geminiResult);
            }

            // Step 4: Validate completeness if analysis is complete
            if (analysisComplete)
            {
                var (isComplete, missingComponents) = ValidateAnalysisCompleteness(geminiResult, filteredSpecialties);

                if (!isComplete)
                {
                    _logger.LogWarning("AI returned incomplete conclusion. Missing: {Components}",
                        string.Join(", ", missingComponents));

                    analysisComplete = false;
                    shouldAskMoreQuestions = true;
                    EnsureFollowUpQuestion(geminiResult);
                }
            }

            // Step 5: Create base response
            _logger.LogInformation(
                "Building response message: analysisComplete={Complete}, shouldAskMore={ShouldAsk}, NextQuestionsCount={QCount}",
                geminiResult.AnalysisComplete, shouldAskMoreQuestions, geminiResult.NextQuestions.Count);

            var baseMessage = BuildResponseMessage(shouldAskMoreQuestions, geminiResult);

            _logger.LogInformation("Built message preview: {Preview}",
                baseMessage.Length > 100 ? baseMessage.Substring(0, 100) + "..." : baseMessage);

            // Remove unwanted question about suggesting doctors/hospitals from AI response
            // This question should not appear in follow-up conversations
            baseMessage = RemoveUnwantedSuggestionsQuestion(baseMessage);

            var response = new SymptomAnalysisResponse
            {
                SessionId = sessionId,
                Message = baseMessage,
                PossibleDiseases = geminiResult.PossibleDiseases.Select(d => new DiseaseMatch
                {
                    Name = d.Name,
                    Confidence = d.Confidence,
                    Description = d.Description
                }).ToList(),
                NextQuestions = GetNextQuestions(shouldAskMoreQuestions, geminiResult),
                GeneralAdvice = geminiResult.GeneralAdvice,
                AnalysisComplete = analysisComplete
            };

            // Step 5: Map specialties from Gemini to DB
            if (analysisComplete && filteredSpecialties.Any())
            {
                var specialtyMatches = await MapSpecialtiesToDbAsync(filteredSpecialties);
                response.RecommendedSpecialties = specialtyMatches;

                // Step 6: Get recommendations nếu cần
                await AddRecommendationsIfNeededAsync(response, true, specialtyMatches, request.Location);
            }
            else
            {
                response.RecommendedSpecialties = new List<SpecialtyMatch>();
            }

            // Step 7: Save conversation history
            await SaveConversationHistorySafelyAsync(sessionId, request, response);

            _logger.LogInformation("Symptom analysis completed. Recommendations: {DoctorCount} doctors, {HospitalCount} hospitals",
                response.RecommendedDoctors.Count, response.RecommendedHospitals.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException?.Message);
            throw new SymptomAnalysisException($"Failed to analyze symptoms: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Apply confidence thresholds to determine if analysis is complete and filter specialties
    /// </summary>
    private bool ApplyConfidenceThresholds(
        GeminiAnalysisResult geminiResult,
        double topSpecialtyConfidence,
        out List<GeminiSpecialty> filteredSpecialties,
        out bool shouldAskMoreQuestions)
    {
        filteredSpecialties = new List<GeminiSpecialty>();
        shouldAskMoreQuestions = false;

        // Confidence > 0.8: Recommend 1 specialty immediately
        if (topSpecialtyConfidence > 0.8)
        {
            filteredSpecialties = geminiResult.RecommendedSpecialties
                .Where(s => s.Confidence > 0.8)
                .OrderByDescending(s => s.Confidence)
                .Take(1)
                .ToList();
            return true;
        }

        // Confidence 0.5-0.8: Show 2-3 specialty options
        if (topSpecialtyConfidence >= 0.5)
        {
            filteredSpecialties = geminiResult.RecommendedSpecialties
                .Where(s => s.Confidence >= 0.5)
                .OrderByDescending(s => s.Confidence)
                .Take(3)
                .ToList();
            return true;
        }

        // Confidence < 0.5: Ask more questions
        shouldAskMoreQuestions = true;
        return false;
    }

    /// <summary>
    /// Append possible diseases to message
    /// </summary>
    private void AppendPossibleDiseases(StringBuilder sb, List<GeminiDisease> diseases)
    {
        if (diseases.Any())
        {
            sb.AppendLine("Dựa trên các triệu chứng bạn mô tả, có thể liên quan đến:");
            foreach (var disease in diseases.OrderByDescending(d => d.Confidence).Take(5))
            {
                var confidencePercent = (int)(disease.Confidence * 100);
                sb.AppendLine($"- {disease.Name} (độ tin cậy: {confidencePercent}%)");
            }
            sb.AppendLine();
        }
        else
        {
            _logger.LogWarning("BuildAIMessage called but no possible diseases provided");
            sb.AppendLine("Dựa trên các triệu chứng bạn mô tả, tôi cần thêm thông tin để đưa ra đánh giá chính xác.");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Append general advice to message
    /// </summary>
    private void AppendGeneralAdvice(StringBuilder sb, List<string> advice)
    {
        if (advice.Any())
        {
            sb.AppendLine("**Lời khuyên chung**:");
            foreach (var item in advice)
            {
                sb.AppendLine($"• {item}");
            }
            sb.AppendLine();
        }
        else
        {
            _logger.LogWarning("BuildAIMessage called but no general advice provided");
            sb.AppendLine("**Lời khuyên chung**:");
            sb.AppendLine("• Theo dõi triệu chứng của bạn");
            sb.AppendLine("• Nếu triệu chứng trở nên nghiêm trọng hơn, hãy đến cơ sở y tế");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Append recommended specialties to message
    /// </summary>
    private void AppendRecommendedSpecialties(StringBuilder sb, List<GeminiSpecialty> specialties)
    {
        if (specialties.Any())
        {
            // Sort by confidence descending
            var sortedSpecialties = specialties.OrderByDescending(s => s.Confidence).ToList();

            // If only 1 specialty
            if (sortedSpecialties.Count == 1)
            {
                sb.AppendLine($"**Chuyên khoa phù hợp**: {sortedSpecialties[0].SpecialtyName}");
            }
            else
            {
                // Multiple specialties - format as list
                sb.AppendLine("**Chuyên khoa phù hợp**:");
                foreach (var specialty in sortedSpecialties)
                {
                    sb.AppendLine($"- {specialty.SpecialtyName} (độ tin cậy: {specialty.Confidence:P0})");
                }
            }
            sb.AppendLine();
        }
        else
        {
            _logger.LogWarning("BuildAIMessage called but no recommended specialties provided");
            sb.AppendLine("**Chuyên khoa phù hợp**: Nội tổng quát");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Append next questions prompt if needed
    /// </summary>
    private void AppendNextQuestionsPrompt(StringBuilder sb, bool analysisComplete, List<GeminiQuestion> questions)
    {
        if (!analysisComplete && questions.Any())
        {
            sb.AppendLine("Để tư vấn chính xác hơn, bạn có thể cho tôi biết thêm:");
        }
    }

    private string BuildAIMessage(GeminiAnalysisResult result)
    {
        var sb = new StringBuilder();

        AppendPossibleDiseases(sb, result.PossibleDiseases);
        AppendGeneralAdvice(sb, result.GeneralAdvice);
        AppendRecommendedSpecialties(sb, result.RecommendedSpecialties);
        AppendNextQuestionsPrompt(sb, result.AnalysisComplete, result.NextQuestions);

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Get or build specialty lookup dictionaries
    /// </summary>
    private (Dictionary<string, SpecialtyDto> exactMatchDict, Dictionary<string, SpecialtyDto> normalizedMatchDict)
        GetOrBuildSpecialtyDictionaries(List<SpecialtyDto> allSpecialties)
    {
        lock (_cacheLock)
        {
            // Check if dictionaries are cached and still valid
            if (_exactMatchDictCache != null &&
                _normalizedMatchDictCache != null &&
                _cacheLastUpdated != DateTime.MinValue &&
                DateTime.UtcNow - _cacheLastUpdated < CacheExpiration)
            {
                return (_exactMatchDictCache, _normalizedMatchDictCache);
            }

            // Build dictionaries
            var exactMatchDict = new Dictionary<string, SpecialtyDto>(StringComparer.OrdinalIgnoreCase);
            var normalizedMatchDict = new Dictionary<string, SpecialtyDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var specialty in allSpecialties)
            {
                // Add exact match (case-insensitive)
                if (!exactMatchDict.ContainsKey(specialty.Name))
                {
                    exactMatchDict[specialty.Name] = specialty;
                }

                // Add normalized match
                var normalizedName = NormalizeSpecialtyName(specialty.Name);
                if (!string.IsNullOrWhiteSpace(normalizedName) && !normalizedMatchDict.ContainsKey(normalizedName))
                {
                    normalizedMatchDict[normalizedName] = specialty;
                }
            }

            // Cache dictionaries
            UpdateDictionaryCaches(exactMatchDict, normalizedMatchDict);
            return (exactMatchDict, normalizedMatchDict);
        }
    }

    /// <summary>
    /// Find specialty match for a Gemini specialty
    /// </summary>
    private SpecialtyMatch? FindSpecialtyMatch(
        GeminiSpecialty geminiSpecialty,
        Dictionary<string, SpecialtyDto> exactMatchDict,
        Dictionary<string, SpecialtyDto> normalizedMatchDict)
    {
        Guid? specialtyId = null;
        string? matchedSpecialtyName = null;

        // Try exact match with original name (case-insensitive) - O(1) lookup
        if (exactMatchDict.TryGetValue(geminiSpecialty.SpecialtyName, out var exactMatch))
        {
            specialtyId = exactMatch.Id;
            matchedSpecialtyName = exactMatch.Name;
            _logger.LogDebug("Exact match found for specialty: {GeminiName} -> {DbName} (ID: {Id})",
                geminiSpecialty.SpecialtyName, matchedSpecialtyName, exactMatch.Id);
        }
        else
        {
            // Try normalized match as fallback - O(1) lookup
            var normalizedGeminiName = NormalizeSpecialtyName(geminiSpecialty.SpecialtyName);
            if (!string.IsNullOrWhiteSpace(normalizedGeminiName) &&
                normalizedMatchDict.TryGetValue(normalizedGeminiName, out var normalizedMatch))
            {
                specialtyId = normalizedMatch.Id;
                matchedSpecialtyName = normalizedMatch.Name;
                _logger.LogDebug("Normalized match found for specialty: {GeminiName} -> {DbName} (ID: {Id})",
                    geminiSpecialty.SpecialtyName, matchedSpecialtyName, normalizedMatch.Id);
            }
            else
            {
                _logger.LogWarning("No match found for specialty: {GeminiName}. Only mapping specialties that exist in database.",
                    geminiSpecialty.SpecialtyName);
            }
        }

        // Only return if we found a match (specialtyId is not null)
        if (specialtyId.HasValue)
        {
            return new SpecialtyMatch
            {
                SpecialtyId = specialtyId,
                SpecialtyName = matchedSpecialtyName ?? geminiSpecialty.SpecialtyName,
                Confidence = geminiSpecialty.Confidence,
                Urgency = geminiSpecialty.Urgency,
                Reasons = geminiSpecialty.Reasons
            };
        }

        return null;
    }

    private async Task<List<SpecialtyMatch>> MapSpecialtiesToDbAsync(List<GeminiSpecialty> geminiSpecialties)
    {
        var result = new List<SpecialtyMatch>();

        try
        {
            // Get all specialties from cache or API (with caching)
            var allSpecialties = await GetAllSpecialtiesCachedAsync();

            if (!allSpecialties.Any())
            {
                _logger.LogWarning("No specialties available for mapping");
                return result;
            }

            // Get or build lookup dictionaries from cache for O(1) lookup instead of O(n) FirstOrDefault
            // This is much faster when we have many specialties
            var (exactMatchDict, normalizedMatchDict) = GetOrBuildSpecialtyDictionaries(allSpecialties);

            // Now do fast dictionary lookups
            foreach (var geminiSpecialty in geminiSpecialties)
            {
                var specialtyMatch = FindSpecialtyMatch(
                    geminiSpecialty,
                    exactMatchDict,
                    normalizedMatchDict);

                if (specialtyMatch != null)
                {
                    result.Add(specialtyMatch);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping specialties");
        }

        return result;
    }

    private async Task<List<SpecialtyDto>> GetAllSpecialtiesCachedAsync()
    {
        lock (_cacheLock)
        {
            // Check if cache is valid
            if (_allSpecialtiesCache != null &&
                _cacheLastUpdated != DateTime.MinValue &&
                DateTime.UtcNow - _cacheLastUpdated < CacheExpiration)
            {
                _logger.LogDebug("Using cached specialties (last updated: {Time})", _cacheLastUpdated);
                return _allSpecialtiesCache;
            }
        }

        // Cache expired or not available, fetch from API
        var specialties = await GetAllSpecialtiesAsync();

        lock (_cacheLock)
        {
            UpdateSpecialtyCache(specialties);
            _logger.LogInformation("Updated specialty cache with {Count} specialties", specialties.Count);
        }

        return specialties;
    }

    private Dictionary<string, Guid> GetDefaultSpecialtyMap()
    {
        // This should be replaced with actual DB data or API calls
        // For now, return empty - actual specialty IDs should come from the database
        _logger.LogWarning("Using default specialty map - this should be replaced with actual DB queries");
        return new Dictionary<string, Guid>();
    }

    private string NormalizeSpecialtyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.ToLowerInvariant()
            .Replace("khoa ", "")
            .Replace("chuyên khoa ", "")
            .Replace("cơ ", "") // Remove "cơ" prefix (e.g., "Cơ Xương Khớp" -> "Xương Khớp")
            .Trim();

        return normalized;
    }

    private async Task<List<SpecialtyDto>> GetAllSpecialtiesAsync()
    {
        try
        {
            _logger.LogDebug("Fetching all specialties via gRPC");

            var request = new BookingCare.Services.Doctor.Protos.GetAllSpecialtiesRequest();
            var response = await _doctorClient.GetAllSpecialtiesAsync(request);

            if (response?.Specialties != null)
            {
                var specialties = response.Specialties.Select(s => new SpecialtyDto
                {
                    Id = Guid.Parse(s.Id),
                    Name = s.Name,
                    ImageUrl = s.ImageUrl
                }).ToList();

                _logger.LogDebug("Fetched {Count} specialties via gRPC", specialties.Count);
                return specialties;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all specialties via gRPC for fuzzy matching");
        }

        return new List<SpecialtyDto>();
    }

    private (Guid Id, string Name, double SimilarityScore)? FindBestSpecialtyMatch(string normalizedGeminiName, List<SpecialtyDto> allSpecialties)
    {
        if (allSpecialties == null || !allSpecialties.Any())
            return null;

        var bestMatch = allSpecialties
            .Select(s => new
            {
                Specialty = s,
                NormalizedName = NormalizeSpecialtyName(s.Name),
                Similarity = CalculateSimilarity(normalizedGeminiName, NormalizeSpecialtyName(s.Name))
            })
            .Where(x => x.Similarity > 0.5) // Minimum similarity threshold
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();

        return bestMatch != null
            ? (bestMatch.Specialty.Id, bestMatch.Specialty.Name, bestMatch.Similarity)
            : null;
    }

    /// <summary>
    /// Calculate contains-based similarity
    /// </summary>
    private double CalculateContainsSimilarity(string str1, string str2)
    {
        if (str1.Contains(str2) || str2.Contains(str1))
        {
            var longer = str1.Length > str2.Length ? str1 : str2;
            var shorter = str1.Length > str2.Length ? str2 : str1;
            return (double)shorter.Length / longer.Length;
        }
        return 0.0;
    }

    /// <summary>
    /// Calculate word-based similarity
    /// </summary>
    private double CalculateWordSimilarity(string str1, string str2)
    {
        var words1 = str1.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var words2 = str2.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        var matchingWords = words1.Count(w1 => words2.Any(w2 => w1 == w2 || w1.Contains(w2) || w2.Contains(w1)));
        var totalWords = Math.Max(words1.Length, words2.Length);

        return totalWords == 0 ? 0.0 : (double)matchingWords / totalWords;
    }

    /// <summary>
    /// Calculate character-level similarity using Levenshtein distance
    /// </summary>
    private double CalculateCharacterSimilarity(string str1, string str2)
    {
        var levenshteinDistance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        return maxLength > 0 ? 1.0 - ((double)levenshteinDistance / maxLength) : 0.0;
    }

    private double CalculateSimilarity(string str1, string str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2))
            return 0.0;

        // Exact match
        if (str1 == str2)
            return 1.0;

        // Contains match
        var containsSimilarity = CalculateContainsSimilarity(str1, str2);
        if (containsSimilarity > 0)
            return containsSimilarity;

        // Word-based and character-level similarity
        var wordSimilarity = CalculateWordSimilarity(str1, str2);
        var charSimilarity = CalculateCharacterSimilarity(str1, str2);

        // Combine word and character similarity (weighted)
        return (wordSimilarity * 0.6) + (charSimilarity * 0.4);
    }

    private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // Initialize first column
        for (int i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        // Initialize first row
        for (int j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Build filter request for doctors
    /// </summary>
    private BookingCare.Services.Doctor.Protos.FilterDoctorsForRecommendationRequest BuildDoctorFilterRequest(
        List<Guid> specialtyIds,
        LocationContext? location)
    {
        var request = new BookingCare.Services.Doctor.Protos.FilterDoctorsForRecommendationRequest
        {
            MaxResults = 10 // Get top 10, then rank and take top 5
        };
        request.SpecialtyIds.AddRange(specialtyIds.Select(id => id.ToString()));

        if (location != null)
        {
            if (!string.IsNullOrWhiteSpace(location.ProvinceId))
            {
                request.ProvinceId = location.ProvinceId;
            }
            if (!string.IsNullOrWhiteSpace(location.DistrictId))
            {
                request.DistrictId = location.DistrictId;
            }
        }

        return request;
    }

    /// <summary>
    /// Get hospital addresses for location matching
    /// </summary>
    private async Task<Dictionary<string, string>> GetHospitalAddressesAsync(
        List<string> hospitalIds,
        LocationContext? location)
    {
        var hospitalAddressMap = new Dictionary<string, string>();

        if (!hospitalIds.Any() || location == null)
        {
            return hospitalAddressMap;
        }

        try
        {
            var hospitalRequest = new BookingCare.Services.Hospital.GetHospitalsBasicInfoRequest();
            hospitalRequest.Ids.AddRange(hospitalIds);

            // Add timeout (5 seconds) to prevent hanging
            var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
            var hospitalResponse = await _hospitalClient.GetHospitalsBasicInfoAsync(hospitalRequest, callOptions);

            foreach (var hospital in hospitalResponse.Hospitals)
            {
                if (!string.IsNullOrWhiteSpace(hospital.Address))
                {
                    hospitalAddressMap[hospital.Id] = hospital.Address;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching hospital addresses for location matching");
        }

        return hospitalAddressMap;
    }

    /// <summary>
    /// Convert doctor proto to recommendation
    /// </summary>
    private DoctorRecommendation ConvertToDoctorRecommendation(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor,
        List<SpecialtyMatch> specialtyMatches,
        LocationContext? location,
        Dictionary<string, string> hospitalAddressMap)
    {
        var address = hospitalAddressMap.TryGetValue(doctor.HospitalId, out var addr) ? addr : null;

        return new DoctorRecommendation
        {
            Id = doctor.Id,
            Name = doctor.FullName,
            SpecialtyName = doctor.SpecialtyName,
            HospitalName = doctor.HospitalName,
            Rating = doctor.Rating,
            YearOfExperience = doctor.YearsOfExperience,
            ServiceTypeName = doctor.ServiceTypeName ?? "Khám chuyên khoa",
            Price = doctor.ConsultationFee > 0 ? $"{doctor.ConsultationFee:N0} VNĐ" : null,
            RecommendationScore = CalculateDoctorScore(doctor, specialtyMatches, location, address),
            AvatarUrl = !string.IsNullOrWhiteSpace(doctor.AvatarUrl) ? doctor.AvatarUrl : null
        };
    }

    private async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(
        List<Guid> specialtyIds,
        LocationContext? location,
        List<SpecialtyMatch> specialtyMatches)
    {
        try
        {
            var request = BuildDoctorFilterRequest(specialtyIds, location);
            var grpcResponse = await _doctorClient.FilterDoctorsForRecommendationAsync(request);

            if (grpcResponse.Doctors == null || !grpcResponse.Doctors.Any())
            {
                _logger.LogWarning("No doctors found for specialties: {SpecialtyIds}", string.Join(", ", specialtyIds));
                return new List<DoctorRecommendation>();
            }

            // Get hospital IDs
            var hospitalIds = grpcResponse.Doctors
                .Where(d => !string.IsNullOrWhiteSpace(d.HospitalId))
                .Select(d => d.HospitalId)
                .Distinct()
                .ToList();

            // Get hospital addresses
            var hospitalAddressMap = await GetHospitalAddressesAsync(hospitalIds, location);

            // Convert doctors to recommendations
            var allDoctors = grpcResponse.Doctors
                .Select(d => ConvertToDoctorRecommendation(d, specialtyMatches, location, hospitalAddressMap))
                .ToList();

            // Group by specialty and take top 5 per specialty to ensure diversity
            // This ensures each recommended specialty gets representation
            var doctorsPerSpecialty = allDoctors
                .GroupBy(d => d.SpecialtyName)
                .SelectMany(group => group
                    .OrderByDescending(d => d.RecommendationScore)
                    .Take(5) // Top 5 doctors per specialty
                )
                .OrderByDescending(d => d.RecommendationScore)
                .Take(10) // Max 10 doctors total (2-3 specialties × 5 doctors)
                .ToList();

            _logger.LogInformation("Selected {Count} doctors across {SpecialtyCount} specialties",
                doctorsPerSpecialty.Count,
                doctorsPerSpecialty.Select(d => d.SpecialtyName).Distinct().Count());

            return doctorsPerSpecialty;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error fetching doctor recommendations: {Status}", ex.Status);
            return new List<DoctorRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching doctor recommendations");
            return new List<DoctorRecommendation>();
        }
    }

    /// <summary>
    /// Calculate hospital address score for doctor
    /// </summary>
    private double CalculateHospitalAddressScore(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor)
    {
        bool hasHospitalAddress = !string.IsNullOrWhiteSpace(doctor.HospitalName) &&
                                  !string.IsNullOrWhiteSpace(doctor.HospitalId);
        return hasHospitalAddress ? 0.30 : 0.05;
    }

    /// <summary>
    /// Calculate location matching score for doctor
    /// </summary>
    private double CalculateLocationScore(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor,
        LocationContext? location,
        string? hospitalAddress)
    {
        if (location == null)
        {
            return 0.05; // No location preference
        }

        // Check exact address match
        if (!string.IsNullOrWhiteSpace(hospitalAddress) && !string.IsNullOrWhiteSpace(location.DisplayName))
        {
            bool locationMatch = IsAddressInLocation(hospitalAddress, location.DisplayName);
            if (locationMatch)
            {
                _logger.LogDebug("Doctor {DoctorId} hospital address matches location: {Address} matches {Location}",
                    doctor.Id, hospitalAddress, location.DisplayName);
                return 0.50; // HIGHEST priority for location match
            }
            else
            {
                _logger.LogDebug("Doctor {DoctorId} hospital address does NOT match location: {Address} vs {Location}",
                    doctor.Id, hospitalAddress, location.DisplayName);
                return 0.05;
            }
        }

        // Fallback to district/province matching
        if (!string.IsNullOrEmpty(location.DistrictId))
        {
            return 0.20; // Medium priority for district match
        }

        if (!string.IsNullOrEmpty(location.ProvinceId))
        {
            return 0.15; // Lower priority for province match
        }

        return 0.05; // No location data
    }

    /// <summary>
    /// Calculate specialty matching score
    /// </summary>
    private double CalculateSpecialtyScore(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor,
        List<SpecialtyMatch> specialtyMatches)
    {
        var specialtyMatch = specialtyMatches.FirstOrDefault(s =>
            s.SpecialtyName.Equals(doctor.SpecialtyName, StringComparison.OrdinalIgnoreCase));

        return specialtyMatch != null ? 0.10 * specialtyMatch.Confidence : 0.02;
    }

    private double CalculateDoctorScore(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor,
        List<SpecialtyMatch> specialtyMatches,
        LocationContext? location,
        string? hospitalAddress = null)
    {
        double score = 0;

        // Hospital address score (30%)
        score += CalculateHospitalAddressScore(doctor);

        // Location matching score (50%)
        score += CalculateLocationScore(doctor, location, hospitalAddress);

        // Specialty match score (10%)
        score += CalculateSpecialtyScore(doctor, specialtyMatches);

        // Rating score (5%)
        score += 0.05 * Math.Min(doctor.Rating / 5.0, 1.0);

        // Experience score (5%)
        score += 0.05 * Math.Min(doctor.YearsOfExperience / 30.0, 1.0);

        return Math.Round(score, 3);
    }

    /// <summary>
    /// Check if hospital/doctor address matches user location
    /// </summary>
    private bool IsAddressInLocation(string address, string locationDisplayName)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(locationDisplayName))
            return false;

        // Normalize both strings for comparison
        var normalizedAddress = address.ToLowerInvariant().Trim();
        var normalizedLocation = locationDisplayName.ToLowerInvariant().Trim();

        // Extract province/city name from DisplayName
        // Examples: "Thành phố Hà Nội" -> "hà nội", "Tỉnh Hải Dương" -> "hải dương"
        var locationName = normalizedLocation
            .Replace("thành phố", "")
            .Replace("tp.", "")
            .Replace("tp ", "")
            .Replace("tỉnh", "")
            .Replace("t.", "")
            .Trim();

        // Check if address contains the location name
        // Also check for common variations
        var locationVariations = new List<string> { locationName, normalizedLocation };

        // Add variations like "hà nội", "ha noi" (without diacritics would need more complex logic)
        // For now, just check direct match and normalized location
        foreach (var variation in locationVariations)
        {
            if (!string.IsNullOrWhiteSpace(variation) && normalizedAddress.Contains(variation))
            {
                _logger.LogDebug("Address match found: '{Address}' contains '{Variation}'", address, variation);
                return true;
            }
        }

        // Also check common city names that might appear in addresses
        // Example: "Hà Nội" might appear as "Hanoi" in some addresses, but we'll focus on Vietnamese
        // For now, return false if no match found
        return false;
    }

    /// <summary>
    /// Get specialty names for mapping
    /// </summary>
    private async Task<Dictionary<Guid, string>> GetSpecialtyNamesAsync(List<Guid> specialtyIds)
    {
        var specialtyNameMap = new Dictionary<Guid, string>();

        if (!specialtyIds.Any())
        {
            return specialtyNameMap;
        }

        try
        {
            var specialtiesRequest = new GetSpecialtiesByIdsRequest
            {
                Ids = { specialtyIds.Select(id => id.ToString()) }
            };
            var specialtiesResponse = await _doctorClient.GetSpecialtiesByIdsAsync(specialtiesRequest);

            foreach (var specialty in specialtiesResponse.Specialties)
            {
                if (Guid.TryParse(specialty.Id, out var specialtyGuid))
                {
                    specialtyNameMap[specialtyGuid] = specialty.Name;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching specialty names via gRPC");
        }

        return specialtyNameMap;
    }

    /// <summary>
    /// Get hospitals by specialties via gRPC
    /// </summary>
    private async Task<Dictionary<string, HospitalDto>> GetHospitalsBySpecialtiesAsync(
        List<Guid> specialtyIds,
        Dictionary<Guid, string> specialtyNameMap)
    {
        var allHospitals = new Dictionary<string, HospitalDto>();

        foreach (var specialtyId in specialtyIds)
        {
            await ProcessSpecialtyHospitalsAsync(specialtyId, specialtyNameMap, allHospitals);
        }

        return allHospitals;
    }

    /// <summary>
    /// Process hospitals for a single specialty
    /// </summary>
    private async Task ProcessSpecialtyHospitalsAsync(
        Guid specialtyId,
        Dictionary<Guid, string> specialtyNameMap,
        Dictionary<string, HospitalDto> allHospitals)
    {
        try
        {
            var hospitals = await FetchHospitalsBySpecialtyAsync(specialtyId);
            ProcessHospitalResults(hospitals, specialtyId, specialtyNameMap, allHospitals);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "Error fetching hospitals for specialty {SpecialtyId} via gRPC", specialtyId);
        }
    }

    /// <summary>
    /// Fetch hospitals for a specific specialty via gRPC
    /// </summary>
    private async Task<IEnumerable<BookingCare.Services.Hospital.HospitalReply>> FetchHospitalsBySpecialtyAsync(Guid specialtyId)
    {
        var request = new BookingCare.Services.Hospital.GetHospitalsBySpecialtyRequest
        {
            SpecialtyId = specialtyId.ToString()
        };

        var grpcResponse = await _hospitalClient.GetHospitalsBySpecialtyAsync(request);
        return grpcResponse.Hospitals;
    }

    /// <summary>
    /// Process hospital results and update the hospital dictionary
    /// </summary>
    private void ProcessHospitalResults(
        IEnumerable<BookingCare.Services.Hospital.HospitalReply> hospitals,
        Guid specialtyId,
        Dictionary<Guid, string> specialtyNameMap,
        Dictionary<string, HospitalDto> allHospitals)
    {
        foreach (var hospital in hospitals)
        {
            var hospitalDto = GetOrCreateHospitalDto(hospital, allHospitals);
            AddSpecialtyToHospital(hospitalDto, specialtyId, specialtyNameMap);
        }
    }

    /// <summary>
    /// Get existing or create new hospital DTO
    /// </summary>
    private HospitalDto GetOrCreateHospitalDto(
        BookingCare.Services.Hospital.HospitalReply hospital,
        Dictionary<string, HospitalDto> allHospitals)
    {
        if (!allHospitals.ContainsKey(hospital.Id))
        {
            allHospitals[hospital.Id] = CreateHospitalDto(hospital);
        }
        return allHospitals[hospital.Id];
    }

    /// <summary>
    /// Create a new hospital DTO from hospital info
    /// </summary>
    private HospitalDto CreateHospitalDto(BookingCare.Services.Hospital.HospitalReply hospital)
    {
        return new HospitalDto
        {
            Id = hospital.Id,
            Name = hospital.Name,
            Address = hospital.Address,
            SpecialtyNames = new List<string>(),
            ImageUrl = !string.IsNullOrWhiteSpace(hospital.AvatarUrl) ? hospital.AvatarUrl : null
        };
    }

    /// <summary>
    /// Add specialty name to hospital's specialty list if not already present
    /// </summary>
    private void AddSpecialtyToHospital(
        HospitalDto hospitalDto,
        Guid specialtyId,
        Dictionary<Guid, string> specialtyNameMap)
    {
        if (!specialtyNameMap.TryGetValue(specialtyId, out var specialtyName))
        {
            return;
        }

        if (!hospitalDto.SpecialtyNames!.Contains(specialtyName))
        {
            hospitalDto.SpecialtyNames.Add(specialtyName);
        }
    }

    /// <summary>
    /// Get hospitals via REST API fallback
    /// </summary>
    private async Task<Dictionary<string, HospitalDto>> GetHospitalsFallbackAsync()
    {
        var allHospitals = new Dictionary<string, HospitalDto>();

        try
        {
            _logger.LogDebug("Fetching hospitals via gRPC");

            var request = new BookingCare.Services.Hospital.GetHospitalsListRequest
            {
                Page = 1,
                PageSize = 20
            };

            var response = await _hospitalClient.GetHospitalsListAsync(request);

            if (response?.Hospitals != null)
            {
                foreach (var hospital in response.Hospitals)
                {
                    var hospitalDto = new HospitalDto
                    {
                        Id = hospital.Id,
                        Name = hospital.Name,
                        Address = hospital.Address,
                        SpecialtyNames = new List<string>() // Will be populated by GetHospitalsBySpecialtiesAsync if needed
                    };

                    allHospitals[hospital.Id] = hospitalDto;
                }

                _logger.LogDebug("Fetched {Count} hospitals via gRPC", allHospitals.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching hospitals via gRPC");
        }

        return allHospitals;
    }

    /// <summary>
    /// Convert hospital to recommendation
    /// </summary>
    private HospitalRecommendation ConvertToHospitalRecommendation(
        HospitalDto hospital,
        LocationContext? location,
        int specialtyCount = 0)
    {
        return new HospitalRecommendation
        {
            Id = hospital.Id,
            Name = hospital.Name,
            Address = hospital.Address,
            SpecialtyNames = hospital.SpecialtyNames ?? new List<string>(),
            RecommendationScore = CalculateHospitalScore(hospital, location, specialtyCount),
            ImageUrl = hospital.ImageUrl
        };
    }

    private async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(
        List<Guid> specialtyIds,
        LocationContext? location)
    {
        try
        {
            Dictionary<string, HospitalDto> allHospitals;

            if (specialtyIds.Any())
            {
                // Get specialty names
                var specialtyNameMap = await GetSpecialtyNamesAsync(specialtyIds);

                // Get hospitals by specialties
                allHospitals = await GetHospitalsBySpecialtiesAsync(specialtyIds, specialtyNameMap);
            }
            else
            {
                // Fallback to REST API if no specialties
                allHospitals = await GetHospitalsFallbackAsync();
            }

            // Convert and rank hospitals
            var specialtyCount = specialtyIds.Count;
            var hospitals = allHospitals.Values
                .Select(h => ConvertToHospitalRecommendation(h, location, specialtyCount))
                .OrderByDescending(h => h.RecommendationScore)
                .Take(5) // Top 5 hospitals (increased from 3 to better cover multiple specialties)
                .ToList();

            _logger.LogInformation("Selected {Count} hospitals with {SpecialtyCount} recommended specialties",
                hospitals.Count, specialtyCount);

            return hospitals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hospital recommendations");
            return new List<HospitalRecommendation>();
        }
    }

    /// <summary>
    /// Calculate hospital address availability score
    /// </summary>
    private double CalculateHospitalAddressScore(HospitalDto hospital)
    {
        bool hasAddress = !string.IsNullOrWhiteSpace(hospital.Address);
        return hasAddress ? 0.30 : 0.05;
    }

    /// <summary>
    /// Calculate hospital location matching score
    /// </summary>
    private double CalculateHospitalLocationScore(
        HospitalDto hospital,
        LocationContext? location)
    {
        if (location == null)
        {
            return 0.05; // No location preference
        }

        bool hasAddress = !string.IsNullOrWhiteSpace(hospital.Address);

        // Check exact address match
        if (hasAddress && !string.IsNullOrWhiteSpace(location.DisplayName))
        {
            bool locationMatch = IsAddressInLocation(hospital.Address, location.DisplayName);
            if (locationMatch)
            {
                _logger.LogDebug("Hospital {HospitalId} address matches location: {Address} matches {Location}",
                    hospital.Id, hospital.Address, location.DisplayName);
                return 0.50; // HIGHEST priority
            }
            else
            {
                _logger.LogDebug("Hospital {HospitalId} address does NOT match location: {Address} vs {Location}",
                    hospital.Id, hospital.Address, location.DisplayName);
                return 0.05;
            }
        }

        // Fallback to district/province matching
        if (!string.IsNullOrEmpty(location.DistrictId))
        {
            return 0.20; // Medium priority
        }

        if (!string.IsNullOrEmpty(location.ProvinceId))
        {
            return 0.15; // Lower priority
        }

        return 0.05; // No location data
    }

    /// <summary>
    /// Calculate hospital specialty matching score
    /// </summary>
    private double CalculateHospitalSpecialtyScore(
        HospitalDto hospital,
        int specialtyCount)
    {
        if (specialtyCount > 0 && hospital.SpecialtyNames != null && hospital.SpecialtyNames.Any())
        {
            var matchRatio = Math.Min(
                (double)hospital.SpecialtyNames.Count / Math.Max(specialtyCount, 1),
                1.0
            );
            return 0.30 * matchRatio;
        }
        return 0.05; // Minimal score if no specialty match
    }

    private double CalculateHospitalScore(
        HospitalDto hospital,
        LocationContext? location,
        int specialtyCount = 0)
    {
        double score = 0;

        // Address availability score (30%)
        score += CalculateHospitalAddressScore(hospital);

        // Location matching score (50%)
        score += CalculateHospitalLocationScore(hospital, location);

        // Specialty matching score (30%)
        score += CalculateHospitalSpecialtyScore(hospital, specialtyCount);

        // Specialty diversity score (10%)
        var specialtyDiversity = Math.Min((hospital.SpecialtyNames?.Count ?? 0) / 20.0, 1.0);
        score += 0.10 * specialtyDiversity;

        return Math.Round(score, 3);
    }

    #region DTOs for external API calls

    private class HospitalFilterResponse
    {
        public HospitalDataWrapper? Data { get; set; }
    }

    private class HospitalDataWrapper
    {
        public List<HospitalDto> Items { get; set; } = new();
    }

    private class SpecialtyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int DoctorCount { get; set; }
    }

    private class HospitalDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string>? SpecialtyNames { get; set; }
        public string? ImageUrl { get; set; }
        public string? AvatarUrl { get; set; } // For mapping from REST API response
    }

    #endregion

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(Guid sessionId, Guid userId)
    {
        try
        {
            return await _conversationSessionService.LoadConversationHistoryAsync(sessionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
            throw new SymptomAnalysisException($"Failed to get conversation history: {ex.Message}", ex);
        }
    }

    public async Task<List<SessionSummary>> GetUserSessionsAsync(Guid userId)
    {
        try
        {
            var sessions = await _conversationSessionService.GetUserSessionsAsync(userId);

            return sessions.Select(s => new SessionSummary
            {
                SessionId = s.Id,
                UserId = s.UserId,
                Title = s.Title,
                LastMessage = s.LastMessage,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                MessageCount = s.MessageCount
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions for user {UserId}", userId);
            throw new SymptomAnalysisException($"Failed to get user sessions: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId)
    {
        try
        {
            return await _conversationSessionService.DeleteSessionAsync(sessionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            throw new SymptomAnalysisException($"Failed to delete session: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Removes unwanted questions about suggesting doctors/hospitals from AI response
    /// </summary>
    private string RemoveUnwantedSuggestionsQuestion(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        // Patterns to remove (case-insensitive)
        var patternsToRemove = new[]
        {
            @"bạn có muốn tôi gợi ý bác sĩ hoặc bệnh viện ngay bây giờ không\??",
            @"bạn có muốn tôi gợi ý bác sĩ hoặc bệnh viện không\??",
            @"bạn có muốn tôi gợi ý bác sĩ/bệnh viện ngay bây giờ không\??",
            @"bạn có muốn tôi gợi ý bác sĩ/bệnh viện không\??",
            @"bạn có muốn tôi đề xuất bác sĩ hoặc bệnh viện ngay bây giờ không\??",
            @"bạn có muốn tôi đề xuất bác sĩ hoặc bệnh viện không\??",
            @"bạn có muốn tôi đề xuất bác sĩ/bệnh viện ngay bây giờ không\??",
            @"bạn có muốn tôi đề xuất bác sĩ/bệnh viện không\??"
        };

        var result = message;
        foreach (var pattern in patternsToRemove)
        {
            // Remove the pattern and any surrounding whitespace/newlines
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                pattern,
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Multiline,
                TimeSpan.FromSeconds(2)
            );
        }

        // Clean up multiple consecutive newlines
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\n{3,}", "\n\n", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));

        return result.Trim();
    }

    /// <summary>
    /// Normalize line breaks in message content
    /// Converts \r\n\r\n, \n\n, \r\n to single \n
    /// Removes consecutive empty lines
    /// </summary>
    private string NormalizeLineBreaks(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content ?? string.Empty;
        }

        // Step 1: Normalize all line break types to \n
        // Replace \r\n (Windows) and \r (old Mac) with \n
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Step 2: Replace multiple consecutive newlines (2+) with single newline
        // This handles \n\n, \n\n\n, etc. -> \n
        normalized = Regex.Replace(normalized, @"\n{2,}", "\n", RegexOptions.None, TimeSpan.FromSeconds(2));

        // Step 3: Trim leading and trailing newlines (but keep content)
        normalized = normalized.Trim('\n');

        return normalized;
    }
}


