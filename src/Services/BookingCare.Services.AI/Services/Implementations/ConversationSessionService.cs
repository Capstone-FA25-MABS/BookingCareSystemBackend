using System.Text.Json;
using System.Text.RegularExpressions;
using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Exceptions;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for managing conversation sessions
/// </summary>
public class ConversationSessionService : IConversationSessionService
{
    private readonly AiDbContext _context;
    private readonly ILogger<ConversationSessionService> _logger;

    public ConversationSessionService(
        AiDbContext context,
        ILogger<ConversationSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> GetOrCreateSessionAsync(Guid? sessionId, Guid userId, LocationContext? location)
    {
        try
        {
            // If sessionId is provided, check if it exists and belongs to the user
            if (sessionId.HasValue)
            {
                var existingSession = await _context.ConversationSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == userId);

                if (existingSession != null)
                {
                    return existingSession.Id;
                }
            }

            // Create new session
            // Note: CreatedAt and UpdatedAt will be set automatically by DbContext.UpdateTimestamps()
            var newSession = new Models.Entities.ConversationSessionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConversationHistory = "[]"
            };

            _context.ConversationSessions.Add(newSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new conversation session: {SessionId} for user: {UserId}", newSession.Id, userId);

            return newSession.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating session: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            throw new ConversationSessionException(
                $"Failed to get or create conversation session: {ex.Message}",
                sessionId,
                userId,
                ex);
        }
    }

