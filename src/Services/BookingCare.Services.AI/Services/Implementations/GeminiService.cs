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
                var models = await TryFetchModelsFromEndpoint(endpoint);
                if (models.Any())
                {
                    return models;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch available models, will use fallback models");
        }

        return new List<string>();
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
            var options = GetJsonSerializerOptions();
            return JsonSerializer.Deserialize<GeminiAnalysisResult>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to deserialize Gemini result. This is expected for incomplete JSON.");
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

            _logger.LogDebug("Parsing Gemini response (cleaned). Length: {Length}", cleanedResponse.Length);
            _logger.LogDebug("Original response preview: {Preview}", geminiResponse.Substring(0, Math.Min(200, geminiResponse.Length)));

            var result = TryDeserializeGeminiResult(cleanedResponse);

            if (result == null)
            {
                _logger.LogError("Failed to parse Gemini response - result is null. Response: {Response}", cleanedResponse);
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
    /// Find last "tư vấn thêm" request index in history
    /// </summary>
    private int FindLastConsultMoreIndex(List<ConversationMessage> history)
    {
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if ((history[i].Role?.ToLower() == "patient" || history[i].Role?.ToLower() == "guest" || history[i].Role?.ToLower() == "user") &&
                (history[i].Content?.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true ||
                 history[i].Content?.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase) == true))
            {
                return i;
            }
        }
        return -1;
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
    /// Handle "tư vấn thêm" request and append instructions
    /// </summary>
    private void HandleConsultMoreRequest(StringBuilder sb, List<ConversationMessage>? history)
    {
        sb.AppendLine();
        sb.AppendLine("**YÊU CẦU ĐẶC BIỆT - TƯ VẤN THÊM:**");
        sb.AppendLine("Người dùng yêu cầu tư vấn thêm để khoanh vùng bệnh chính xác hơn.");

        var followUpCount = 0;
        if (history != null && history.Any())
        {
            var lastConsultIndex = FindLastConsultMoreIndex(history);
            if (lastConsultIndex >= 0)
            {
                followUpCount = history.Skip(lastConsultIndex + 1)
                    .Count(m => m.Role?.ToLower() == "ai" &&
                               !m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) == true &&
                               !m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) == true &&
                               !m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) == true &&
                               (m.Content?.Contains("?") == true ||
                                m.Content?.Contains("cho tôi biết") == true));
            }
        }

        if (followUpCount >= 3)
        {
            sb.AppendLine("ĐÃ HỎI ĐỦ 3 CÂU SAU 'TƯ VẤN THÊM' - BẮT BUỘC PHẢI KẾT LUẬN ĐẦY ĐỦ:");
            sb.AppendLine("1. analysisComplete: PHẢI set = true");
            sb.AppendLine("2. possibleDiseases: PHẢI có ít nhất 2-3 bệnh với tỉ lệ confidence");
            sb.AppendLine("3. recommendedSpecialties: PHẢI có ít nhất 1 chuyên khoa phù hợp");
            sb.AppendLine("4. generalAdvice: PHẢI có ít nhất 2-3 lời khuyên cụ thể, chi tiết");
            sb.AppendLine("5. nextQuestions: PHẢI để trống []");
            sb.AppendLine();
            sb.AppendLine("KHÔNG ĐƯỢC THIẾU BẤT KỲ THÀNH PHẦN NÀO! Đây là kết luận cuối cùng.");
        }
        else
        {
            sb.AppendLine("BẮT BUỘC: Hỏi 1 câu hỏi cụ thể, tập trung vào:");
            sb.AppendLine("- Vị trí chính xác của triệu chứng");
            sb.AppendLine("- Thời gian và tần suất xuất hiện");
            sb.AppendLine("- Mức độ nghiêm trọng");
            sb.AppendLine("- Các yếu tố làm tăng/giảm triệu chứng");
            sb.AppendLine($"Đã hỏi: {followUpCount}/3 câu sau 'tư vấn thêm'");
            sb.AppendLine();
            sb.AppendLine("**QUAN TRỌNG - KHI ĐANG HỎI:**");
            sb.AppendLine("1. analysisComplete: set = false (vì đang hỏi thêm)");
            sb.AppendLine("2. nextQuestions: 1 câu hỏi để khoanh vùng thêm");
            sb.AppendLine("3. possibleDiseases: có thể để trống [] hoặc 1-2 bệnh sơ bộ");
            sb.AppendLine("4. recommendedSpecialties: có thể để trống []");
            sb.AppendLine("5. generalAdvice: có thể để trống []");
            sb.AppendLine();
            sb.AppendLine("CHỈ khi đã hỏi đủ 3 câu thì mới set analysisComplete = true và trả về đầy đủ.");
        }
    }

    /// <summary>
    /// Append conversation history context to prompt
    /// </summary>
    private void AppendConversationHistory(StringBuilder sb, List<ConversationMessage> history, int lastConsultMoreIndex)
    {
        var questionsAfterConsultMore = 0;
        if (lastConsultMoreIndex >= 0)
        {
            questionsAfterConsultMore = history
                .Skip(lastConsultMoreIndex + 1)
                .Count(m => m.Role?.ToLower() == "ai" &&
                           !m.Content?.Contains("Dựa trên các triệu chứng", StringComparison.OrdinalIgnoreCase) == true &&
                           !m.Content?.Contains("Lời khuyên chung", StringComparison.OrdinalIgnoreCase) == true &&
                           !m.Content?.Contains("Chuyên khoa phù hợp", StringComparison.OrdinalIgnoreCase) == true &&
                           (m.Content?.Contains("?") == true ||
                            m.Content?.Contains("cho tôi biết") == true ||
                            m.Content?.Contains("bạn có thể") == true));
        }

        var totalQuestionsAsked = CountQuestionsInHistory(history);

        sb.AppendLine("**LỊCH SỬ HỘI THOẠI TRƯỚC ĐÂY:**");

        if (lastConsultMoreIndex >= 0)
        {
            sb.AppendLine($"Người dùng đã yêu cầu 'Tư vấn thêm'. Số câu hỏi đã hỏi sau đó: {questionsAfterConsultMore}/3");
            if (questionsAfterConsultMore >= 3)
            {
                sb.AppendLine("QUAN TRỌNG: Đã hỏi đủ 3 câu hỏi khoanh vùng sau 'Tư vấn thêm', PHẢI kết luận và recommend specialty ngay.");
            }
        }
        else
        {
            sb.AppendLine($"Số câu hỏi đã hỏi: {totalQuestionsAsked}/3");
            if (totalQuestionsAsked >= 3)
            {
                sb.AppendLine("QUAN TRỌNG: Đã hỏi đủ 3 câu hỏi, KHÔNG được hỏi thêm nữa. PHẢI kết luận và recommend specialty ngay.");
            }
        }

        sb.AppendLine();

        // List ONLY questions already asked (not conclusions) to avoid duplication
        sb.AppendLine("**CÁC CÂU HỎI ĐÃ HỎI (KHÔNG ĐƯỢC HỎI LẠI):**");
        var askedQuestions = GetAskedQuestions(history);

        if (askedQuestions.Any())
        {
            foreach (var q in askedQuestions)
            {
                sb.AppendLine($"- {q}");
            }
            sb.AppendLine("QUAN TRỌNG: Phải hỏi câu hỏi KHÁC, không trùng với các câu trên!");
        }
        sb.AppendLine();

        sb.AppendLine("**CHI TIẾT LỊCH SỬ HỘI THOẠI:**");
        foreach (var msg in history.TakeLast(5)) // Only last 5 messages for context
        {
            sb.AppendLine($"[{msg.Role.ToUpper()}]: {msg.Content}");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Append common questioning strategy rules to prompt
    /// </summary>
    private void AppendQuestioningStrategy(StringBuilder sb)
    {
        sb.AppendLine("**CHIẾN LƯỢC ĐẶT CÂU HỎI THEO THỨ TỰ ƯU TIÊN:**");
        sb.AppendLine();
        sb.AppendLine("**Stage 1 (HIGH Priority) - Hỏi TRƯỚC TIÊN nếu thiếu:**");
        sb.AppendLine("- Triệu chứng chính: Vị trí, tính chất (đau, sốt, nôn, ...)");
        sb.AppendLine("- Thời gian: Khi nào bắt đầu? Kéo dài bao lâu?");
        sb.AppendLine("- Mức độ: Nhẹ, trung bình, nặng? Có ảnh hưởng sinh hoạt không?");
        sb.AppendLine();
        sb.AppendLine("**Stage 2 (MEDIUM Priority) - Hỏi SAU Stage 1 nếu cần:**");
        sb.AppendLine("- Severity: Mức độ nghiêm trọng, có tăng dần không?");
        sb.AppendLine("- Triggers: Yếu tố khởi phát (ăn uống, vận động, thời tiết, ...)");
        sb.AppendLine("- Đặc điểm: Đau liên tục hay từng cơn? Có lan tỏa không?");
        sb.AppendLine();
        sb.AppendLine("**Stage 3 (LOW Priority) - Hỏi CUỐI CÙNG nếu vẫn cần:**");
        sb.AppendLine("- Additional context: Tiền sử bệnh, thuốc đang dùng, dị ứng");
        sb.AppendLine("- Đi kèm: Các triệu chứng khác (sốt, mệt mỏi, chán ăn, ...)");
        sb.AppendLine();
        sb.AppendLine("**QUY TẮC QUAN TRỌNG:**");
        sb.AppendLine("- CHỈ HỎI 1 CÂU HỎI MỖI LẦN - KHÔNG BAO GIỜ hỏi nhiều câu cùng lúc");
        sb.AppendLine("- TỐI ĐA CHỈ HỎI 3 CÂU HỎI - Sau 3 câu hỏi, PHẢI kết luận và recommend specialty ngay");
        sb.AppendLine("- Đếm số câu hỏi đã hỏi trong lịch sử hội thoại, nếu đã hỏi 3 câu thì KHÔNG hỏi thêm nữa");
        sb.AppendLine("- Ưu tiên hỏi HIGH priority trước, chỉ hỏi MEDIUM/LOW khi đã có thông tin Stage 1");
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

        // System instructions
        sb.AppendLine("Bạn là một trợ lý y tế AI chuyên nghiệp của hệ thống đặt lịch khám bệnh BookingCare.");
        sb.AppendLine();
        sb.AppendLine("**NHIỆM VỤ CỦA BẠN:**");
        sb.AppendLine("1. Phân tích triệu chứng người dùng nhập (ví dụ: \"Tôi bị đau bụng\", \"Tôi chóng mặt và buồn nôn\").");
        sb.AppendLine("2. Liệt kê tối đa 5 bệnh có khả năng liên quan, kèm confidence (độ tin cậy từ 0–1).");
        sb.AppendLine("3. Nếu thông tin chưa đủ, đặt câu hỏi bổ sung theo **thứ tự ưu tiên** để làm rõ triệu chứng.");
        sb.AppendLine("4. Khi đã đủ thông tin, hãy xác định **chuyên khoa phù hợp nhất** để khám bệnh.");
        sb.AppendLine("5. Tuyệt đối KHÔNG chẩn đoán chính thức, kê đơn, hoặc khẳng định người dùng mắc bệnh.");
        sb.AppendLine("6. Chỉ đưa ra **gợi ý định hướng y tế** và **chuyên khoa phù hợp** để khám.");
        sb.AppendLine("7. Tất cả output phải là JSON hợp lệ theo định dạng quy định.");
        sb.AppendLine("8. Nếu người dùng đã trả lời các câu hỏi trước đó, sử dụng thông tin này để cập nhật phân tích và loại bỏ các câu hỏi trùng lặp.");
        sb.AppendLine();

        // Emergency Detection Keywords
        sb.AppendLine("**PHÁT HIỆN TRƯỜNG HỢP KHẨN CẤP:**");
        sb.AppendLine("Nếu phát hiện các từ khóa sau trong triệu chứng, ĐẶT requiresImmediateAttention = true NGAY LẬP TỨC:");
        sb.AppendLine("- Đau ngực dữ dội, đau ngực đột ngột, đau thắt ngực");
        sb.AppendLine("- Khó thở, thở gấp, nghẹt thở, không thở được");
        sb.AppendLine("- Chảy máu nhiều, mất máu, xuất huyết");
        sb.AppendLine("- Ngất xỉu, bất tỉnh, mất ý thức");
        sb.AppendLine("- Đột quỵ, tai biến, liệt đột ngột");
        sb.AppendLine("- Co giật, động kinh");
        sb.AppendLine("- Sốc phản vệ, dị ứng nặng");
        sb.AppendLine("- Đau bụng dữ dội đột ngột");
        sb.AppendLine("- Nôn ra máu, ho ra máu");
        sb.AppendLine("Khi requiresImmediateAttention = true, urgency PHẢI là EMERGENCY và đề xuất gọi 115.");
        sb.AppendLine();

        // Priority-based Questioning
        AppendQuestioningStrategy(sb);
        sb.AppendLine();
        sb.AppendLine("**XỬ LÝ KHI NHẬN 'tôi muốn được tư vấn thêm':**");
        sb.AppendLine("- Nếu người dùng gửi 'tôi muốn được tư vấn thêm' hoặc 'tư vấn thêm', đây là yêu cầu khoanh vùng bệnh chi tiết hơn");
        sb.AppendLine("- Ngay lập tức hỏi 1 câu hỏi quan trọng nhất (HIGH priority) để làm rõ triệu chứng");
        sb.AppendLine("- QUAN TRỌNG: KHÔNG HỎI LẠI các câu hỏi đã hỏi trong lịch sử hội thoại");
        sb.AppendLine("- Phải xem lại lịch sử và loại bỏ các câu hỏi đã được trả lời");
        sb.AppendLine("- Tập trung vào các câu hỏi khoanh vùng bệnh cụ thể, không hỏi chung chung");
        sb.AppendLine("- Sau khi hỏi tối đa 3 câu, PHẢI đưa ra kết luận và gợi ý chuyên khoa phù hợp");
        sb.AppendLine("- Chọn câu hỏi quan trọng nhất (HIGH priority) và chỉ hỏi câu đó");
        sb.AppendLine("- Sau khi người dùng trả lời, mới hỏi câu tiếp theo");
        sb.AppendLine("- Các câu hỏi phải hỏi theo thứ tự ưu tiên để khoanh vùng bệnh và gen ra câu hỏi tiếp theo");
        sb.AppendLine("- Nếu đã có đủ thông tin Stage 1 và confidence > 0.5, có thể recommend specialty ngay");
        sb.AppendLine("- Nếu confidence < 0.5 sau Stage 1, hỏi thêm Stage 2 (nhưng vẫn chỉ 1 câu mỗi lần)");
        sb.AppendLine();
        sb.AppendLine("**QUY TẮC HIỂN THỊ MESSAGE:**");
        sb.AppendLine("- KHI analysisComplete = false (đang hỏi thêm):");
        sb.AppendLine("  + Field \"message\" CHỈ chứa câu hỏi đơn giản, ngắn gọn");
        sb.AppendLine("  + KHÔNG cần phần \"Dựa trên các triệu chứng...\", \"Lời khuyên chung\"");
        sb.AppendLine("  + possibleDiseases: có thể để trống [] hoặc chỉ 1-2 bệnh sơ bộ");
        sb.AppendLine("  + recommendedSpecialties: có thể để trống [] hoặc chuyên khoa sơ bộ");
        sb.AppendLine("  + generalAdvice: có thể để trống []");
        sb.AppendLine("  + nextQuestions: PHẢI có 1 câu hỏi cụ thể");
        sb.AppendLine();
        sb.AppendLine("- KHI analysisComplete = true (kết luận):");
        sb.AppendLine("  + Field \"message\" chứa phân tích ĐẦY ĐỦ:");
        sb.AppendLine("    \"Dựa trên các triệu chứng bạn mô tả, có thể liên quan đến:\"");
        sb.AppendLine("    + Liệt kê possibleDiseases với confidence");
        sb.AppendLine("    + General advice");
        sb.AppendLine("    + Recommended specialties");
        sb.AppendLine("  + possibleDiseases: PHẢI có ít nhất 2-3 bệnh");
        sb.AppendLine("  + recommendedSpecialties: PHẢI có ít nhất 1 chuyên khoa");
        sb.AppendLine("  + generalAdvice: PHẢI có ít nhất 2 lời khuyên");
        sb.AppendLine("  + nextQuestions: có thể rỗng []");
        sb.AppendLine();

        // Confidence Thresholds
        sb.AppendLine("**NGƯỠNG CONFIDENCE:**");
        sb.AppendLine("- confidence > 0.8: Recommend ngay 1 chuyên khoa chính xác");
        sb.AppendLine("- confidence 0.5-0.8: Show 2-3 chuyên khoa options");
        sb.AppendLine("- confidence < 0.5: Hỏi thêm trước khi recommend");
        sb.AppendLine("- NGOẠI LỆ: Nếu đã hỏi 3 câu (ở bất kỳ giai đoạn nào), PHẢI set analysisComplete = true");
        sb.AppendLine("- NGOẠI LỆ: Sau 3 câu hỏi tư vấn thêm, PHẢI kết luận với analysisComplete = true");
        sb.AppendLine();

        sb.AppendLine("**ĐỊNH DẠNG OUTPUT (JSON):**");
        sb.AppendLine("QUAN TRỌNG: Chỉ trả về JSON thuần, KHÔNG bao bọc trong markdown code blocks (không dùng ```json hoặc ```).");
        sb.AppendLine("Chỉ trả về JSON object trực tiếp, ví dụ:");
        sb.AppendLine("{");
        sb.AppendLine("  \"possibleDiseases\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"name\": \"Tên bệnh tiếng Việt\",");
        sb.AppendLine("      \"confidence\": 0.75,");
        sb.AppendLine("      \"description\": \"Mô tả ngắn gọn về bệnh\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"nextQuestions\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"question\": \"Câu hỏi cụ thể\",");
        sb.AppendLine("      \"purpose\": \"Lý do cần hỏi (Stage 1/2/3)\",");
        sb.AppendLine("      \"priority\": \"HIGH hoặc MEDIUM hoặc LOW\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"recommendedSpecialties\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"specialtyName\": \"Tên chuyên khoa tiếng Việt\",");
        sb.AppendLine("      \"confidence\": 0.85,");
        sb.AppendLine("      \"urgency\": \"EMERGENCY hoặc URGENT hoặc NORMAL hoặc ROUTINE\",");
        sb.AppendLine("      \"reasons\": [\"Lý do 1\", \"Lý do 2\"]");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"generalAdvice\": [");
        sb.AppendLine("    \"Lời khuyên chung 1\",");
        sb.AppendLine("    \"Lời khuyên chung 2\"");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"analysisComplete\": true hoặc false,");
        sb.AppendLine("  \"requiresImmediateAttention\": true hoặc false");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("**QUAN TRỌNG VỀ analysisComplete:**");
        sb.AppendLine("- Set true khi: đã hỏi 3 câu HOẶC confidence > 0.5 HOẶC đã có đủ thông tin");
        sb.AppendLine("- Set false khi: cần hỏi thêm để khoanh vùng bệnh");
        sb.AppendLine();
        sb.AppendLine("**BẮT BUỘC KHI analysisComplete = true (KHÔNG ĐƯỢC THIẾU):**");
        sb.AppendLine("1. possibleDiseases: PHẢI có ít nhất 2-3 bệnh với confidence");
        sb.AppendLine("   Ví dụ: [{\"name\": \"Viêm dạ dày\", \"confidence\": 0.65}, {\"name\": \"Rối loạn tiêu hóa\", \"confidence\": 0.60}]");
        sb.AppendLine("2. recommendedSpecialties: PHẢI có ít nhất 1 chuyên khoa");
        sb.AppendLine("   Ví dụ: [{\"specialtyName\": \"Nội tiêu hóa - Gan mật\", \"confidence\": 0.70}]");
        sb.AppendLine("3. generalAdvice: PHẢI có ít nhất 2-3 lời khuyên cụ thể");
        sb.AppendLine("   Ví dụ: [\"Theo dõi triệu chứng trong 24-48 giờ\", \"Tránh thức ăn cay nóng\", \"Uống đủ nước\"]");
        sb.AppendLine("4. nextQuestions: để trống []");
        sb.AppendLine();
        sb.AppendLine("NGHIÊM NGẶT: Nếu thiếu BẤT KỲ thành phần nào trong 3 mục trên, response sẽ bị từ chối!");
        sb.AppendLine();
        AppendSpecialtyList(sb);
        sb.AppendLine();
        sb.AppendLine("**LƯU Ý AN TOÀN:**");
        sb.AppendLine("- Luôn nhắc người dùng đây chỉ là gợi ý, không thay thế khám bác sĩ.");
        sb.AppendLine("- Dùng ngôn ngữ thân thiện, chuyên nghiệp, dễ hiểu.");
        sb.AppendLine();

        // Add conversation history if available
        if (history != null && history.Any())
        {
            var lastConsultMoreIndex = FindLastConsultMoreIndex(history);
            AppendConversationHistory(sb, history, lastConsultMoreIndex);
        }

        // Current user message
        sb.AppendLine("**TIN NHẮN HIỆN TẠI CỦA NGƯỜI DÙNG:**");
        sb.AppendLine(userMessage);

        // Check if this is ONLY "tư vấn thêm" without any symptoms
        bool isOnlyConsultMore = (userMessage.Equals("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
                                  userMessage.Equals("tư vấn thêm", StringComparison.OrdinalIgnoreCase)) &&
                                 (history == null || !history.Any());

        if (isOnlyConsultMore)
        {
            // User just clicked "Tư vấn thêm" without providing any symptoms first
            sb.AppendLine();
            sb.AppendLine("**TRƯỜNG HỢP ĐẶC BIỆT - CHƯA CÓ TRIỆU CHỨNG:**");
            sb.AppendLine("Người dùng yêu cầu tư vấn nhưng chưa cung cấp triệu chứng.");
            sb.AppendLine("PHẢI:");
            sb.AppendLine("1. Hỏi về triệu chứng cụ thể người dùng đang gặp phải");
            sb.AppendLine("2. possibleDiseases: để trống []");
            sb.AppendLine("3. recommendedSpecialties: [{\"specialtyName\": \"Nội tổng quát\", \"confidence\": 0.5}]");
            sb.AppendLine("4. generalAdvice: [\"Hãy mô tả chi tiết triệu chứng bạn đang gặp phải\", \"Cung cấp thông tin về thời gian xuất hiện triệu chứng\"]");
            sb.AppendLine("5. nextQuestions: [{\"question\": \"Bạn đang gặp phải triệu chứng gì khiến bạn cần tư vấn y tế?\", \"purpose\": \"Thu thập triệu chứng ban đầu\", \"priority\": \"HIGH\"}]");
            sb.AppendLine("6. analysisComplete: false");
        }
        // Special handling for "tư vấn thêm" request with existing symptoms
        else if (userMessage.Contains("tôi muốn được tư vấn thêm", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.Contains("tư vấn thêm", StringComparison.OrdinalIgnoreCase))
        {
            HandleConsultMoreRequest(sb, history);
        }

        sb.AppendLine();
        sb.AppendLine("Hãy phân tích và trả về JSON theo định dạng trên.");
        sb.AppendLine("QUAN TRỌNG: Chỉ trả về JSON object thuần, KHÔNG dùng markdown code blocks (```json hoặc ```).");
        sb.AppendLine("KHÔNG thêm bất kỳ text nào khác ngoài JSON object.");

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
            throw;
        }
    }
}


