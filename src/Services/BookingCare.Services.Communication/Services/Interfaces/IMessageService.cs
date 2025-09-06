using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho Message service
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    Task<MessageResponse> CreateAsync(CreateMessageRequest request);

    /// <summary>
    /// Tạo tin nhắn với file upload (Complete Flow)
    /// </summary>
    Task<MessageResponse> CreateMessageWithFilesAsync(CreateMessageWithFilesRequest request);

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    Task<MessageResponse> UpdateAsync(UpdateMessageRequest request);

    /// <summary>
    /// Lấy tin nhắn theo ID
    /// </summary>
    Task<MessageResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách tin nhắn theo conversation ID
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Lấy danh sách tin nhắn theo conversation ID với cursor-based pagination
    /// </summary>
    Task<CursorPaginatedResponse<MessageResponse>> GetByConversationIdWithCursorAsync(string conversationId, string? before = null, string? after = null, int limit = 50);

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request);

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc trong conversation là đã đọc
    /// </summary>
    Task<bool> MarkAllAsReadAsync(MarkAllMessagesAsReadRequest request);

    /// <summary>
    /// Lấy số tin nhắn chưa đọc
    /// </summary>
    Task<long> GetUnreadCountAsync(string conversationId, string userId);

    /// <summary>
    /// Tìm kiếm tin nhắn
    /// </summary>
    Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request);

    /// <summary>
    /// Lấy tin nhắn theo loại (Text, Image, File, etc.)
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(string conversationId, MessageType messageType, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy tất cả file attachments trong conversation
    /// </summary>
    Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(string conversationId, MessageType? messageType = null, int page = 1, int pageSize = 50);
}