    public async Task<List<ConversationMessage>> LoadConversationHistoryAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null || string.IsNullOrWhiteSpace(session.ConversationHistory))
            {
                return new List<ConversationMessage>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
            };

            var history = JsonSerializer.Deserialize<List<ConversationMessage>>(
                session.ConversationHistory,
                options
            );

            var result = history ?? new List<ConversationMessage>();

            // Normalize line breaks in loaded messages (to fix old data with \r\n\r\n, \n\n issues)
            foreach (var message in result)
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    message.Content = NormalizeLineBreaks(message.Content);
                }
            }

            // Log để debug suggestions
            var aiMessagesWithSuggestions = result.Where(m => m.Role == "ai" && m.Suggestions != null).ToList();
            if (aiMessagesWithSuggestions.Any())
            {
                _logger.LogDebug("Loaded {Count} AI messages with suggestions from session {SessionId}",
                    aiMessagesWithSuggestions.Count, sessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading conversation history for session {SessionId}", sessionId);
            return new List<ConversationMessage>();
        }
    }

    public async Task SaveConversationHistoryAsync(
        Guid sessionId,
        string userMessage,
        string aiMessage,
        LocationContext? location = null,
        object? suggestions = null,
        Guid? userId = null,
        object? disease = null,
        int? questionCount = null,
        bool? analysisComplete = null)
    {
        try
        {
            var session = await _context.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for saving history", sessionId);
                return;
            }

            // Load existing history
            var history = await LoadConversationHistoryAsync(sessionId);

            // Determine user role: "patient" if logged in, "guest" if not
            var userRole = userId.HasValue ? "patient" : "guest";

            // Normalize line breaks before saving to database
            var normalizedUserMessage = NormalizeLineBreaks(userMessage);
            var normalizedAiMessage = NormalizeLineBreaks(aiMessage);

            // Add new messages
            history.Add(new ConversationMessage
            {
                Role = userRole,
                Content = normalizedUserMessage,
                Timestamp = DateTime.UtcNow
            });

            var aiMessageObj = new ConversationMessage
            {
                Role = "ai",
                Content = normalizedAiMessage,
                Timestamp = DateTime.UtcNow,
                Suggestions = suggestions, // Save suggestions with AI message
                Disease = disease, // Save disease conclusion
                QuestionCount = questionCount, // Save question count for progress
                AnalysisComplete = analysisComplete // Save completion flag
            };

            history.Add(aiMessageObj);

            // Update title if this is the first user message (session title is null or default)
            if (string.IsNullOrWhiteSpace(session.Title) || session.Title == "Cuộc trò chuyện mới")
            {
                // Extract title from first user message (truncate to 50 chars)
                var title = userMessage.Length > 50
                    ? userMessage.Substring(0, 50) + "..."
                    : userMessage;
                session.Title = title;
                _logger.LogDebug("Updated session {SessionId} title to: {Title}", sessionId, title);
            }

            // Keep only last 50 messages to prevent database bloat
            if (history.Count > 50)
            {
                history = history.Skip(history.Count - 50).ToList();
            }

            // Update session - serialize with options to ensure suggestions are properly serialized
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Log suggestions before serialization for debugging
            if (suggestions != null)
            {
                var suggestionsJson = JsonSerializer.Serialize(suggestions, options);
                _logger.LogDebug("Saving suggestions for AI message: {SuggestionsJson}", suggestionsJson);
            }

            session.ConversationHistory = JsonSerializer.Serialize(history, options);
            // Note: UpdatedAt will be set automatically by DbContext.UpdateTimestamps()

            _logger.LogInformation("Saved conversation history with {Count} messages. Last AI message has suggestions: {HasSuggestions}, disease: {HasDisease}",
                history.Count,
                aiMessageObj.Suggestions != null,
                aiMessageObj.Disease != null);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved conversation history for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation history for session {SessionId}", sessionId);
            // Don't throw - this is not critical
        }
    }

    /// <summary>
    /// Parse conversation history from JSON string
    /// </summary>
    private List<ConversationMessage>? ParseConversationHistory(string conversationHistory)
    {
        try
        {
            var history = JsonSerializer.Deserialize<List<ConversationMessage>>(
                conversationHistory,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (history != null)
            {
                // Normalize line breaks in parsed messages (to fix old data)
                foreach (var message in history)
                {
                    if (!string.IsNullOrWhiteSpace(message.Content))
                    {
                        message.Content = NormalizeLineBreaks(message.Content);
                    }
                }
            }

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing conversation history");
            return null;
        }
    }

    /// <summary>
    /// Extract title from session or conversation history
    /// </summary>
    private string? ExtractSessionTitle(Models.Entities.ConversationSessionEntity session, List<ConversationMessage> history)
    {
        // Use saved title from DB first
        if (!string.IsNullOrWhiteSpace(session.Title) && session.Title != "Cuộc trò chuyện mới")
        {
            return session.Title;
        }

        // Get first user/patient/guest message as title
        var firstUserMessage = history.FirstOrDefault(m =>
            m.Role?.ToLower() == "user" ||
            m.Role?.ToLower() == "patient" ||
            m.Role?.ToLower() == "guest");

        if (firstUserMessage != null && !string.IsNullOrWhiteSpace(firstUserMessage.Content))
        {
            return firstUserMessage.Content.Length > 50
                ? firstUserMessage.Content.Substring(0, 50) + "..."
                : firstUserMessage.Content;
        }

        return null;
    }

    /// <summary>
    /// Extract last message from conversation history
    /// </summary>
    private string? ExtractLastMessage(List<ConversationMessage> history)
    {
        var lastMsg = history.LastOrDefault();
        if (lastMsg != null && !string.IsNullOrWhiteSpace(lastMsg.Content))
        {
            return lastMsg.Content.Length > 100
                ? lastMsg.Content.Substring(0, 100) + "..."
                : lastMsg.Content;
        }
        return null;
    }

    /// <summary>
    /// Normalize DateTime to UTC
    /// </summary>
    private DateTime NormalizeToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        return dateTime.Kind == DateTimeKind.Local
            ? dateTime.ToUniversalTime()
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
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

    /// <summary>
    /// Create session summary from entity and history
    /// </summary>
    private Models.Entities.SessionSummaryEntity CreateSessionSummary(
        Models.Entities.ConversationSessionEntity session,
        string? title,
        string? lastMessage,
        int messageCount)
    {
        return new Models.Entities.SessionSummaryEntity
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = title ?? "Cuộc trò chuyện mới",
            LastMessage = lastMessage,
            CreatedAt = NormalizeToUtc(session.CreatedAt),
            UpdatedAt = NormalizeToUtc(session.UpdatedAt),
            MessageCount = messageCount
        };
    }

    /// <summary>
    /// Process session entity to create summary
    /// </summary>
    private Models.Entities.SessionSummaryEntity ProcessSessionForSummary(Models.Entities.ConversationSessionEntity session)
    {
        string? title = null;
        string? lastMessage = null;
        int messageCount = 0;

        if (!string.IsNullOrWhiteSpace(session.ConversationHistory))
        {
            var history = ParseConversationHistory(session.ConversationHistory);

            if (history != null && history.Any())
            {
                messageCount = history.Count;
                title = ExtractSessionTitle(session, history);
                lastMessage = ExtractLastMessage(history);
            }
        }

        return CreateSessionSummary(session, title, lastMessage, messageCount);
    }

    public async Task<List<Models.Entities.SessionSummaryEntity>> GetUserSessionsAsync(Guid userId)
    {
        try
        {
            // Always filter by authenticated user
            var query = _context.ConversationSessions
                .Where(s => s.UserId == userId);

            var sessions = await query
                .OrderByDescending(s => s.UpdatedAt)
                .Take(50) // Limit to last 50 sessions
                .ToListAsync();

            var summaries = new List<Models.Entities.SessionSummaryEntity>();

            foreach (var session in sessions)
            {
                var summary = ProcessSessionForSummary(session);
                summaries.Add(summary);
            }

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions for user {UserId}", userId);
            throw new ConversationSessionException(
                $"Failed to get user sessions: {ex.Message}",
                null,
                userId,
                ex);
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId)
    {
        try
        {
            var session = await _context.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found or user {UserId} does not have permission to delete", sessionId, userId);
                return false;
            }

            _context.ConversationSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted conversation session: {SessionId} for user: {UserId}", sessionId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}: {Message}", sessionId, ex.Message);
            throw new ConversationSessionException(
                $"Failed to delete session: {ex.Message}",
                sessionId,
                userId,
                ex);
        }
    }

    public async Task<bool> CheckIfLabResultExistsAsync(Guid sessionId)
    {
        try
        {
            var history = await LoadConversationHistoryAsync(sessionId);

            // Check if any patient message contains "Đã gửi file xét nghiệm:"
            return history.Any(m =>
                m.Role == "patient" &&
                m.Content != null &&
                m.Content.Contains("Đã gửi file xét nghiệm:"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if lab result exists for session {SessionId}", sessionId);
            return false; // Default to allowing upload if check fails
        }
    }
}
