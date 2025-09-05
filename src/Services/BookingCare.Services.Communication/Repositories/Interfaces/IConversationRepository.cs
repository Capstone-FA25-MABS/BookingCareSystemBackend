using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho Conversation repository
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    Task<ConversationEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user
    /// </summary>
    Task<IEnumerable<ConversationEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 người dùng
    /// </summary>
    Task<ConversationEntity?> GetConversationBetweenUsersAsync(string userId1, string userId2);

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation);

    /// <summary>
    /// Cập nhật cuộc hội thoại
    /// </summary>
    Task<ConversationEntity> UpdateAsync(ConversationEntity conversation);

    /// <summary>
    /// Xóa cuộc hội thoại
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Cập nhật tin nhắn cuối cùng
    /// </summary>
    Task<bool> UpdateLastMessageAsync(string conversationId, LastMessage lastMessage);

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    Task<bool> BlockConversationAsync(string conversationId, string blockedBy);

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    Task<bool> UnblockConversationAsync(string conversationId);

    /// <summary>
    /// Kiểm tra cuộc hội thoại có bị chặn không
    /// </summary>
    Task<bool> IsConversationBlockedAsync(string conversationId);
}