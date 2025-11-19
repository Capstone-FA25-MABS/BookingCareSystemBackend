using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Implementation of Gemini API integration service
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;
    private static readonly TimeSpan ModelCacheDuration = TimeSpan.FromMinutes(15);
    private static readonly object _modelCacheLock = new();
    private static readonly List<string> _cachedModels = new();
    private static DateTime _modelCacheUpdatedAt = DateTime.MinValue;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Build list of models to try
    /// </summary>
    private List<string> BuildModelsToTry(List<string> availableModels)
    {
        var modelsToTry = new List<string>();

        if (!string.IsNullOrEmpty(_settings.Model))
        {
            modelsToTry.Add(_settings.Model);
        }

        // Add available models from API
        if (availableModels.Any())
        {
            modelsToTry.AddRange(availableModels.Where(m => !modelsToTry.Contains(m)));
            _logger.LogInformation("Found {Count} available models: {Models}",
                availableModels.Count, string.Join(", ", availableModels));
        }

        // Fallback models to try
        var fallbackModels = new[]
        {
            "gemini-1.5-flash-latest",
            "gemini-1.5-flash",
            "gemini-1.5-pro-latest",
            "gemini-1.5-pro",
            "gemini-pro"
        };

        modelsToTry.AddRange(fallbackModels.Where(m => !modelsToTry.Contains(m)));

        return modelsToTry.Distinct().ToList();
    }

    /// <summary>
    /// Try calling Gemini API with specific model and endpoint
    /// </summary>
    private async Task<string?> TryCallGeminiApi(string model, string endpoint, object requestBody)
    {
        try
        {
            var url = $"{endpoint}/{model}:generateContent?key={_settings.ApiKey}";

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Calling Gemini API: {Endpoint}/{Model}:generateContent",
                endpoint, model);
            _logger.LogDebug("Request body: {RequestBody}", JsonSerializer.Serialize(requestBody));

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gemini API call successful with model {Model} on {Endpoint}. Response length: {Length}",
                    model, endpoint, responseBody.Length);
                _logger.LogDebug("Gemini API response: {Response}", responseBody);

                return ExtractTextFromGeminiResponse(responseBody);
            }

            // Check for authentication/API key errors
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError("Gemini API authentication failed. Please check your API key and ensure Generative Language API is enabled.");
                throw new GeminiApiException(
                    "Gemini API authentication failed. Please check your API key and ensure 'Generative Language API' is enabled in Google Cloud Console.",
                    "AUTHENTICATION_FAILED",
                    (int)response.StatusCode);
            }

            // If 404, return null to try next endpoint
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Model {Model} not found in {Endpoint}, trying next endpoint", model, endpoint);
                return null;
            }

            // Other errors
            _logger.LogWarning("Gemini API call failed for model {Model} on {Endpoint}. Status: {StatusCode}, Response: {ResponseBody}",
                model, endpoint, response.StatusCode, responseBody);

            return null;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "HTTP error calling Gemini API with model {Model} on {Endpoint}: {Message}",
                model, endpoint, httpEx.Message);
            throw new GeminiApiException(
                $"HTTP error calling Gemini API: {httpEx.Message}",
                "HTTP_REQUEST_FAILED",
                null,
                httpEx);
        }
    }

    public async Task<string> AnalyzeSymptomsAsync(string message, List<ConversationMessage>? conversationHistory = null)
    {
        var prompt = BuildSymptomAnalysisPrompt(message, conversationHistory);
        var requestBody = BuildGeminiRequest(prompt);

        var availableModels = await GetAvailableModelsAsync();
        var modelsToTry = BuildModelsToTry(availableModels);

        Exception? lastException = null;

        var endpoints = new[]
        {
            "https://generativelanguage.googleapis.com/v1beta/models",
            "https://generativelanguage.googleapis.com/v1/models"
        };

        foreach (var model in modelsToTry)
        {
            try
            {
                foreach (var endpoint in endpoints)
                {
                    var result = await TryCallGeminiApi(model, endpoint, requestBody);
                    if (result != null)
                    {
                        return result;
                    }
                }

                // If all endpoints failed for this model, try next model
                lastException = new GeminiApiException(
                    $"Gemini API returned error for model {model} on all endpoints",
                    "MODEL_UNAVAILABLE");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling Gemini API with model {Model}: {Message}",
                    model, ex.Message);
                lastException = new GeminiApiException(
                    $"Failed to call Gemini API with model {model}: {ex.Message}",
                    "API_CALL_FAILED",
                    null,
                    ex);
            }
        }

        // If all models failed, provide helpful error message
        _logger.LogError("All Gemini API models failed. Last error: {Message}", lastException?.Message);
        throw new GeminiApiException(
            $"Failed to analyze symptoms with AI after trying {modelsToTry.Count} models. Please ensure:\n" +
            "1. Your API key is valid and has 'Generative Language API' enabled in Google Cloud Console\n" +
            "2. The API key has proper permissions\n" +
            "3. You're using a supported model\n" +
            $"Last error: {lastException?.Message}",
            "ALL_MODELS_FAILED",
            null,
            lastException);
    }

    /// <summary>
    /// Extract model short name from full path
    /// </summary>
    private string? ExtractModelShortName(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return null;
        }

        // Extract model name from full path (e.g., "models/gemini-pro" -> "gemini-pro")
        var parts = modelName.Split('/');
        return parts.Length > 0 ? parts[parts.Length - 1] : null;
    }

    /// <summary>
    /// Parse models from API response
    /// </summary>
    private List<string> ParseModelsFromResponse(string responseBody)
    {
        var models = new List<string>();

        try
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("models", out var modelsElement))
            {
                foreach (var model in modelsElement.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var modelName = name.GetString();
                        var shortName = ExtractModelShortName(modelName);

                        if (!string.IsNullOrEmpty(shortName) && !models.Contains(shortName))
                        {
                            models.Add(shortName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing models from response");
        }

        return models;
    }

    /// <summary>
    /// Try to fetch models from specific endpoint
    /// </summary>
    private async Task<List<string>> TryFetchModelsFromEndpoint(string endpoint)
    {
        try
        {
            var url = $"{endpoint}?key={_settings.ApiKey}";
            _logger.LogDebug("Fetching available models from: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var models = ParseModelsFromResponse(responseBody);
                if (models.Any())
                {
                    _logger.LogInformation("Successfully fetched {Count} available models from {Endpoint}",
                        models.Count, endpoint);
                    return models;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching models from {Endpoint}", endpoint);
        }

        return new List<string>();
    }

    /// <summary>
    /// Get list of available models from Gemini API
    /// </summary>
    private async Task<List<string>> GetAvailableModelsAsync()
    {
        lock (_modelCacheLock)
        {
            if (_cachedModels.Any() && DateTime.UtcNow - _modelCacheUpdatedAt < ModelCacheDuration)
            {
                _logger.LogDebug("Using cached Gemini models (cached {Minutes} minutes ago)",
                    (DateTime.UtcNow - _modelCacheUpdatedAt).TotalMinutes.ToString("F1"));
                return new List<string>(_cachedModels);
            }
        }

        List<string> models = new();
        try
        {
            // Try v1beta first (for Google AI Studio API keys)
            var endpoints = new[]
            {
                "https://generativelanguage.googleapis.com/v1beta/models",
                "https://generativelanguage.googleapis.com/v1/models"
            };

            foreach (var endpoint in endpoints)
            {
                var fetched = await TryFetchModelsFromEndpoint(endpoint);
                if (fetched.Any())
                {
                    models = fetched;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch available models, will use fallback models");
        }

        if (models.Any())
        {
            UpdateModelCache(models);
        }

        return models;
    }

    private static void UpdateModelCache(List<string> models)
    {
        lock (_modelCacheLock)
        {
            _cachedModels.Clear();
            _cachedModels.AddRange(models);
            _modelCacheUpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get JSON serializer options for parsing
    /// </summary>
    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Try to parse JSON string to GeminiAnalysisResult
    /// </summary>
    private GeminiAnalysisResult? TryDeserializeGeminiResult(string json)
    {
        try
        {
            _logger.LogInformation("Attempting to deserialize JSON. Length: {Length}", json.Length);

            var options = GetJsonSerializerOptions();
            var result = JsonSerializer.Deserialize<GeminiAnalysisResult>(json, options);

            if (result == null)
            {
                _logger.LogWarning("Deserialization returned null. JSON: {Json}", json);
            }
            else
            {
                _logger.LogInformation("Deserialization successful. Result type: {Type}", result.GetType().Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize Gemini result. Exception: {Message}. JSON: {Json}",
                ex.Message, json.Length > 500 ? json.Substring(0, 500) + "..." : json);
            return null;
        }
    }

    /// <summary>
    /// Check if JSON exception indicates incomplete/truncated response
    /// </summary>
    private bool IsIncompleteJsonError(JsonException jsonEx)
    {
        return jsonEx.Message.Contains("end of data", StringComparison.OrdinalIgnoreCase) ||
               jsonEx.Message.Contains("unexpected end", StringComparison.OrdinalIgnoreCase) ||
               jsonEx.Message.Contains("end of string", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Handle incomplete JSON by trying to fix and parse
    /// </summary>
    private GeminiAnalysisResult? HandleIncompleteJson(string geminiResponse, string cleanedResponse, JsonException jsonEx)
    {
        var responsePreview = geminiResponse ?? string.Empty;
        var previewLength = Math.Min(1000, responsePreview.Length);
        _logger.LogError(jsonEx, "JSON response appears to be incomplete/truncated. This may be due to MaxTokens limit being too low. Response preview: {Response}",
            responsePreview.Substring(0, previewLength));

        // Try to fix incomplete JSON by closing open structures
        var jsonToFix = cleanedResponse;
        var fixedJson = TryFixIncompleteJson(jsonToFix);

        if (fixedJson != null)
        {
            _logger.LogInformation("Attempting to parse fixed JSON");
            var result = TryDeserializeGeminiResult(fixedJson);
            if (result != null)
            {
                _logger.LogInformation("Successfully parsed fixed JSON response");
                return result;
            }
            _logger.LogWarning("Failed to parse fixed JSON, will throw original error");
        }

        var shortPreview = responsePreview.Substring(0, Math.Min(500, responsePreview.Length));
        throw new GeminiResponseParseException(
            $"AI response was incomplete (truncated). This usually happens when the response is too long. " +
            $"Please try again or contact support. Error: {jsonEx.Message}",
            shortPreview,
            jsonEx);
    }

    public GeminiAnalysisResult ParseGeminiResponse(string geminiResponse)
    {
        string cleanedResponse = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(geminiResponse))
            {
                throw new ArgumentException("Gemini response is empty");
            }

            cleanedResponse = RemoveMarkdownCodeBlocks(geminiResponse);

            _logger.LogInformation("Parsing Gemini response. Original length: {OrigLen}, Cleaned length: {CleanLen}",
                geminiResponse.Length, cleanedResponse.Length);
            _logger.LogInformation("Original response preview (200 chars): {Preview}",
                geminiResponse.Substring(0, Math.Min(200, geminiResponse.Length)));
            _logger.LogInformation("Cleaned response preview (200 chars): {Preview}",
                cleanedResponse.Substring(0, Math.Min(200, cleanedResponse.Length)));

            var result = TryDeserializeGeminiResult(cleanedResponse);

            if (result == null)
            {
                _logger.LogError("Failed to parse Gemini response - result is null.");
                _logger.LogError("Full cleaned response: {Response}", cleanedResponse);
                _logger.LogError("Full original response: {Response}", geminiResponse);
                throw new JsonException("Failed to parse Gemini response - result is null");
            }

            _logger.LogInformation("Successfully parsed Gemini response. Diseases: {Count}, Specialties: {SpecialtyCount}",
                result.PossibleDiseases?.Count ?? 0, result.RecommendedSpecialties?.Count ?? 0);

            return result;
        }
        catch (JsonException jsonEx)
        {
            if (IsIncompleteJsonError(jsonEx))
            {
                var fixedResult = HandleIncompleteJson(geminiResponse, cleanedResponse, jsonEx);
                if (fixedResult != null)
                {
                    return fixedResult;
                }
            }

            var preview = (geminiResponse ?? string.Empty).Substring(0, Math.Min(500, (geminiResponse ?? string.Empty).Length));
            _logger.LogError(jsonEx, "JSON parsing error. Response preview: {Response}", preview);
            throw new GeminiResponseParseException(
                $"Failed to parse AI response as JSON: {jsonEx.Message}",
                preview,
                jsonEx);
        }
        catch (Exception ex)
        {
            var safeResponse = geminiResponse ?? string.Empty;
            var preview = safeResponse.Substring(0, Math.Min(500, safeResponse.Length));
            _logger.LogError(ex, "Error parsing Gemini response: {Message}. Response: {Response}",
                ex.Message, preview);
            throw new GeminiResponseParseException(
                $"Failed to parse AI response: {ex.Message}",
                preview,
                ex);
        }
    }

    /// <summary>
    /// Remove markdown code blocks from text (e.g., ```json ... ```)
    /// </summary>
    private string RemoveMarkdownCodeBlocks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Remove leading/trailing whitespace
        text = text.Trim();

        // Pattern 1: ```json ... ``` or ``` ... ```
        var codeBlockPattern = @"^```(?:json|JSON)?\s*\r?\n(.*?)\r?\n```\s*$";
        var match = System.Text.RegularExpressions.Regex.Match(text, codeBlockPattern,
            System.Text.RegularExpressions.RegexOptions.Singleline |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));

        if (match.Success && match.Groups.Count > 1)
        {
            var extractedJson = match.Groups[1].Value.Trim();
            _logger.LogDebug("Removed markdown code block wrapper. Extracted JSON length: {Length}", extractedJson.Length);
            return extractedJson;
        }

        // Pattern 2: ```json at start and ``` at end (multiline)
        if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
            {
                var afterFirstNewline = text.Substring(firstNewline + 1);
                var lastCodeBlock = afterFirstNewline.LastIndexOf("```", StringComparison.OrdinalIgnoreCase);
                if (lastCodeBlock > 0)
                {
                    var extractedJson = afterFirstNewline.Substring(0, lastCodeBlock).Trim();
                    _logger.LogDebug("Removed markdown code block wrapper (pattern 2). Extracted JSON length: {Length}", extractedJson.Length);
                    return extractedJson;
                }
            }
        }

        // Pattern 3: Just remove ```json and ``` if they exist
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(7).TrimStart();
        }
        else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(3).TrimStart();
        }

        if (text.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(0, text.Length - 3).TrimEnd();
        }

        return text.Trim();
    }

    /// <summary>
    /// Check if JSON brackets/braces are balanced
    /// </summary>
    private bool IsJsonBalanced(string json, out int missingBraces, out int missingBrackets)
    {
        var openBraces = json.Count(c => c == '{');
        var closeBraces = json.Count(c => c == '}');
        var openBrackets = json.Count(c => c == '[');
        var closeBrackets = json.Count(c => c == ']');

        missingBraces = openBraces - closeBraces;
        missingBrackets = openBrackets - closeBrackets;

        return missingBraces == 0 && missingBrackets == 0;
    }

    /// <summary>
    /// Try to fix incomplete string at the end of JSON
    /// </summary>
    private bool TryFixIncompleteString(StringBuilder fixedJson, string jsonStr)
    {
        var lastQuoteIndex = jsonStr.LastIndexOf('"');
        if (lastQuoteIndex <= 0 || lastQuoteIndex <= jsonStr.Length - 200)
        {
            return false;
        }

        var afterLastQuote = jsonStr.Substring(lastQuoteIndex + 1).Trim();

        // If there's text after the last quote but no closing quote, comma, brace, or bracket
        if (afterLastQuote.Length > 0 &&
            !afterLastQuote.Contains('"') &&
            !afterLastQuote.Contains(',') &&
            !afterLastQuote.Contains('}') &&
            !afterLastQuote.Contains(']'))
        {
            // Check if we're in a string value context
            var beforeLastQuote = jsonStr.Substring(0, lastQuoteIndex);
            var lastColonIndex = beforeLastQuote.LastIndexOf(':');

            if (lastColonIndex > 0 && lastColonIndex > lastQuoteIndex - 50)
            {
                fixedJson.Append('"');
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validate if fixed JSON is parseable
    /// </summary>
    private bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "JSON validation failed. JSON preview: {Preview}",
                json.Substring(0, Math.Min(100, json.Length)));
            return false;
        }
    }

    /// <summary>
    /// Try to fix incomplete JSON by closing open brackets/braces
    /// </summary>
    private string? TryFixIncompleteJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            // Check if JSON is already balanced
            if (IsJsonBalanced(json, out int missingBraces, out int missingBrackets))
            {
                return null;
            }

            var fixedJson = new StringBuilder(json.TrimEnd());
            var jsonStr = fixedJson.ToString();

            // Try to fix incomplete string at the end
            TryFixIncompleteString(fixedJson, jsonStr);

            // Close arrays first
            for (int i = 0; i < missingBrackets; i++)
            {
                fixedJson.Append(']');
            }

            // Close objects
            for (int i = 0; i < missingBraces; i++)
            {
                fixedJson.Append('}');
            }

            var result = fixedJson.ToString();

            // Validate the fixed JSON is parseable
            if (IsValidJson(result))
            {
                _logger.LogInformation("Successfully fixed incomplete JSON. Added {Braces} braces and {Brackets} brackets",
                    missingBraces, missingBrackets);
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fix incomplete JSON");
            return null;
        }
    }

    /// <summary>
    /// Count questions (not conclusions) in conversation history
    /// </summary>
    private int CountQuestionsInHistory(List<ConversationMessage> history)
    {
        return history
            .Count(m => m.Role?.ToLower() == "ai" &&
                       !m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) == true &&
                       !m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) == true &&
                       !m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) == true &&
                       (m.Content?.Contains("?") == true ||
                        m.Content?.Contains("cho tôi biết") == true ||
                        m.Content?.Contains("bạn có thể") == true));
    }


    /// <summary>
    /// Get list of questions already asked (for duplication check)
    /// </summary>
    private List<string> GetAskedQuestions(List<ConversationMessage> history)
    {
        return history
            .Where(m => m.Role?.ToLower() == "ai" &&
                       !string.IsNullOrEmpty(m.Content) &&
                       !m.Content.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) &&
                       !m.Content.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) &&
                       !m.Content.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) &&
                       (m.Content.Contains("?") ||
                        m.Content.Contains("cho tôi biết") ||
                        m.Content.Contains("bạn có thể")))
            .Select(m => m.Content)
            .ToList();
    }

    /// <summary>
    /// Append question rules to prompt
    /// </summary>
    private void AppendQuestionRules(StringBuilder sb)
    {
        sb.AppendLine("• PHẢI xem lại TẤT CẢ lịch sử hội thoại trước khi hỏi câu tiếp theo");
        sb.AppendLine("• DỰA VÀO câu trả lời của user trước đó để hỏi câu TIẾP THEO có logic");
        sb.AppendLine("• KHÔNG hỏi lại những câu đã hỏi (xem danh sách 'Đã hỏi' ở phần lịch sử)");
        sb.AppendLine("• Mỗi câu hỏi phải thu hẹp phạm vi bệnh/chuyên khoa dựa trên thông tin ĐÃ CÓ");
        sb.AppendLine("• Ví dụ: Nếu đã biết 'đau bụng vùng thượng vị' → KHÔNG hỏi lại 'vị trí đau ở đâu?'");
        sb.AppendLine("• Ví dụ: Đã biết 'đau bụng vùng thượng vị' → HỎI 'mức độ đau? thời gian? có nóng rát không?'");
    }

    /// <summary>
    /// Append requirements when asking more questions
    /// </summary>
    private void AppendWhenAskingMore(StringBuilder sb)
    {
        sb.AppendLine("   - DỰA VÀO câu trả lời trước đó của user để hỏi câu TIẾP THEO");
        sb.AppendLine("   - KHÔNG hỏi lại những câu đã hỏi (xem danh sách 'Đã hỏi')");
        sb.AppendLine("   - Câu hỏi phải giúp THU HẸP phạm vi bệnh/chuyên khoa dựa trên thông tin đã có");
    }

    /// <summary>
    /// Append requirements when concluding analysis
    /// </summary>
    private void AppendWhenConcluding(StringBuilder sb)
    {
        sb.AppendLine("⚠️ KHI analysisComplete = true, BẮT BUỘC PHẢI CÓ:");
        sb.AppendLine("  1. possibleDiseases: >= 1 bệnh (name, confidence, description)");
        sb.AppendLine("  2. recommendedSpecialties: >= 1 chuyên khoa (specialtyName, confidence, urgency, reasons)");
        sb.AppendLine("  3. generalAdvice: >= 2 lời khuyên");
        sb.AppendLine("  4. nextQuestions: [] (rỗng vì đã kết luận)");
    }

    /// <summary>
    /// Append JSON output format instructions
    /// </summary>
    private void AppendJsonOutputFormat(StringBuilder sb)
    {
        sb.AppendLine("   - Trả về JSON THUẦN (KHÔNG ```json), theo format ở trên");
        sb.AppendLine("   - Đảm bảo JSON hợp lệ, có đủ các field: possibleDiseases, recommendedSpecialties, generalAdvice, nextQuestions, analysisComplete");
    }

    /// <summary>
    /// Append core rules section to prompt
    /// </summary>
    private void AppendCoreRules(StringBuilder sb, int questionsAsked)
    {
        var maxQuestions = AiConversationRules.MaxQuestionsPerSession;
        sb.AppendLine("Bạn là trợ lý y tế AI của BookingCare. Nhiệm vụ: Hỏi để khoanh vùng bệnh → Gợi ý chuyên khoa.");
        sb.AppendLine();
        sb.AppendLine("⛔ QUY TẮC TUYỆT ĐỐI:");
        sb.AppendLine($"• PHẢI HỎI ĐỦ {maxQuestions} CÂU trước khi kết luận (hiện tại: {questionsAsked}/{maxQuestions}). DÙ CONFIDENCE CAO vẫn phải hỏi tiếp cho đến khi đủ.");
        sb.AppendLine($"• Nếu CHƯA ĐỦ {maxQuestions} câu → analysisComplete = false, nextQuestions = [1 câu HIGH priority duy nhất]");
        sb.AppendLine("• Triệu chứng mơ hồ (chỉ biết 'đau bụng', 'đau đầu') → HỎI vị trí/thời gian/mức độ");
        sb.AppendLine();
        sb.AppendLine("🎯 QUY TẮC HỎI THÊM (QUAN TRỌNG):");
        AppendQuestionRules(sb);
        sb.AppendLine();
    }

    /// <summary>
    /// Append examples section to prompt
    /// </summary>
    private void AppendExamples(StringBuilder sb)
    {
        sb.AppendLine("📌 VÍ DỤ QUAN TRỌNG:");
        sb.AppendLine();
        sb.AppendLine("❌ SAI - TUYỆT ĐỐI KHÔNG:");
        sb.AppendLine("User: 'Tôi bị đau bụng'");
        sb.AppendLine("{\"analysisComplete\": true, \"possibleDiseases\": [{\"name\":\"Viêm dạ dày\",\"confidence\":0.3}], \"recommendedSpecialties\": [...]}");
        sb.AppendLine("→ SAI vì: Chỉ biết 'đau bụng', không biết vị trí/thời gian/mức độ, confidence thấp!");
        sb.AppendLine();
        sb.AppendLine("✅ ĐÚNG:");
        sb.AppendLine("User: 'Tôi bị đau bụng'");
        sb.AppendLine("{\"analysisComplete\": false, \"possibleDiseases\": [], \"recommendedSpecialties\": [], \"generalAdvice\": [],");
        sb.AppendLine(" \"nextQuestions\": [{\"question\":\"Vị trí đau ở đâu? (Thượng vị/hạ vị/quanh rốn/toàn bộ?)\",\"priority\":\"HIGH\"}]}");
        sb.AppendLine();
    }

    /// <summary>
    /// Append confidence rules section to prompt
    /// </summary>
    private void AppendConfidenceRules(StringBuilder sb, int questionsAsked)
    {
        var maxQuestions = AiConversationRules.MaxQuestionsPerSession;
        sb.AppendLine("📊 NGƯỠNG CONFIDENCE & REQUIREMENTS:");
        sb.AppendLine($"• Hiện tại đã hỏi: {questionsAsked}/{maxQuestions} câu");
        sb.AppendLine($"• CHƯA ĐỦ {maxQuestions} câu → analysisComplete = false, nextQuestions BẮT BUỘC chứa đúng 1 câu HIGH priority tiếp theo");
        sb.AppendLine($"• Sau khi đã hỏi ĐỦ {maxQuestions} câu → PHẢI kết luận (analysisComplete = true) với đầy đủ diseases / specialties / advice, dù confidence cao hay thấp");
        sb.AppendLine();
        AppendWhenConcluding(sb);
        sb.AppendLine();
        sb.AppendLine("⛔ KHÔNG BAO GIỜ analysisComplete = true nếu thiếu bất kỳ thành phần nào trên!");
        sb.AppendLine();
    }

    /// <summary>
    /// Append JSON format schema section to prompt
    /// </summary>
    private void AppendJsonFormatSchema(StringBuilder sb)
    {
        sb.AppendLine("📤 JSON OUTPUT FORMAT (BẮT BUỘC):");
        sb.AppendLine();
        sb.AppendLine("Trả về JSON object với CẤU TRÚC CHÍNH XÁC SAU (KHÔNG dùng ```json):");
        sb.AppendLine("{");
        sb.AppendLine("  \"possibleDiseases\": [],");
        sb.AppendLine("  \"recommendedSpecialties\": [],");
        sb.AppendLine("  \"generalAdvice\": [],");
        sb.AppendLine("  \"nextQuestions\": [");
        sb.AppendLine("    {\"question\": \"Vị trí đau ở đâu?\", \"purpose\": \"Xác định vị trí\", \"priority\": \"HIGH\"}");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"analysisComplete\": false,");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("✅ Khi KẾT LUẬN (analysisComplete = true), ĐẦY ĐỦ:");
        sb.AppendLine("{");
        sb.AppendLine("  \"possibleDiseases\": [");
        sb.AppendLine("    {\"name\": \"Viêm dạ dày\", \"confidence\": 0.75, \"description\": \"Viêm niêm mạc dạ dày do ăn uống không điều độ\"},");
        sb.AppendLine("    {\"name\": \"Trào ngược dạ dày\", \"confidence\": 0.65, \"description\": \"Axit dạ dày trào ngược lên thực quản\"}");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"recommendedSpecialties\": [");
        sb.AppendLine("    {\"specialtyName\": \"Nội tiêu hóa - Gan mật\", \"confidence\": 0.85, \"urgency\": \"NORMAL\", \"reasons\": [\"Triệu chứng đau dạ dày rõ ràng\", \"Cần nội soi để chẩn đoán chính xác\"]}");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"generalAdvice\": [");
        sb.AppendLine("    \"Ăn nhiều bữa nhỏ trong ngày, tránh thức ăn cay nóng\",");
        sb.AppendLine("    \"Nếu đau kéo dài > 3 ngày hoặc có nóng rát, xuất huyết thì đến khám ngay\"");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"nextQuestions\": [],");
        sb.AppendLine("  \"analysisComplete\": true,");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("⚠️ TÓM TẮT:");
        sb.AppendLine("• analysisComplete=false → possibleDiseases=[], recommendedSpecialties=[], generalAdvice=[], nextQuestions=[1 câu]");
        sb.AppendLine("• analysisComplete=true → BẮT BUỘC: diseases >= 1, specialties >= 1, advice >= 2, nextQuestions=[]");
        sb.AppendLine();
    }

    /// <summary>
    /// Append conversation context section to prompt
    /// </summary>
    private void AppendConversationContext(StringBuilder sb, List<ConversationMessage> history)
    {
        sb.AppendLine("💬 LỊCH SỬ HỘI THOẠI (PHÂN TÍCH KỸ):");
        sb.AppendLine();

        // Show full conversation context (last 10 messages to get better context)
        var recentHistory = history.TakeLast(10).ToList();
        for (int i = 0; i < recentHistory.Count; i++)
        {
            var msg = recentHistory[i];
            sb.AppendLine($"[{msg.Role?.ToUpper() ?? "UNKNOWN"}]: {msg.Content}");
        }

        // Extract key information already gathered
        var userMessages = history.Where(m => m.Role?.ToLower() == "patient" || m.Role?.ToLower() == "user" || m.Role?.ToLower() == "guest").ToList();
        var askedQuestions = GetAskedQuestions(history);

        sb.AppendLine();
        sb.AppendLine("📋 THÔNG TIN ĐÃ THU THẬP:");
        if (userMessages.Any())
        {
            sb.AppendLine("• Câu trả lời của user:");
            foreach (var userMsg in userMessages.TakeLast(5))
            {
                var trimmed = userMsg.Content?.Length > 100 ? userMsg.Content.Substring(0, 100) + "..." : userMsg.Content;
                sb.AppendLine($"  - {trimmed}");
            }
        }
        else
        {
            sb.AppendLine("• Chưa có câu trả lời từ user");
        }

        if (askedQuestions.Any())
        {
            sb.AppendLine();
            sb.AppendLine("❓ ĐÃ HỎI (TUYỆT ĐỐI KHÔNG hỏi lại):");
            foreach (var q in askedQuestions.TakeLast(5))
            {
                var trimmed = q?.Length > 80 ? q.Substring(0, 80) + "..." : q;
                sb.AppendLine($"  - {trimmed}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("🎯 YÊU CẦU QUAN TRỌNG KHI HỎI TIẾP:");
        sb.AppendLine("1. DỰA VÀO câu trả lời của user ở trên để hỏi câu tiếp theo");
        sb.AppendLine("2. KHÔNG hỏi lại những gì đã hỏi (xem danh sách 'Đã hỏi' ở trên)");
        sb.AppendLine("3. Hỏi câu hỏi TIẾP THEO có logic, giúp khoanh vùng bệnh/chuyên khoa rõ hơn");
        sb.AppendLine("4. Nếu đã biết: vị trí → hỏi thời gian/mức độ/tính chất");
        sb.AppendLine("5. Nếu đã biết: thời gian → hỏi vị trí/các triệu chứng kèm theo");
        sb.AppendLine("6. Mục tiêu: Mỗi câu hỏi phải thu hẹp phạm vi bệnh/chuyên khoa dựa trên thông tin đã có");
        sb.AppendLine();
    }

    /// <summary>
    /// Append consult more request handling section to prompt
    /// </summary>
    private void AppendConsultMoreHandling(StringBuilder sb, string userMessage, List<ConversationMessage> history, int questionsAsked)
    {
        var maxQuestions = AiConversationRules.MaxQuestionsPerSession;
        bool isConsultMoreRequest = userMessage.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
                                     userMessage.Contains("hỏi thêm", StringComparison.OrdinalIgnoreCase) ||
                                     userMessage.Contains("cần thêm thông tin", StringComparison.OrdinalIgnoreCase);

        if (!isConsultMoreRequest || history == null || !history.Any())
        {
            return;
        }

        sb.AppendLine("⚠️ USER YÊU CẦU TƯ VẤN THÊM - XỬ LÝ ĐẶC BIỆT:");
        sb.AppendLine();

        if (questionsAsked >= maxQuestions)
        {
            sb.AppendLine($"• Đã hỏi đủ {questionsAsked}/{maxQuestions} câu → PHẢI kết luận với đầy đủ diseases/specialties/advice");
            sb.AppendLine("• Dựa vào TẤT CẢ thông tin đã thu thập ở trên để đưa ra kết luận chính xác");
        }
        else
        {
            sb.AppendLine($"• Mới hỏi {questionsAsked}/{maxQuestions} câu → HỎI THÊM 1 câu quan trọng");
            AppendWhenAskingMore(sb);
            sb.AppendLine("• Ví dụ: Nếu đã biết 'đau bụng vùng thượng vị' → hỏi 'mức độ đau? thời gian? có nóng rát không?'");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Append final guidelines section to prompt
    /// </summary>
    private void AppendFinalGuidelines(StringBuilder sb)
    {
        var maxQuestions = AiConversationRules.MaxQuestionsPerSession;
        sb.AppendLine();
        sb.AppendLine("==================================================");
        sb.AppendLine("✅ HƯỚNG DẪN CUỐI CÙNG:");
        sb.AppendLine("1. PHÂN TÍCH KỸ:");
        sb.AppendLine("   - Xem lại TẤT CẢ lịch sử hội thoại ở trên");
        sb.AppendLine("   - Xác định THÔNG TIN ĐÃ THU THẬP (câu trả lời của user)");
        sb.AppendLine("   - Xác định CÂU HỎI ĐÃ HỎI (không hỏi lại)");
        sb.AppendLine("   - Xác định THÔNG TIN CÒN THIẾU để khoanh vùng bệnh/chuyên khoa");
        sb.AppendLine();
        sb.AppendLine("2. QUYẾT ĐỊNH:");
        sb.AppendLine($"   - Nếu câu hỏi < {maxQuestions} → Hỏi thêm (analysisComplete=false)");
        sb.AppendLine($"   - Khi đã hỏi đủ {maxQuestions} câu → Kết luận (analysisComplete=true), tổng hợp đầy đủ thông tin");
        sb.AppendLine();
        sb.AppendLine("3. KHI HỎI THÊM (analysisComplete=false):");
        AppendWhenAskingMore(sb);
        sb.AppendLine("   - Ví dụ: Đã biết 'đau đầu' + 'vùng thái dương' → hỏi 'thời gian? mức độ? có buồn nôn không?'");
        sb.AppendLine();
        sb.AppendLine("4. KHI KẾT LUẬN (analysisComplete=true):");
        sb.AppendLine("   - Dựa vào TẤT CẢ thông tin đã thu thập để đưa ra kết luận chính xác");
        sb.AppendLine("   - BẮT BUỘC có đủ: diseases >= 1, specialties >= 1, advice >= 2");
        sb.AppendLine();
        sb.AppendLine("5. JSON OUTPUT:");
        AppendJsonOutputFormat(sb);
        sb.AppendLine("==================================================");
    }

    /// <summary>
    /// Append specialty list to prompt
    /// </summary>
    private void AppendSpecialtyList(StringBuilder sb)
    {
        sb.AppendLine("**DANH SÁCH CHUYÊN KHOA ĐƯỢC PHÉP:**");
        sb.AppendLine("QUAN TRỌNG: Bạn CHỈ được recommend các chuyên khoa sau đây. KHÔNG được recommend bất kỳ chuyên khoa nào khác ngoài danh sách này:");
        sb.AppendLine("1. Nội tổng quát");
        sb.AppendLine("2. Nội tim mạch");
        sb.AppendLine("3. Giải phẫu bệnh");
        sb.AppendLine("4. Chẩn đoán hình ảnh");
        sb.AppendLine("5. Huyết học truyền máu");
        sb.AppendLine("6. Nhi hô hấp");
        sb.AppendLine("7. Nội tiết - Tiểu đường");
        sb.AppendLine("8. Nội tiêu hóa - Gan mật");
        sb.AppendLine("9. Xét nghiệm di truyền");
        sb.AppendLine("10. Nhi dinh dưỡng");
        sb.AppendLine("11. Y học lao động");
        sb.AppendLine("12. Da liễu");
        sb.AppendLine("13. Phổi");
        sb.AppendLine("14. Nhi tim mạch");
        sb.AppendLine("15. Nhi tiêu hóa");
        sb.AppendLine("16. Nội hô hấp");
        sb.AppendLine("17. Nội thần kinh");
        sb.AppendLine("18. Thận nhân tạo");
        sb.AppendLine("19. Y học cổ truyền");
        sb.AppendLine("20. Nhi thận");
        sb.AppendLine("21. Nhi sơ sinh");
        sb.AppendLine("22. Nội thận - Tiết niệu");
        sb.AppendLine("23. Nội cơ xương khớp");
        sb.AppendLine("24. Nội huyết học");
        sb.AppendLine("25. Lão khoa");
        sb.AppendLine("26. Nội ung bướu");
        sb.AppendLine("27. Nhi tâm lý");
        sb.AppendLine("28. Thần kinh");
        sb.AppendLine("29. Bỏng - Tạo hình");
        sb.AppendLine("30. Nội cơ - Thần kinh");
        sb.AppendLine("31. Vật lý trị liệu");
        sb.AppendLine("32. Nhi dị ứng");
        sb.AppendLine("33. Hồi sức - Cấp cứu");
        sb.AppendLine("34. Chăm sóc giảm nhẹ");
        sb.AppendLine("35. Y học thể thao");
        sb.AppendLine("36. Nam khoa");
        sb.AppendLine("37. Xét nghiệm");
        sb.AppendLine("38. Nhãn khoa (Mắt)");
        sb.AppendLine("39. Nội dị ứng - Miễn dịch");
        sb.AppendLine("40. Nội lao và bệnh phổi");
        sb.AppendLine("41. Dị ứng - Miễn dịch lâm sàng");
        sb.AppendLine("42. Chỉnh hình nhi");
        sb.AppendLine("43. Nhi thần kinh");
        sb.AppendLine("44. Thận - Lọc máu");
        sb.AppendLine("45. Y học hạt nhân");
        sb.AppendLine("46. Tâm thần");
        sb.AppendLine("47. Ngoại tổng quát");
        sb.AppendLine("48. Răng - Hàm - Mặt");
        sb.AppendLine("49. Thẩm mỹ");
        sb.AppendLine("50. Tai - Mũi - Họng");
        sb.AppendLine("51. Phục hồi chức năng");
        sb.AppendLine("52. Ngoại thần kinh");
        sb.AppendLine("53. Ngoại tim mạch - Lồng ngực");
        sb.AppendLine("54. Ngoại gan mật - Tiêu hóa");
        sb.AppendLine("55. Chấn thương chỉnh hình");
        sb.AppendLine("56. Nhi nội tiết");
        sb.AppendLine("57. Nhiễm - Truyền nhiễm");
        sb.AppendLine("58. Ngoại tiết niệu");
        sb.AppendLine("59. Sản phụ khoa");
        sb.AppendLine("60. Nội cơ - Khớp");
        sb.AppendLine("61. Y học thẩm mỹ");
        sb.AppendLine("62. Nhi khoa");
        sb.AppendLine("63. Tim mạch can thiệp");
        sb.AppendLine("64. Pháp y");
        sb.AppendLine("65. Thiếu máu - Tan máu");
        sb.AppendLine("66. Rối loạn giấc ngủ");
        sb.AppendLine("67. Y học dự phòng");
        sb.AppendLine("68. Nhi tai mũi họng");
        sb.AppendLine("69. Dinh dưỡng");
        sb.AppendLine("70. Nội soi");
        sb.AppendLine();
        sb.AppendLine("**QUY TẮC VỀ CHUYÊN KHOA:**");
        sb.AppendLine("- Bạn PHẢI chỉ recommend các chuyên khoa trong danh sách trên");
        sb.AppendLine("- Tên chuyên khoa PHẢI khớp chính xác với một trong các tên trong danh sách (có thể không phân biệt hoa thường)");
        sb.AppendLine("- Nếu triệu chứng không phù hợp với bất kỳ chuyên khoa nào trong danh sách, hãy chọn chuyên khoa gần nhất hoặc \"Nội tổng quát\"");
    }

    private string BuildSymptomAnalysisPrompt(string userMessage, List<ConversationMessage>? history)
    {
        var sb = new StringBuilder();
        var questionsAsked = CountQuestionsInHistory(history ?? new List<ConversationMessage>());
        var maxQuestions = AiConversationRules.MaxQuestionsPerSession;

        AppendCoreRules(sb, questionsAsked);
        AppendExamples(sb);
        AppendConfidenceRules(sb, questionsAsked);
        AppendJsonFormatSchema(sb);

        sb.AppendLine("🚨 KHẨN CẤP: Đau ngực dữ dội, khó thở, xuất huyết, ngất, đột quỵ (Không trả cờ, chỉ đưa khuyến cáo trong message)");
        sb.AppendLine();

        AppendSpecialtyList(sb);
        sb.AppendLine();

        if (history != null && history.Any())
        {
            AppendConversationContext(sb, history);
        }

        sb.AppendLine($"👤 TIN NHẮN HIỆN TẠI: {userMessage}");
        sb.AppendLine();

        AppendConsultMoreHandling(sb, userMessage, history, questionsAsked);
        AppendFinalGuidelines(sb);

        return sb.ToString();
    }

    private object BuildGeminiRequest(string prompt)
    {
        // Build request with generation config
        // Note: responseMimeType might not be supported in v1, so we'll request JSON in the prompt instead
        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _settings.Temperature,
                topK = _settings.TopK,
                topP = _settings.TopP,
                maxOutputTokens = _settings.MaxTokens
                // Note: responseMimeType is not available in v1 API, JSON format is requested in prompt
            }
        };
    }

    /// <summary>
    /// Check for API error in response
    /// </summary>
    private void CheckForApiError(JsonDocument jsonDoc)
    {
        if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
        {
            var errorMessage = errorElement.GetProperty("message").GetString() ?? "Unknown error";
            _logger.LogError("Gemini API returned error: {Error}", errorMessage);
            throw new GeminiApiException(errorMessage, "API_ERROR");
        }
    }

    /// <summary>
    /// Get candidates array from response
    /// </summary>
    private JsonElement GetCandidates(JsonDocument jsonDoc, string responseBody)
    {
        if (!jsonDoc.RootElement.TryGetProperty("candidates", out var candidates))
        {
            _logger.LogError("No 'candidates' property in Gemini response. Response: {Response}", responseBody);
            throw new JsonException("No 'candidates' property found in Gemini response");
        }

        if (candidates.GetArrayLength() == 0)
        {
            _logger.LogError("Empty 'candidates' array in Gemini response. Response: {Response}", responseBody);
            throw new JsonException("Empty candidates array in Gemini response");
        }

        return candidates;
    }

    /// <summary>
    /// Check finish reason and handle special cases
    /// </summary>
    private void CheckFinishReason(JsonElement candidate)
    {
        if (!candidate.TryGetProperty("finishReason", out var finishReason))
        {
            return;
        }

        var finishReasonValue = finishReason.GetString();
        if (finishReasonValue == "STOP" || finishReasonValue == null)
        {
            return;
        }

        _logger.LogWarning("Gemini response finish reason: {Reason}", finishReasonValue);

        if (finishReasonValue == "SAFETY")
        {
            throw new GeminiApiException(
                "Gemini API blocked the content for safety reasons",
                "SAFETY_BLOCKED");
        }

        if (finishReasonValue == "MAX_TOKENS")
        {
            _logger.LogWarning("Gemini response was truncated due to MAX_TOKENS limit. Consider increasing MaxTokens in config.");
        }
    }

    /// <summary>
    /// Extract text from candidate content
    /// </summary>
    private string ExtractTextFromCandidate(JsonElement candidate, string responseBody)
    {
        if (!candidate.TryGetProperty("content", out var content))
        {
            _logger.LogError("No 'content' property in candidate. Response: {Response}", responseBody);
            throw new JsonException("No 'content' property found in candidate");
        }

        if (!content.TryGetProperty("parts", out var parts))
        {
            _logger.LogError("No 'parts' property in content. Response: {Response}", responseBody);
            throw new JsonException("No 'parts' property found in content");
        }

        if (parts.GetArrayLength() == 0)
        {
            _logger.LogError("Empty 'parts' array. Response: {Response}", responseBody);
            throw new JsonException("Empty parts array in Gemini response");
        }

        var textPart = parts[0];
        if (!textPart.TryGetProperty("text", out var textElement))
        {
            _logger.LogError("No 'text' property in part. Response: {Response}", responseBody);
            throw new JsonException("No 'text' property found in part");
        }

        var text = textElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogError("Empty text in Gemini response. Response: {Response}", responseBody);
            throw new JsonException("Empty text in Gemini response");
        }

        return text;
    }

    private string ExtractTextFromGeminiResponse(string responseBody)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new ArgumentException("Response body is empty");
            }

            _logger.LogDebug("Extracting text from Gemini response. Length: {Length}", responseBody.Length);

            var jsonDoc = JsonDocument.Parse(responseBody);

            // Check for API errors
            CheckForApiError(jsonDoc);

            // Get candidates array
            var candidates = GetCandidates(jsonDoc, responseBody);
            var firstCandidate = candidates[0];

            // Check finish reason
            CheckFinishReason(firstCandidate);

            // Extract text from candidate
            var text = ExtractTextFromCandidate(firstCandidate, responseBody);

            // Remove markdown code blocks if present
            text = RemoveMarkdownCodeBlocks(text);

            _logger.LogDebug("Successfully extracted text from Gemini response. Text length: {Length}", text.Length);
            return text;
        }
        catch (JsonException jsonEx)
        {
            var safeBody = responseBody ?? string.Empty;
            var preview = safeBody.Substring(0, Math.Min(500, safeBody.Length));
            _logger.LogError(jsonEx, "JSON parsing error extracting text. Response: {Response}", preview);
            throw new GeminiResponseParseException(
                $"Failed to extract text from Gemini response: {jsonEx.Message}",
                preview,
                jsonEx);
        }
        catch (Exception ex)
        {
            var safeBody = responseBody ?? string.Empty;
            var preview = safeBody.Substring(0, Math.Min(500, safeBody.Length));
            _logger.LogError(ex, "Error extracting text from Gemini response: {Message}. Response: {Response}",
                ex.Message, preview);
            throw new GeminiResponseParseException(
                $"Failed to extract text from Gemini response: {ex.Message}",
                preview,
                ex);
        }
    }
}


