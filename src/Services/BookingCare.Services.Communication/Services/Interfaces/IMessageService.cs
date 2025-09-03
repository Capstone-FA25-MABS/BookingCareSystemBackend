using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho Message service
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// T?o tin nh?n m?i
    /// </summary>
    Task<MessageResponse> CreateAsync(CreateMessageRequest request);

    /// <summary>
    /// T?o tin nh?n v?i file upload (Complete Flow)
    /// </summary>
    Task<MessageResponse> CreateMessageWithFilesAsync(CreateMessageWithFilesRequest request);

    /// <summary>
    /// C?p nh?t tin nh?n
    /// </summary>
    Task<MessageResponse> UpdateAsync(UpdateMessageRequest request);

    /// <summary>
    /// L?y tin nh?n theo ID
    /// </summary>
    Task<MessageResponse?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách tin nh?n theo conversation ID
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Xóa tin nh?n
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// ?ánh d?u tin nh?n ?ã ??c
    /// </summary>
    Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request);

    /// <summary>
    /// L?y s? tin nh?n ch?a ??c
    /// </summary>
    Task<long> GetUnreadCountAsync(string conversationId, string userId);

    /// <summary>
    /// Tìm ki?m tin nh?n
    /// </summary>
    Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request);

    /// <summary>
    /// L?y tin nh?n theo lo?i (Text, Image, File, etc.)
    /// </summary>
    Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(string conversationId, MessageType messageType, int page = 1, int pageSize = 20);

    /// <summary>
    /// L?y t?t c? file attachments trong conversation
    /// </summary>
    Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(string conversationId, MessageType? messageType = null, int page = 1, int pageSize = 50);
}