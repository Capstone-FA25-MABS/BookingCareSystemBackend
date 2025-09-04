using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho Message repository
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// L?y tin nh?n theo ID
    /// </summary>
    Task<MessageEntity?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách tin nh?n theo conversation ID
    /// </summary>
    Task<IEnumerable<MessageEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50);

    /// <summary>
    /// T?o tin nh?n m?i
    /// </summary>
    Task<MessageEntity> CreateAsync(MessageEntity message);

    /// <summary>
    /// C?p nh?t tin nh?n
    /// </summary>
    Task<MessageEntity> UpdateAsync(MessageEntity message);

    /// <summary>
    /// Xóa tin nh?n
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// ?ánh d?u tin nh?n ?ã ??c
    /// </summary>
    Task<bool> MarkAsReadAsync(string messageId, DateTime readAt);

    /// <summary>
    /// ?ánh d?u t?t c? tin nh?n ch?a ??c c?a user trong conversation là ?ã ??c
    /// </summary>
    Task<bool> MarkAllAsReadAsync(string conversationId, string userId, DateTime readAt);

    /// <summary>
    /// L?y s? tin nh?n ch?a ??c theo conversation
    /// </summary>
    Task<long> GetUnreadCountAsync(string conversationId, string userId);

    /// <summary>
    /// Tìm ki?m tin nh?n theo n?i dung
    /// </summary>
    Task<IEnumerable<MessageEntity>> SearchAsync(string conversationId, string searchTerm, int page = 1, int pageSize = 20);
}