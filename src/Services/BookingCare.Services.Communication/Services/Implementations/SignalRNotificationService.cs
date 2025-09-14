using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Triển khai SignalR notification service
/// </summary>
public class SignalRNotificationService : BaseService, ISignalRNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRNotificationService(
        IHubContext<ChatHub> hubContext,
        ILogger<SignalRNotificationService> logger) : base(logger)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Gửi tin nhắn đến conversation group
    /// </summary>
    public async Task SendMessageToConversationAsync(string conversationId, MessageResponse message)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi tin nhắn SignalR đến conversation: {ConversationId}", null, conversationId);

            var groupName = GetConversationGroupName(conversationId);
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                MessageId = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Type = message.Type,
                Attachments = message.Attachments,
                CreatedAt = message.CreatedAt,
                Status = message.Status
            });

            LogInfo("Tin nhắn SignalR đã được gửi thành công đến conversation: {ConversationId}", null, conversationId);
        }, "SendMessageToConversation");
    }

    /// <summary>
    /// Gửi thông báo tin nhắn đã đọc
    /// </summary>
    public async Task SendMessageReadNotificationAsync(string conversationId, string messageId, string userId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo tin nhắn đã đọc: {MessageId} bởi user: {UserId}", null, messageId, userId);

            var groupName = GetConversationGroupName(conversationId);
            await _hubContext.Clients.Group(groupName).SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReadBy = userId,
                ReadAt = DateTime.UtcNow
            });

            LogInfo("Thông báo tin nhắn đã đọc đã được gửi thành công: {MessageId}", null, messageId);
        }, "SendMessageReadNotification");
    }

    /// <summary>
    /// Gửi thông báo tất cả tin nhắn đã đọc
    /// </summary>
    public async Task SendAllMessagesReadNotificationAsync(string conversationId, string userId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo tất cả tin nhắn đã đọc trong conversation: {ConversationId} bởi user: {UserId}",
                null, conversationId, userId);

            var groupName = GetConversationGroupName(conversationId);
            await _hubContext.Clients.Group(groupName).SendAsync("AllMessagesRead", new
            {
                ConversationId = conversationId,
                ReadBy = userId,
                ReadAt = DateTime.UtcNow
            });

            LogInfo("Thông báo tất cả tin nhắn đã đọc đã được gửi thành công cho conversation: {ConversationId}",
                null, conversationId);
        }, "SendAllMessagesReadNotification");
    }

    /// <summary>
    /// Gửi thông báo user online
    /// </summary>
    public async Task SendUserOnlineNotificationAsync(string userId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo user online: {UserId}", null, userId);

            await _hubContext.Clients.All.SendAsync("UserOnline", userId);

            LogInfo("Thông báo user online đã được gửi thành công: {UserId}", null, userId);
        }, "SendUserOnlineNotification");
    }

    /// <summary>
    /// Gửi thông báo user offline
    /// </summary>
    public async Task SendUserOfflineNotificationAsync(string userId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo user offline: {UserId}", null, userId);

            await _hubContext.Clients.All.SendAsync("UserOffline", userId);

            LogInfo("Thông báo user offline đã được gửi thành công: {UserId}", null, userId);
        }, "SendUserOfflineNotification");
    }

    /// <summary>
    /// Gửi thông báo conversation mới được tạo
    /// </summary>
    public async Task SendConversationCreatedNotificationAsync(ConversationResponse conversation)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo conversation mới được tạo: {ConversationId}", null, conversation.Id);

            // Gửi đến tất cả participants
            foreach (var participantId in conversation.Participants)
            {
                var userConnections = ChatHub.GetUserConnections(participantId);
                if (userConnections.Any())
                {
                    await _hubContext.Clients.Clients(userConnections).SendAsync("ConversationCreated", conversation);
                }
            }

            LogInfo("Thông báo conversation mới đã được gửi thành công: {ConversationId}", null, conversation.Id);
        }, "SendConversationCreatedNotification");
    }

    /// <summary>
    /// Gửi thông báo user đang gõ
    /// </summary>
    public async Task SendTypingNotificationAsync(string conversationId, string userId, bool isTyping)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Đang gửi thông báo typing cho user: {UserId} trong conversation: {ConversationId}, isTyping: {IsTyping}",
                null, userId, conversationId, isTyping);

            var groupName = GetConversationGroupName(conversationId);
            var eventName = isTyping ? "UserStartedTyping" : "UserStoppedTyping";

            await _hubContext.Clients.Group(groupName).SendAsync(eventName, new
            {
                UserId = userId,
                ConversationId = conversationId
            });

            LogInfo("Thông báo typing đã được gửi thành công cho user: {UserId}", null, userId);
        }, "SendTypingNotification");
    }

    #region Private Methods

    /// <summary>
    /// Tạo tên group cho conversation
    /// </summary>
    private static string GetConversationGroupName(string conversationId)
    {
        return $"conversation_{conversationId}";
    }

    #endregion
}