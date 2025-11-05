using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface for Message service
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Create a new message
    /// </summary>
    Task<MessageResponse> CreateAsync(CreateMessageRequest request);

    /// <summary>
    /// Create a message with file upload (complete flow)
    /// </summary>
    Task<MessageResponse> CreateMessageWithFilesAsync(CreateMessageWithFilesRequest request);

    /// <summary>
    /// Update a message
    /// </summary>
    Task<MessageResponse> UpdateAsync(UpdateMessageRequest request);

    /// <summary>
    /// Get a message by ID
    /// </summary>
    Task<MessageResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Get messages by conversation ID
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(
        string conversationId,
        int page = 1,
        int pageSize = 50
    );

    /// <summary>
    /// 🎯 NEW: Get messages by conversation ID with user info enrichment
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetByConversationIdWithUserInfoAsync(
        string conversationId,
        int page = 1,
        int pageSize = 50,
        MessageLoadOptions? options = null
    );

    /// <summary>
    /// Delete a message
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Mark a message as read
    /// </summary>
    Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request);

    /// <summary>
    /// Mark all unread messages in a conversation as read
    /// </summary>
    Task<bool> MarkAllAsReadAsync(MarkAllMessagesAsReadRequest request);

    /// <summary>
    /// Get unread message count for a specific conversation
    /// </summary>
    Task<long> GetUnreadCountAsync(string conversationId, string userId);

    /// <summary>
    /// Get total unread message count across all conversations for a user (for badge notification)
    /// </summary>
    Task<long> GetTotalUnreadCountAsync(string userId);

    /// <summary>
    /// Get unread count per conversation for a user (for conversation list badges)
    /// </summary>
    Task<Dictionary<string, long>> GetUnreadCountByConversationsAsync(string userId);

    /// <summary>
    /// Search messages
    /// </summary>
    Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request);

    /// <summary>
    /// Get messages by type (Text, Image, File, etc.)
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(
        string conversationId,
        MessageType messageType,
        int page = 1,
        int pageSize = 20
    );

    /// <summary>
    /// Get all file attachments in a conversation
    /// </summary>
    Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(
        string conversationId,
        MessageType? messageType = null,
        int page = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Get mixed timeline (messages + call logs) for a conversation
    /// </summary>
    Task<MixedTimelineResponse> GetMixedTimelineAsync(GetMixedTimelineRequest request);

    /// <summary>
    /// 🎯 NEW: Get mixed timeline with user info enrichment
    /// </summary>
    Task<MixedTimelineResponse> GetMixedTimelineWithUserInfoAsync(
        GetMixedTimelineRequest request,
        MessageLoadOptions? options = null
    );
}
