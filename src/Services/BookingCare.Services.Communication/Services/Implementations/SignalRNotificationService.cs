using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation of the SignalR notification service
/// </summary>
public class SignalRNotificationService : BaseService, ISignalRNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRNotificationService(
        IHubContext<ChatHub> hubContext,
        ILogger<SignalRNotificationService> logger
    )
        : base(logger)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Send a message to the conversation group
    /// </summary>
    public async Task SendMessageToConversationAsync(string conversationId, MessageResponse message)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending SignalR message to conversation: {ConversationId}",
                    null,
                    conversationId
                );

                var groupName = GetConversationGroupName(conversationId);
                await _hubContext
                    .Clients.Group(groupName)
                    .SendAsync(
                        "ReceiveMessage",
                        new
                        {
                            MessageId = message.Id,
                            ConversationId = message.ConversationId,
                            SenderId = message.SenderId,
                            ReceiverId = message.ReceiverId,
                            Content = message.Content,
                            Type = message.Type,
                            Attachments = message.Attachments,
                            CreatedAt = message.CreatedAt,
                            Status = message.Status,
                        }
                    );

                LogInfo(
                    "SignalR message successfully sent to conversation: {ConversationId}",
                    null,
                    conversationId
                );
            },
            "SendMessageToConversation"
        );
    }

    /// <summary>
    /// Send a message read notification
    /// </summary>
    public async Task SendMessageReadNotificationAsync(
        string conversationId,
        string messageId,
        string userId
    )
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending message read notification: {MessageId} by user: {UserId}",
                    null,
                    messageId,
                    userId
                );

                var groupName = GetConversationGroupName(conversationId);
                await _hubContext
                    .Clients.Group(groupName)
                    .SendAsync(
                        "MessageRead",
                        new
                        {
                            MessageId = messageId,
                            ReadBy = userId,
                            ReadAt = DateTime.UtcNow,
                        }
                    );

                LogInfo(
                    "Message read notification successfully sent: {MessageId}",
                    null,
                    messageId
                );
            },
            "SendMessageReadNotification"
        );
    }

    /// <summary>
    /// Send notification that all messages have been read
    /// </summary>
    public async Task SendAllMessagesReadNotificationAsync(string conversationId, string userId)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending all-messages-read notification for conversation: {ConversationId} by user: {UserId}",
                    null,
                    conversationId,
                    userId
                );

                var groupName = GetConversationGroupName(conversationId);
                await _hubContext
                    .Clients.Group(groupName)
                    .SendAsync(
                        "AllMessagesRead",
                        new
                        {
                            ConversationId = conversationId,
                            ReadBy = userId,
                            ReadAt = DateTime.UtcNow,
                        }
                    );

                LogInfo(
                    "All-messages-read notification successfully sent for conversation: {ConversationId}",
                    null,
                    conversationId
                );
            },
            "SendAllMessagesReadNotification"
        );
    }

    /// <summary>
    /// Send user online notification
    /// </summary>
    public async Task SendUserOnlineNotificationAsync(string userId)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Sending user online notification: {UserId}", null, userId);

                await _hubContext.Clients.All.SendAsync("UserOnline", userId);

                LogInfo("User online notification successfully sent: {UserId}", null, userId);
            },
            "SendUserOnlineNotification"
        );
    }

    /// <summary>
    /// Send user offline notification
    /// </summary>
    public async Task SendUserOfflineNotificationAsync(string userId)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Sending user offline notification: {UserId}", null, userId);

                await _hubContext.Clients.All.SendAsync("UserOffline", userId);

                LogInfo("User offline notification successfully sent: {UserId}", null, userId);
            },
            "SendUserOfflineNotification"
        );
    }

    /// <summary>
    /// Send notification when a conversation is created
    /// </summary>
    public async Task SendConversationCreatedNotificationAsync(ConversationResponse conversation)
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending conversation created notification: {ConversationId}",
                    null,
                    conversation.Id
                );

                // Send to all participants
                foreach (var participantId in conversation.Participants)
                {
                    var userConnections = ChatHub.GetUserConnections(participantId);
                    if (userConnections.Any())
                    {
                        await _hubContext
                            .Clients.Clients(userConnections)
                            .SendAsync("ConversationCreated", conversation);
                    }
                }

                LogInfo(
                    "Conversation created notification successfully sent: {ConversationId}",
                    null,
                    conversation.Id
                );
            },
            "SendConversationCreatedNotification"
        );
    }

    /// <summary>
    /// Send typing notification
    /// </summary>
    public async Task SendTypingNotificationAsync(
        string conversationId,
        string userId,
        bool isTyping
    )
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending typing notification for user: {UserId} in conversation: {ConversationId}, isTyping: {IsTyping}",
                    null,
                    userId,
                    conversationId,
                    isTyping
                );

                var groupName = GetConversationGroupName(conversationId);
                var eventName = isTyping ? "UserStartedTyping" : "UserStoppedTyping";

                await _hubContext
                    .Clients.Group(groupName)
                    .SendAsync(eventName, new { UserId = userId, ConversationId = conversationId });

                LogInfo("Typing notification successfully sent for user: {UserId}", null, userId);
            },
            "SendTypingNotification"
        );
    }

    /// <summary>
    /// Send notification when a message is recalled
    /// </summary>
    public async Task SendMessageRecalledNotificationAsync(
        string conversationId,
        MessageResponse message
    )
    {
        await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Sending message recalled notification: {MessageId} in conversation: {ConversationId}",
                    null,
                    message.Id,
                    conversationId
                );

                var groupName = GetConversationGroupName(conversationId);
                await _hubContext
                    .Clients.Group(groupName)
                    .SendAsync(
                        "MessageRecalled",
                        new
                        {
                            MessageId = message.Id,
                            ConversationId = message.ConversationId,
                            SenderId = message.SenderId,
                            Content = message.Content,
                            Status = message.Status,
                            UpdatedAt = message.UpdatedAt,
                        }
                    );

                LogInfo(
                    "Message recalled notification successfully sent: {MessageId}",
                    null,
                    message.Id
                );
            },
            "SendMessageRecalledNotification"
        );
    }

    #region Private Methods

    /// <summary>
    /// Create the group name for a conversation
    /// </summary>
    private static string GetConversationGroupName(string conversationId)
    {
        return $"conversation_{conversationId}";
    }

    #endregion
}
