using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface for Conversation service
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Create a new conversation
    /// </summary>
    Task<ConversationResponse> CreateAsync(CreateConversationRequest request);

    /// <summary>
    /// Get a conversation by ID
    /// </summary>
    Task<ConversationResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Get conversations for a user with lazy loading options
    /// </summary>
    Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        ConversationLoadOptions? options = null
    );

    /// <summary>
    /// Get conversations for a user with cursor-based pagination
    /// </summary>
    Task<CursorPaginatedResponse<ConversationResponse>> GetByUserIdWithCursorAsync(
        string userId,
        string? before = null,
        string? after = null,
        int limit = 20,
        ConversationLoadOptions? options = null
    );

    /// <summary>
    /// Get lightweight list of conversations (basic info only)
    /// </summary>
    Task<IEnumerable<ConversationListResponse>> GetConversationsLightweightAsync(
        string userId,
        int page = 1,
        int pageSize = 20
    );

    /// <summary>
    /// Get conversation details with full info and lazy loading
    /// </summary>
    Task<ConversationResponse?> GetConversationDetailsAsync(
        string id,
        ConversationLoadOptions? options = null
    );

    /// <summary>
    /// Find a conversation between two users
    /// </summary>
    Task<ConversationResponse?> GetConversationBetweenUsersAsync(string userId1, string userId2);

    /// <summary>
    /// Delete a conversation
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Update the last message of a conversation
    /// </summary>
    Task<bool> UpdateLastMessageAsync(
        string conversationId,
        string messageId,
        string content,
        string senderId
    );

    /// <summary>
    /// Block a conversation
    /// </summary>
    Task<bool> BlockConversationAsync(BlockConversationRequest request);

    /// <summary>
    /// Unblock a conversation
    /// </summary>
    Task<bool> UnblockConversationAsync(UnblockConversationRequest request);

    /// <summary>
    /// Check if a conversation is blocked
    /// </summary>
    Task<bool> IsConversationBlockedAsync(string conversationId);

    /// <summary>
    /// Get conversations by tag IDs
    /// </summary>
    Task<PaginatedResponse<ConversationResponse>> GetConversationsByTagsAsync(
        string userId,
        List<string> tagIds,
        string filterMode = "any",
        int pageNumber = 1,
        int pageSize = 20
    );

    /// <summary>
    /// Get grouped conversations by tags
    /// </summary>
    Task<Dictionary<string, List<ConversationResponse>>> GetGroupedConversationsByTagsAsync(
        string userId
    );
}
