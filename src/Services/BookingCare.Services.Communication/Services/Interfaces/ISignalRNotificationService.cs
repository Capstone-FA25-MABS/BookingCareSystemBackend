using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface for the SignalR notification service
/// </summary>
public interface ISignalRNotificationService
{
    /// <summary>
    /// Send a message to a conversation group
    /// </summary>
    Task SendMessageToConversationAsync(string conversationId, MessageResponse message);

    /// <summary>
    /// Send a read notification for a message
    /// </summary>
    Task SendMessageReadNotificationAsync(string conversationId, string messageId, string userId);

    /// <summary>
    /// Send a notification that all messages have been read
    /// </summary>
    Task SendAllMessagesReadNotificationAsync(string conversationId, string userId);

    /// <summary>
    /// Send a user online notification
    /// </summary>
    Task SendUserOnlineNotificationAsync(string userId);

    /// <summary>
    /// Send a user offline notification
    /// </summary>
    Task SendUserOfflineNotificationAsync(string userId);

    /// <summary>
    /// Send a notification when a conversation is created
    /// </summary>
    Task SendConversationCreatedNotificationAsync(ConversationResponse conversation);

    /// <summary>
    /// Send a typing notification
    /// </summary>
    Task SendTypingNotificationAsync(string conversationId, string userId, bool isTyping);
}