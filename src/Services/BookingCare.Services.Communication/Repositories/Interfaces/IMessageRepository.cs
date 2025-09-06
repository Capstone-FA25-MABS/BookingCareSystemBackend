using BookingCare.Services.Communication.Models.Entities;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho Message repository
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Lấy tin nhắn theo ID
    /// </summary>
    Task<MessageEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách tin nhắn theo conversation ID
    /// </summary>
    Task<IEnumerable<MessageEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    Task<MessageEntity> CreateAsync(MessageEntity message);

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    Task<MessageEntity> UpdateAsync(MessageEntity message);

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    Task<bool> MarkAsReadAsync(string messageId, DateTime readAt);

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc của user trong conversation là đã đọc
    /// </summary>
    Task<bool> MarkAllAsReadAsync(string conversationId, string userId, DateTime readAt);

    /// <summary>
    /// Lấy số tin nhắn chưa đọc theo conversation
    /// </summary>
    Task<long> GetUnreadCountAsync(string conversationId, string userId);

    /// <summary>
    /// Tìm kiếm tin nhắn theo nội dung
    /// </summary>
    Task<IEnumerable<MessageEntity>> SearchAsync(string conversationId, string searchTerm, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy tin nhắn với cursor-based pagination
    /// </summary>
    Task<IEnumerable<MessageEntity>> GetByConversationIdWithCursorAsync(string conversationId, string? before = null, string? after = null, int limit = 50);
}