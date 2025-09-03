using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho Conversation repository
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// L?y cu?c h?i tho?i theo ID
    /// </summary>
    Task<ConversationEntity?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách cu?c h?i tho?i c?a user
    /// </summary>
    Task<IEnumerable<ConversationEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Tìm cu?c h?i tho?i gi?a 2 ng??i dùng
    /// </summary>
    Task<ConversationEntity?> GetConversationBetweenUsersAsync(string userId1, string userId2);

    /// <summary>
    /// T?o cu?c h?i tho?i m?i
    /// </summary>
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation);

    /// <summary>
    /// C?p nh?t cu?c h?i tho?i
    /// </summary>
    Task<ConversationEntity> UpdateAsync(ConversationEntity conversation);

    /// <summary>
    /// Xóa cu?c h?i tho?i
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// C?p nh?t tin nh?n cu?i cùng
    /// </summary>
    Task<bool> UpdateLastMessageAsync(string conversationId, LastMessage lastMessage);

    /// <summary>
    /// Ch?n cu?c h?i tho?i
    /// </summary>
    Task<bool> BlockConversationAsync(string conversationId, string blockedBy);

    /// <summary>
    /// B? ch?n cu?c h?i tho?i
    /// </summary>
    Task<bool> UnblockConversationAsync(string conversationId);

    /// <summary>
    /// Ki?m tra cu?c h?i tho?i có b? ch?n không
    /// </summary>
    Task<bool> IsConversationBlockedAsync(string conversationId);
}