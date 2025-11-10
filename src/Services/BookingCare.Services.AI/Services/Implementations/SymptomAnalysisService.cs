using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.DTOs.Responses;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Shared.Common.Models;
using Grpc.Core;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Main service for symptom analysis and doctor/hospital recommendations
/// </summary>
public class SymptomAnalysisService : ISymptomAnalysisService
{
    private readonly IGeminiService _geminiService;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly BookingCare.Services.Hospital.HospitalService.HospitalServiceClient _hospitalClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SymptomAnalysisService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConversationSessionService _conversationSessionService;

    // Service URLs from configuration (for REST API fallback)
    private readonly string _doctorServiceUrl;
    private readonly string _hospitalServiceUrl;

    // Cache for specialty mapping to avoid multiple API calls
    private static Dictionary<string, Guid>? _specialtyNameMapCache = null;
    private static List<SpecialtyDto>? _allSpecialtiesCache = null;
    private static Dictionary<string, SpecialtyDto>? _exactMatchDictCache = null;
    private static Dictionary<string, SpecialtyDto>? _normalizedMatchDictCache = null;
    private static DateTime _cacheLastUpdated = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30); // Cache for 30 minutes
    private static readonly object _cacheLock = new object();

    // Emergency detection keywords
    private static readonly string[] EmergencyKeywords = new[]
    {
        "đau ngực dữ dội", "đau ngực đột ngột", "đau thắt ngực",
        "khó thở", "thở gấp", "nghẹt thở", "không thở được",
        "chảy máu nhiều", "mất máu", "xuất huyết",
        "ngất xỉu", "bất tỉnh", "mất ý thức",
        "đột quỵ", "tai biến", "liệt đột ngột",
        "co giật", "động kinh",
        "sốc phản vệ", "dị ứng nặng",
        "đau bụng dữ dội đột ngột",
        "nôn ra máu", "ho ra máu"
    };

    public SymptomAnalysisService(
        IGeminiService geminiService,
        DoctorService.DoctorServiceClient doctorClient,
        BookingCare.Services.Hospital.HospitalService.HospitalServiceClient hospitalClient,
        IHttpClientFactory httpClientFactory,
        ILogger<SymptomAnalysisService> logger,
        IConfiguration configuration,
        IConversationSessionService conversationSessionService)
    {
        _geminiService = geminiService;
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _httpClient = httpClientFactory.CreateClient("BookingCareServices");
        _logger = logger;
        _configuration = configuration;
        _conversationSessionService = conversationSessionService;

        _doctorServiceUrl = configuration["Services:Doctor:Url"] ?? "http://localhost:6008";
        _hospitalServiceUrl = configuration["Services:Hospital:Url"] ?? "http://localhost:6004";
    }

    /// <summary>
    /// Detect emergency keywords in user message
    /// </summary>
    private bool DetectEmergency(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var lowerMessage = message.ToLowerInvariant();
        return EmergencyKeywords.Any(keyword => lowerMessage.Contains(keyword.ToLowerInvariant()));
    }

    public async Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Starting symptom analysis for message: {Message}", request.Message);

            // Step 0: Get or create session and load conversation history
            Guid sessionId;
            List<ConversationMessage> conversationHistory;

            // Validate that userId is provided (authentication required)
            if (!request.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated to use AI chat service.");
            }

            try
            {
                sessionId = await _conversationSessionService.GetOrCreateSessionAsync(
                    request.SessionId,
                    request.UserId.Value,
                    request.Location
                );

                conversationHistory = await _conversationSessionService.LoadConversationHistoryAsync(sessionId);
                _logger.LogInformation("Session loaded: {SessionId}, History count: {Count}", sessionId, conversationHistory.Count);
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Failed to load/create session from database, using fallback session ID");
                // Fallback: Use provided sessionId or create new one
                sessionId = request.SessionId ?? Guid.NewGuid();
                conversationHistory = request.ConversationHistory?.Select(msg =>
                {
                    DateTime timestamp = DateTime.UtcNow;
                    if (msg.Timestamp != null)
                    {
                        if (msg.Timestamp is DateTime dt)
                            timestamp = dt;
                        else if (DateTime.TryParse(msg.Timestamp.ToString(), out var parsed))
                            timestamp = parsed;
                    }

                    return new ConversationMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content,
                        Timestamp = timestamp
                    };
                }).ToList() ?? new List<ConversationMessage>();
            }

            // Step 1: Detect emergency (both in code and let AI detect)
            var isEmergencyDetected = DetectEmergency(request.Message);

            // Step 2: Call Gemini AI to analyze symptoms
            var geminiResponse = await _geminiService.AnalyzeSymptomsAsync(
                request.Message,
                conversationHistory
            );

            var geminiResult = _geminiService.ParseGeminiResponse(geminiResponse);

            // Override emergency detection if code detects it (safer)
            var requiresImmediateAttention = isEmergencyDetected || geminiResult.RequiresImmediateAttention;

            // Step 3: Apply confidence thresholds
            var topSpecialtyConfidence = geminiResult.RecommendedSpecialties
                .OrderByDescending(s => s.Confidence)
                .FirstOrDefault()?.Confidence ?? 0;

            // Count number of QUESTIONS (not conclusions) already asked in conversation history
            var questionsAskedCount = conversationHistory
                .Where(m => m.Role?.ToLower() == "ai" &&
                           !m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) == true &&
                           !m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) == true &&
                           !m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) == true &&
                           (m.Content?.Contains("?") == true ||
                            m.Content?.Contains("cho tôi biết") == true ||
                            m.Content?.Contains("bạn có thể") == true))
                .Count();

            _logger.LogInformation("Questions already asked in conversation: {Count}", questionsAskedCount);

            // Check if user is asking follow-up questions after having suggestions
            // If user message contains keywords like "hỏi thêm", "tư vấn thêm", "giải thích thêm"
            // and there are previous suggestions, allow more questions without auto-recommending
            var hasPreviousSuggestions = conversationHistory
                .Any(m => m.Role?.ToLower() == "ai" && m.Suggestions != null);

            // Check if user is asking for more consultation (clicked "Tư vấn thêm" button)
            var isConsultMoreRequest = hasPreviousSuggestions && (
                request.Message.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
                request.Message.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase)
            );

            var isFollowUpQuestion = hasPreviousSuggestions && (
                request.Message.Contains("hỏi thêm", StringComparison.OrdinalIgnoreCase) ||
                request.Message.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
                request.Message.Contains("giải thích thêm", StringComparison.OrdinalIgnoreCase) ||
                request.Message.Contains("tại sao", StringComparison.OrdinalIgnoreCase) ||
                request.Message.Contains("như thế nào", StringComparison.OrdinalIgnoreCase)
            );

            // Count follow-up questions (AI messages after user clicked "Tư vấn thêm")
            // Find the index of the last user message with "tư vấn thêm"
            var lastConsultMoreIndex = -1;
            for (int i = conversationHistory.Count - 1; i >= 0; i--)
            {
                if ((conversationHistory[i].Role?.ToLower() == "patient" ||
                     conversationHistory[i].Role?.ToLower() == "guest") &&
                    (conversationHistory[i].Content?.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true ||
                     conversationHistory[i].Content?.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true))
                {
                    lastConsultMoreIndex = i;
                    break;
                }
            }

            // Count ONLY AI questions (not conclusions) after the last "tư vấn thêm" request
            // Exclude messages that contain conclusion markers
            var followUpQuestionsCount = 0;
            if (lastConsultMoreIndex >= 0)
            {
                followUpQuestionsCount = conversationHistory
                    .Skip(lastConsultMoreIndex + 1)
                    .Where(m => m.Role?.ToLower() == "ai" &&
                               !m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) == true &&
                               !m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) == true &&
                               !m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) == true &&
                               (m.Content?.Contains("?") == true ||
                                m.Content?.Contains("cho tôi biết") == true ||
                                m.Content?.Contains("bạn có thể") == true))
                    .Count();
            }

            _logger.LogInformation("Follow-up questions count after 'tư vấn thêm': {Count}", followUpQuestionsCount);

            // Determine if analysis is complete based on confidence
            var analysisComplete = ApplyConfidenceThresholds(
                geminiResult,
                topSpecialtyConfidence,
                out var filteredSpecialties,
                out var shouldAskMoreQuestions
            );

            // If user clicked "Tư vấn thêm" or asking follow-up questions, allow more questions (max 3)
            if (isConsultMoreRequest || isFollowUpQuestion)
            {
                if (followUpQuestionsCount >= 3)
                {
                    // Already asked 3 follow-up questions, force complete and recommend
                    _logger.LogInformation("Already asked {Count} follow-up questions, forcing analysis complete and recommending", followUpQuestionsCount);
                    analysisComplete = true;
                    shouldAskMoreQuestions = false;
                }
                else
                {
                    _logger.LogInformation("User is asking follow-up questions ({Count}/3), allowing more questions", followUpQuestionsCount);

                    // For "Tư vấn thêm", keep asking questions until we have 3 questions
                    // Only show full recommendations after 3 questions
                    analysisComplete = false; // Keep false while asking questions
                    shouldAskMoreQuestions = true;

                    // If AI doesn't have specific questions, ensure we still get relevant questions
                    if (!geminiResult.NextQuestions.Any() && isConsultMoreRequest)
                    {
                        _logger.LogInformation("User requested more consultation but AI has no questions. This should not happen if prompt is correct.");
                    }
                }
            }
            // Force complete if already asked 3 questions (but not for follow-up questions)
            else if (questionsAskedCount >= 3)
            {
                _logger.LogInformation("Already asked {Count} questions, forcing analysis complete", questionsAskedCount);
                analysisComplete = true;
                shouldAskMoreQuestions = false;
            }

            // Step 4: Validate complete analysis has all required components
            if (analysisComplete)
            {
                // Validate that AI returned all required components for conclusion
                var missingComponents = new List<string>();

                if (!geminiResult.PossibleDiseases.Any())
                    missingComponents.Add("possibleDiseases");
                if (!filteredSpecialties.Any())
                    missingComponents.Add("recommendedSpecialties");
                if (!geminiResult.GeneralAdvice.Any())
                    missingComponents.Add("generalAdvice");

                if (missingComponents.Any())
                {
                    _logger.LogWarning("AI returned incomplete conclusion. Missing: {Components}. Forcing to ask more questions.",
                        string.Join(", ", missingComponents));

                    // Force back to asking mode if conclusion is incomplete
                    analysisComplete = false;
                    shouldAskMoreQuestions = true;

                    // Add a generic follow-up question if AI didn't provide one
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
            }

            // Step 5: Create base response
            var baseMessage = shouldAskMoreQuestions
                ? (geminiResult.NextQuestions.Any()
                    ? geminiResult.NextQuestions.First().Question  // Chỉ hiển thị câu hỏi khi chưa đủ thông tin
                    : "Xin lỗi, tôi cần thêm thông tin để tư vấn chính xác hơn. Bạn có thể mô tả thêm về triệu chứng của mình không?")
                : BuildAIMessage(geminiResult, requiresImmediateAttention);  // Hiển thị đầy đủ khi đã đủ thông tin

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
                NextQuestions = shouldAskMoreQuestions && geminiResult.NextQuestions.Any()
                    ? geminiResult.NextQuestions
                        .OrderByDescending(q => q.Priority == "HIGH" ? 3 : q.Priority == "MEDIUM" ? 2 : 1)
                        .Take(1)  // Chỉ lấy 1 câu hỏi đầu tiên
                        .Select(q => new FollowUpQuestion
                        {
                            Question = q.Question,
                            Purpose = q.Purpose,
                            Priority = q.Priority
                        }).ToList()
                    : new List<FollowUpQuestion>(),
                GeneralAdvice = geminiResult.GeneralAdvice,
                AnalysisComplete = analysisComplete,
                RequiresImmediateAttention = requiresImmediateAttention
            };

            // Step 5: Map specialties from Gemini to DB
            var specialtyMatches = await MapSpecialtiesToDbAsync(filteredSpecialties);
            response.RecommendedSpecialties = specialtyMatches;

            // Step 6: If analysis is complete and specialties identified, get recommendations
            // Always recommend when analysis is complete, regardless of follow-up status
            // This ensures user gets doctor/hospital cards whenever we have enough info
            var shouldRecommend = analysisComplete && specialtyMatches.Any(s => s.SpecialtyId != null);

            if (shouldRecommend)
            {
                var specialtyIds = specialtyMatches
                    .Where(s => s.SpecialtyId != null)
                    .Select(s => s.SpecialtyId!.Value)
                    .ToList();

                // Get doctor and hospital recommendations in parallel for better performance
                var doctorTask = GetDoctorRecommendationsAsync(
                    specialtyIds,
                    request.Location,
                    specialtyMatches
                );
                var hospitalTask = GetHospitalRecommendationsAsync(
                    specialtyIds,
                    request.Location,
                    requiresImmediateAttention
                );

                // Wait for both to complete in parallel
                await Task.WhenAll(doctorTask, hospitalTask);

                response.RecommendedDoctors = doctorTask.Result;
                response.RecommendedHospitals = hospitalTask.Result;

                // Add disclaimer and intro text to message if we have recommendations
                // Remove unwanted question first, then add disclaimer
                if (response.RecommendedDoctors.Any() || response.RecommendedHospitals.Any())
                {
                    // Remove unwanted question from message before adding disclaimer
                    response.Message = RemoveUnwantedSuggestionsQuestion(response.Message);

                    // Add disclaimer and intro text
                    response.Message += "\n\nLưu ý:\n\n*Đây chỉ là gợi ý định hướng y tế, không thay thế chẩn đoán chính thức của bác sĩ. Vui lòng đến cơ sở y tế để được khám và điều trị chính xác.*";
                    response.Message += "\n\nDưới đây là gợi ý của mình về các bác sĩ và bệnh viện:";
                }
                else
                {
                    // Even if no recommendations, remove unwanted question
                    response.Message = RemoveUnwantedSuggestionsQuestion(response.Message);
                }
            }

            // Step 7: Save conversation history with suggestions (non-blocking - don't fail if this fails)
            try
            {
                // Prepare suggestions data for saving
                object? suggestionsData = null;
                if (response.RecommendedDoctors.Any() || response.RecommendedHospitals.Any())
                {
                    suggestionsData = new
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

                await _conversationSessionService.SaveConversationHistoryAsync(
                    sessionId,
                    request.Message,
                    response.Message,
                    request.Location,
                    suggestionsData,
                    request.UserId
                );
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to save conversation history, but continuing with response");
                // Don't throw - saving history is not critical for the response
            }

            _logger.LogInformation("Symptom analysis completed. Recommendations: {DoctorCount} doctors, {HospitalCount} hospitals",
                response.RecommendedDoctors.Count, response.RecommendedHospitals.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException?.Message);
            throw new ApplicationException($"Failed to analyze symptoms: {ex.Message}", ex);
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

        // If emergency, always complete and recommend
        if (geminiResult.RequiresImmediateAttention)
        {
            filteredSpecialties = geminiResult.RecommendedSpecialties
                .OrderByDescending(s => s.Confidence)
                .ToList();
            return true;
        }

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
        if (topSpecialtyConfidence >= 0.5 && topSpecialtyConfidence <= 0.8)
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

    private string BuildAIMessage(GeminiAnalysisResult result, bool requiresImmediateAttention)
    {
        var sb = new StringBuilder();

        if (requiresImmediateAttention)
        {
            sb.AppendLine("⚠️ **KHẨN CẤP**: Các triệu chứng của bạn cần được chăm sóc y tế ngay lập tức. Vui lòng gọi **115** hoặc đến phòng cấp cứu gần nhất ngay bây giờ!");
            sb.AppendLine();
            sb.AppendLine("**Không nên chờ đợi** - đây là trường hợp khẩn cấp y tế.");
            sb.AppendLine();
        }

        // REQUIRED: Possible diseases with confidence
        if (result.PossibleDiseases.Any())
        {
            sb.AppendLine("Dựa trên các triệu chứng bạn mô tả, có thể liên quan đến:");
            foreach (var disease in result.PossibleDiseases.OrderByDescending(d => d.Confidence).Take(5))
            {
                var confidencePercent = (int)(disease.Confidence * 100);
                sb.AppendLine($"- {disease.Name} (độ tin cậy: {confidencePercent}%)");
            }
            sb.AppendLine();
        }
        else
        {
            // Fallback if no diseases provided (should not happen with validation)
            _logger.LogWarning("BuildAIMessage called but no possible diseases provided");
            sb.AppendLine("Dựa trên các triệu chứng bạn mô tả, tôi cần thêm thông tin để đưa ra đánh giá chính xác.");
            sb.AppendLine();
        }

        // REQUIRED: General advice
        if (result.GeneralAdvice.Any())
        {
            sb.AppendLine("**Lời khuyên chung**:");
            foreach (var advice in result.GeneralAdvice)
            {
                sb.AppendLine($"• {advice}");
            }
            sb.AppendLine();
        }
        else
        {
            // Fallback if no advice provided (should not happen with validation)
            _logger.LogWarning("BuildAIMessage called but no general advice provided");
            sb.AppendLine("**Lời khuyên chung**:");
            sb.AppendLine("• Theo dõi triệu chứng của bạn");
            sb.AppendLine("• Nếu triệu chứng trở nên nghiêm trọng hơn, hãy đến cơ sở y tế");
            sb.AppendLine();
        }

        // REQUIRED: Recommended specialties
        if (result.RecommendedSpecialties.Any())
        {
            var topSpecialty = result.RecommendedSpecialties.OrderByDescending(s => s.Confidence).First();
            sb.AppendLine($"**Chuyên khoa phù hợp**: {topSpecialty.SpecialtyName}");
            sb.AppendLine();
        }
        else
        {
            // Fallback if no specialties provided (should not happen with validation)
            _logger.LogWarning("BuildAIMessage called but no recommended specialties provided");
            sb.AppendLine("**Chuyên khoa phù hợp**: Nội tổng quát");
            sb.AppendLine();
        }

        if (!result.AnalysisComplete && result.NextQuestions.Any())
        {
            sb.AppendLine("Để tư vấn chính xác hơn, bạn có thể cho tôi biết thêm:");
        }

        return sb.ToString().Trim();
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
            Dictionary<string, SpecialtyDto> exactMatchDict;
            Dictionary<string, SpecialtyDto> normalizedMatchDict;

            lock (_cacheLock)
            {
                // Check if dictionaries are cached and still valid
                if (_exactMatchDictCache != null &&
                    _normalizedMatchDictCache != null &&
                    _cacheLastUpdated != DateTime.MinValue &&
                    DateTime.UtcNow - _cacheLastUpdated < CacheExpiration)
                {
                    exactMatchDict = _exactMatchDictCache;
                    normalizedMatchDict = _normalizedMatchDictCache;
                }
                else
                {
                    // Build dictionaries
                    exactMatchDict = new Dictionary<string, SpecialtyDto>(StringComparer.OrdinalIgnoreCase);
                    normalizedMatchDict = new Dictionary<string, SpecialtyDto>(StringComparer.OrdinalIgnoreCase);

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
                    _exactMatchDictCache = exactMatchDict;
                    _normalizedMatchDictCache = normalizedMatchDict;
                }
            }

            // Now do fast dictionary lookups
            foreach (var geminiSpecialty in geminiSpecialties)
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

                // Only add to result if we found a match (specialtyId is not null)
                if (specialtyId.HasValue)
                {
                    result.Add(new SpecialtyMatch
                    {
                        SpecialtyId = specialtyId,
                        SpecialtyName = matchedSpecialtyName ?? geminiSpecialty.SpecialtyName,
                        Confidence = geminiSpecialty.Confidence,
                        Urgency = geminiSpecialty.Urgency,
                        Reasons = geminiSpecialty.Reasons
                    });
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
            _allSpecialtiesCache = specialties;
            _cacheLastUpdated = DateTime.UtcNow;

            // Clear dictionary caches when specialties are refreshed
            _exactMatchDictCache = null;
            _normalizedMatchDictCache = null;

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
            var url = $"{_doctorServiceUrl}/api/v1.0/specialties/all";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SpecialtyDto>>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all specialties for fuzzy matching");
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

        if (bestMatch != null)
        {
            return (bestMatch.Specialty.Id, bestMatch.Specialty.Name, bestMatch.Similarity);
        }

        return null;
    }

    private double CalculateSimilarity(string str1, string str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2))
            return 0.0;

        // Exact match
        if (str1 == str2)
            return 1.0;

        // Contains match (one contains the other)
        if (str1.Contains(str2) || str2.Contains(str1))
        {
            var longer = str1.Length > str2.Length ? str1 : str2;
            var shorter = str1.Length > str2.Length ? str2 : str1;
            return (double)shorter.Length / longer.Length;
        }

        // Word-based matching (check if key words match)
        var words1 = str1.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var words2 = str2.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        var matchingWords = words1.Count(w1 => words2.Any(w2 => w1 == w2 || w1.Contains(w2) || w2.Contains(w1)));
        var totalWords = Math.Max(words1.Length, words2.Length);

        if (totalWords == 0)
            return 0.0;

        var wordSimilarity = (double)matchingWords / totalWords;

        // Levenshtein distance for character-level similarity
        var levenshteinDistance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        var charSimilarity = maxLength > 0 ? 1.0 - ((double)levenshteinDistance / maxLength) : 0.0;

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

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

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

    private async Task<List<DoctorRecommendation>> GetDoctorRecommendationsAsync(
        List<Guid> specialtyIds,
        LocationContext? location,
        List<SpecialtyMatch> specialtyMatches)
    {
        try
        {
            // Use gRPC to filter doctors
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

            var grpcResponse = await _doctorClient.FilterDoctorsForRecommendationAsync(request);

            if (grpcResponse.Doctors == null || !grpcResponse.Doctors.Any())
            {
                _logger.LogWarning("No doctors found for specialties: {SpecialtyIds}", string.Join(", ", specialtyIds));
                return new List<DoctorRecommendation>();
            }

            // Get hospital addresses for location matching
            var hospitalIds = grpcResponse.Doctors
                .Where(d => !string.IsNullOrWhiteSpace(d.HospitalId))
                .Select(d => d.HospitalId)
                .Distinct()
                .ToList();

            var hospitalAddressMap = new Dictionary<string, string>();
            if (hospitalIds.Any() && location != null)
            {
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
            }

            // Rank doctors
            return grpcResponse.Doctors
                .Select(d => new DoctorRecommendation
                {
                    Id = d.Id,
                    Name = d.FullName,
                    SpecialtyName = d.SpecialtyName,
                    HospitalName = d.HospitalName,
                    Rating = d.Rating,
                    YearOfExperience = d.YearsOfExperience,
                    ServiceTypeName = d.ServiceTypeName ?? "Khám chuyên khoa",
                    Price = d.ConsultationFee > 0 ? $"{d.ConsultationFee:N0} VNĐ" : null,
                    RecommendationScore = CalculateDoctorScore(d, specialtyMatches, location,
                        hospitalAddressMap.TryGetValue(d.HospitalId, out var address) ? address : null),
                    AvatarUrl = !string.IsNullOrWhiteSpace(d.AvatarUrl) ? d.AvatarUrl : null
                })
                .OrderByDescending(d => d.RecommendationScore)
                .Take(5) // Top 5 doctors
                .ToList();
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

    private double CalculateDoctorScore(
        BookingCare.Services.Doctor.Protos.DoctorRecommendationInfo doctor,
        List<SpecialtyMatch> specialtyMatches,
        LocationContext? location,
        string? hospitalAddress = null)
    {
        double score = 0;

        // PRIORITY: Doctor with hospital address map (has hospital name = has address) gets bonus
        // This ensures doctors with hospital address are ranked first
        bool hasHospitalAddress = !string.IsNullOrWhiteSpace(doctor.HospitalName) &&
                                  !string.IsNullOrWhiteSpace(doctor.HospitalId);
        if (hasHospitalAddress)
        {
            score += 0.30; // Base bonus for having hospital address map
        }
        else
        {
            score += 0.05; // Very low score if no hospital address
        }

        // CRITICAL: Location matching based on actual address (ưu tiên cao nhất)
        // Check if hospital address matches user location
        bool locationMatch = false;
        if (location != null && !string.IsNullOrWhiteSpace(hospitalAddress) && !string.IsNullOrWhiteSpace(location.DisplayName))
        {
            locationMatch = IsAddressInLocation(hospitalAddress, location.DisplayName);
            if (locationMatch)
            {
                score += 0.50; // HIGHEST priority for location match
                _logger.LogDebug("Doctor {DoctorId} hospital address matches location: {Address} matches {Location}",
                    doctor.Id, hospitalAddress, location.DisplayName);
            }
            else
            {
                score += 0.05; // Very low score if location doesn't match
                _logger.LogDebug("Doctor {DoctorId} hospital address does NOT match location: {Address} vs {Location}",
                    doctor.Id, hospitalAddress, location.DisplayName);
            }
        }
        else if (location != null && !string.IsNullOrEmpty(location.DistrictId))
        {
            // Fallback: if no address, use district/province ID matching
            score += 0.20; // Medium priority for district match
        }
        else if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
        {
            // Fallback: province match
            score += 0.15; // Lower priority for province match
        }
        else
        {
            // No location preference or no location data
            score += 0.05; // Lower score without location
        }

        // 10% - Specialty match confidence
        var specialtyMatch = specialtyMatches.FirstOrDefault(s =>
            s.SpecialtyName.Equals(doctor.SpecialtyName, StringComparison.OrdinalIgnoreCase));
        if (specialtyMatch != null)
        {
            score += 0.10 * specialtyMatch.Confidence;
        }
        else
        {
            // No specialty match = very low score
            score += 0.02; // Minimal score
        }

        // 5% - Rating (normalized to 0-1, assuming 5-star scale)
        // Rating 5.0 = 5%, Rating 4.0 = 4%, Rating 3.0 = 3%
        score += 0.05 * Math.Min(doctor.Rating / 5.0, 1.0);

        // 5% - Experience (capped at 30 years for normalization)
        // 30+ years = 5%, 20 years = 3.33%, 10 years = 1.67%
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

    private async Task<List<HospitalRecommendation>> GetHospitalRecommendationsAsync(
        List<Guid> specialtyIds,
        LocationContext? location,
        bool isEmergency)
    {
        try
        {
            var allHospitals = new Dictionary<string, HospitalDto>();
            var specialtyNameMap = new Dictionary<Guid, string>(); // Cache specialty names

            // Get specialty names for matching
            if (specialtyIds.Any())
            {
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
            }

            // Use gRPC to get hospitals by specialty
            if (specialtyIds.Any())
            {
                foreach (var specialtyId in specialtyIds)
                {
                    try
                    {
                        var request = new BookingCare.Services.Hospital.GetHospitalsBySpecialtyRequest
                        {
                            SpecialtyId = specialtyId.ToString()
                        };

                        var grpcResponse = await _hospitalClient.GetHospitalsBySpecialtyAsync(request);

                        foreach (var hospital in grpcResponse.Hospitals)
                        {
                            // Store or update hospital info
                            if (!allHospitals.ContainsKey(hospital.Id))
                            {
                                allHospitals[hospital.Id] = new HospitalDto
                                {
                                    Id = hospital.Id,
                                    Name = hospital.Name,
                                    Address = hospital.Address,
                                    SpecialtyNames = new List<string>(),
                                    ImageUrl = !string.IsNullOrWhiteSpace(hospital.AvatarUrl) ? hospital.AvatarUrl : null
                                };
                            }

                            // Add specialty name to hospital's specialty list
                            if (specialtyNameMap.TryGetValue(specialtyId, out var specialtyName))
                            {
                                var hospitalDto = allHospitals[hospital.Id];
                                if (!hospitalDto.SpecialtyNames!.Contains(specialtyName))
                                {
                                    hospitalDto.SpecialtyNames.Add(specialtyName);
                                }
                            }
                        }
                    }
                    catch (RpcException ex)
                    {
                        _logger.LogWarning(ex, "Error fetching hospitals for specialty {SpecialtyId} via gRPC", specialtyId);
                    }
                }
            }
            else
            {
                // Fallback to REST API if no specialties
                var url = $"{_hospitalServiceUrl}/api/v1.0/hospitals/list?pageSize=20";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var hospitalResponse = JsonSerializer.Deserialize<HospitalFilterResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (hospitalResponse?.Data?.Items != null)
                    {
                        foreach (var hospital in hospitalResponse.Data.Items)
                        {
                            // Map AvatarUrl to ImageUrl if ImageUrl is not set
                            if (string.IsNullOrWhiteSpace(hospital.ImageUrl) && !string.IsNullOrWhiteSpace(hospital.AvatarUrl))
                            {
                                hospital.ImageUrl = hospital.AvatarUrl;
                            }
                            allHospitals[hospital.Id] = hospital;
                        }
                    }
                }
            }

            // Convert dictionary to list for processing
            var hospitalsList = allHospitals.Values.ToList();

            return hospitalsList
                .Select(h => new HospitalRecommendation
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    SpecialtyNames = h.SpecialtyNames ?? new List<string>(),
                    RecommendationScore = CalculateHospitalScore(h, specialtyIds.Count, location, isEmergency),
                    ImageUrl = h.ImageUrl
                })
                .OrderByDescending(h => h.RecommendationScore)
                .Take(3) // Top 3 hospitals
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hospital recommendations");
            return new List<HospitalRecommendation>();
        }
    }

    private double CalculateHospitalScore(HospitalDto hospital, int specialtyCount, LocationContext? location, bool isEmergency)
    {
        double score = 0;

        // PRIORITY: Hospital with address map (has address) gets bonus
        // This ensures hospitals with address are ranked first
        bool hasAddress = !string.IsNullOrWhiteSpace(hospital.Address);
        if (hasAddress)
        {
            score += 0.30; // Base bonus for having address map
        }
        else
        {
            score += 0.05; // Very low score if no address
        }

        // CRITICAL: Location matching based on actual address (ưu tiên cao nhất)
        // Check if hospital address matches user location
        bool locationMatch = false;
        if (location != null && hasAddress && !string.IsNullOrWhiteSpace(location.DisplayName))
        {
            locationMatch = IsAddressInLocation(hospital.Address, location.DisplayName);
            if (locationMatch)
            {
                score += 0.50; // HIGHEST priority for location match
                _logger.LogDebug("Hospital {HospitalId} address matches location: {Address} matches {Location}",
                    hospital.Id, hospital.Address, location.DisplayName);
            }
            else
            {
                score += 0.05; // Very low score if location doesn't match
                _logger.LogDebug("Hospital {HospitalId} address does NOT match location: {Address} vs {Location}",
                    hospital.Id, hospital.Address, location.DisplayName);
            }
        }
        else if (location != null && !string.IsNullOrEmpty(location.DistrictId))
        {
            // Fallback: if no address, use district/province ID matching
            score += 0.20; // Medium priority for district match
        }
        else if (location != null && !string.IsNullOrEmpty(location.ProvinceId))
        {
            // Fallback: province match
            score += 0.15; // Lower priority for province match
        }
        else
        {
            // No location preference or no location data
            score += 0.05; // Lower score without location
        }

        // 30% - Specialty match (hospitals with matching specialties)
        if (specialtyCount > 0 && hospital.SpecialtyNames != null && hospital.SpecialtyNames.Any())
        {
            // Calculate how many recommended specialties match hospital's specialties
            // In production, you would match by specialty IDs, not names
            // For now, assume hospitals with more specialties have better match
            var matchRatio = Math.Min(
                (double)hospital.SpecialtyNames.Count / Math.Max(specialtyCount, 1),
                1.0
            );
            score += 0.30 * matchRatio;
        }
        else
        {
            score += 0.05; // Minimal score if no specialty match
        }

        // 20% - Has emergency department (if urgent/emergency case)
        if (isEmergency)
        {
            // In production, check hospital.HasEmergencyDepartment flag
            // For now, assume hospitals with many specialties (10+) have ER
            bool hasEmergencyDept = (hospital.SpecialtyNames?.Count ?? 0) >= 10;
            score += hasEmergencyDept ? 0.20 : 0.05; // Critical for emergency cases
        }
        else
        {
            // Non-emergency: still give points for having ER (better equipped)
            bool hasEmergencyDept = (hospital.SpecialtyNames?.Count ?? 0) >= 10;
            score += hasEmergencyDept ? 0.10 : 0.05;
        }

        // 10% - Specialty diversity (more specialties = better equipped hospital)
        // 20+ specialties = 10%, 10 specialties = 5%, 5 specialties = 2.5%
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

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(Guid sessionId)
    {
        try
        {
            return await _conversationSessionService.LoadConversationHistoryAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
            throw new ApplicationException($"Failed to get conversation history: {ex.Message}", ex);
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
            throw new ApplicationException($"Failed to get user sessions: {ex.Message}", ex);
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
            throw new ApplicationException($"Failed to delete session: {ex.Message}", ex);
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
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
        }

        // Clean up multiple consecutive newlines
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\n{3,}", "\n\n");

        return result.Trim();
    }
}


