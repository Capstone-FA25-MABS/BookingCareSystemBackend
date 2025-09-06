using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho Conversation service
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    Task<ConversationResponse> CreateAsync(CreateConversationRequest request);

    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    Task<ConversationResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với lazy loading options
    /// </summary>
    Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20, ConversationLoadOptions? options = null);

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với cursor-based pagination
    /// </summary>
    Task<CursorPaginatedResponse<ConversationResponse>> GetByUserIdWithCursorAsync(string userId, string? before = null, string? after = null, int limit = 20, ConversationLoadOptions? options = null);

    /// <summary>
    /// Lấy danh sách cuộc hội thoại lightweight (chỉ thông tin cơ bản)
    /// </summary>
    Task<IEnumerable<ConversationListResponse>> GetConversationsLightweightAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy chi tiết conversation với đầy đủ thông tin lazy loading
    /// </summary>
    Task<ConversationResponse?> GetConversationDetailsAsync(string id, ConversationLoadOptions? options = null);

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 người dùng
    /// </summary>
    Task<ConversationResponse?> GetConversationBetweenUsersAsync(string userId1, string userId2);

    /// <summary>
    /// Xóa cuộc hội thoại
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Cập nhật tin nhắn cuối cùng
    /// </summary>
    Task<bool> UpdateLastMessageAsync(string conversationId, string messageId, string content, string senderId);

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    Task<bool> BlockConversationAsync(BlockConversationRequest request);

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    Task<bool> UnblockConversationAsync(UnblockConversationRequest request);

    /// <summary>
    /// Kiểm tra cuộc hội thoại có bị chặn không
    /// </summary>
    Task<bool> IsConversationBlockedAsync(string conversationId);
}