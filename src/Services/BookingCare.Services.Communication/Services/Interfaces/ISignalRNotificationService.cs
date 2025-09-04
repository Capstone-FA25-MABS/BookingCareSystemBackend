using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho SignalR notification service
/// </summary>
public interface ISignalRNotificationService
{
    /// <summary>
    /// Gửi tin nhắn đến conversation group
    /// </summary>
    Task SendMessageToConversationAsync(string conversationId, MessageResponse message);

    /// <summary>
    /// Gửi thông báo tin nhắn đã đọc
    /// </summary>
    Task SendMessageReadNotificationAsync(string conversationId, string messageId, string userId);

    /// <summary>
    /// Gửi thông báo tất cả tin nhắn đã đọc
    /// </summary>
    Task SendAllMessagesReadNotificationAsync(string conversationId, string userId);

    /// <summary>
    /// Gửi thông báo user online
    /// </summary>
    Task SendUserOnlineNotificationAsync(string userId);

    /// <summary>
    /// Gửi thông báo user offline
    /// </summary>
    Task SendUserOfflineNotificationAsync(string userId);

    /// <summary>
    /// Gửi thông báo conversation mới được tạo
    /// </summary>
    Task SendConversationCreatedNotificationAsync(ConversationResponse conversation);

    /// <summary>
    /// Gửi thông báo user đang gõ
    /// </summary>
    Task SendTypingNotificationAsync(string conversationId, string userId, bool isTyping);
}