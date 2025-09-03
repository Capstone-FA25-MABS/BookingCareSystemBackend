using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho Conversation service
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// T?o cu?c h?i tho?i m?i
    /// </summary>
    Task<ConversationResponse> CreateAsync(CreateConversationRequest request);

    /// <summary>
    /// L?y cu?c h?i tho?i theo ID
    /// </summary>
    Task<ConversationResponse?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách cu?c h?i tho?i c?a user
    /// </summary>
    Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Tìm cu?c h?i tho?i gi?a 2 ng??i dùng
    /// </summary>
    Task<ConversationResponse?> GetConversationBetweenUsersAsync(string userId1, string userId2);

    /// <summary>
    /// Xóa cu?c h?i tho?i
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// C?p nh?t tin nh?n cu?i cùng
    /// </summary>
    Task<bool> UpdateLastMessageAsync(string conversationId, string messageId, string content, string senderId);

    /// <summary>
    /// Ch?n cu?c h?i tho?i
    /// </summary>
    Task<bool> BlockConversationAsync(BlockConversationRequest request);

    /// <summary>
    /// B? ch?n cu?c h?i tho?i
    /// </summary>
    Task<bool> UnblockConversationAsync(UnblockConversationRequest request);

    /// <summary>
    /// Ki?m tra cu?c h?i tho?i có b? ch?n không
    /// </summary>
    Task<bool> IsConversationBlockedAsync(string conversationId);
}