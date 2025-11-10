using System.Text.Json;
using BookingCare.Services.AI.Data;
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

    public async Task<Guid> GetOrCreateSessionAsync(Guid? sessionId, Guid? userId, LocationContext? location)
    {
        try
        {
            // If sessionId is provided, check if it exists
            if (sessionId.HasValue)
            {
                var existingSession = await _context.ConversationSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value);

                if (existingSession != null)
                {
                    // Update location if provided and different
                    if (location != null)
                    {
                        if (existingSession.ProvinceId != location.ProvinceId ||
                            existingSession.DistrictId != location.DistrictId)
                        {
                            existingSession.ProvinceId = location.ProvinceId;
                            existingSession.DistrictId = location.DistrictId;
                            await _context.SaveChangesAsync();
                        }
                    }

                    return existingSession.Id;
                }
            }

            // Create new session
            // Note: CreatedAt and UpdatedAt will be set automatically by DbContext.UpdateTimestamps()
            var newSession = new Models.Entities.ConversationSessionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProvinceId = location?.ProvinceId,
                DistrictId = location?.DistrictId,
                ConversationHistory = "[]"
            };

            _context.ConversationSessions.Add(newSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new conversation session: {SessionId}", newSession.Id);

            return newSession.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating session: {Message}. StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);
            throw new ApplicationException($"Failed to get or create conversation session: {ex.Message}", ex);
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
        Guid? userId = null)
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

            // Add new messages
            history.Add(new ConversationMessage
            {
                Role = userRole,
                Content = userMessage,
                Timestamp = DateTime.UtcNow
            });

            var aiMessageObj = new ConversationMessage
            {
                Role = "ai",
                Content = aiMessage,
                Timestamp = DateTime.UtcNow,
                Suggestions = suggestions // Save suggestions with AI message
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

            _logger.LogInformation("Saved conversation history with {Count} messages. Last AI message has suggestions: {HasSuggestions}",
                history.Count,
                aiMessageObj.Suggestions != null);

            // Update location if provided
            if (location != null)
            {
                session.ProvinceId = location.ProvinceId;
                session.DistrictId = location.DistrictId;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved conversation history for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation history for session {SessionId}", sessionId);
            // Don't throw - this is not critical
        }
    }

    public async Task<List<Models.Entities.SessionSummaryEntity>> GetUserSessionsAsync(Guid? userId)
    {
        try
        {
            var query = _context.ConversationSessions.AsQueryable();

            // Filter by user if provided
            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            var sessions = await query
                .OrderByDescending(s => s.UpdatedAt)
                .Take(50) // Limit to last 50 sessions
                .ToListAsync();

            var summaries = new List<Models.Entities.SessionSummaryEntity>();

            foreach (var session in sessions)
            {
                // Parse conversation history to get title and last message
                string? title = null;
                string? lastMessage = null;
                int messageCount = 0;

                if (!string.IsNullOrWhiteSpace(session.ConversationHistory))
                {
                    try
                    {
                        var history = JsonSerializer.Deserialize<List<ConversationMessage>>(
                            session.ConversationHistory,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (history != null && history.Any())
                        {
                            messageCount = history.Count;

                            // Use saved title from DB first, otherwise extract from first user message
                            if (!string.IsNullOrWhiteSpace(session.Title) && session.Title != "Cuộc trò chuyện mới")
                            {
                                title = session.Title;
                            }
                            else
                            {
                                // Get first user/patient/guest message as title
                                var firstUserMessage = history.FirstOrDefault(m =>
                                    m.Role?.ToLower() == "user" ||
                                    m.Role?.ToLower() == "patient" ||
                                    m.Role?.ToLower() == "guest");
                                if (firstUserMessage != null && !string.IsNullOrWhiteSpace(firstUserMessage.Content))
                                {
                                    title = firstUserMessage.Content.Length > 50
                                        ? firstUserMessage.Content.Substring(0, 50) + "..."
                                        : firstUserMessage.Content;
                                }
                            }

                            // Get last message
                            var lastMsg = history.LastOrDefault();
                            if (lastMsg != null && !string.IsNullOrWhiteSpace(lastMsg.Content))
                            {
                                lastMessage = lastMsg.Content.Length > 100
                                    ? lastMsg.Content.Substring(0, 100) + "..."
                                    : lastMsg.Content;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing conversation history for session {SessionId}", session.Id);
                    }
                }

                // Ensure DateTime is in UTC (convert if needed)
                // Entity Framework may load DateTime as Unspecified or Local, so we need to ensure UTC
                var createdAt = session.CreatedAt;
                if (createdAt.Kind != DateTimeKind.Utc)
                {
                    createdAt = createdAt.Kind == DateTimeKind.Local
                        ? createdAt.ToUniversalTime()
                        : DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
                }

                var updatedAt = session.UpdatedAt;
                if (updatedAt.Kind != DateTimeKind.Utc)
                {
                    updatedAt = updatedAt.Kind == DateTimeKind.Local
                        ? updatedAt.ToUniversalTime()
                        : DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc);
                }

                summaries.Add(new Models.Entities.SessionSummaryEntity
                {
                    Id = session.Id,
                    UserId = session.UserId,
                    Title = title ?? "Cuộc trò chuyện mới",
                    LastMessage = lastMessage,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    MessageCount = messageCount
                });
            }

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions for user {UserId}", userId);
            throw new ApplicationException($"Failed to get user sessions: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for deletion", sessionId);
                return false;
            }

            _context.ConversationSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted conversation session: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}: {Message}", sessionId, ex.Message);
            throw new ApplicationException($"Failed to delete session: {ex.Message}", ex);
        }
    }
}
