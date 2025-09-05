using Microsoft.AspNetCore.SignalR;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace BookingCare.Services.Communication.Hubs;

/// <summary>
/// SignalR Hub cho real-time chat communication
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatHub> _logger;
    
    // Store user connections
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

    public ChatHub(
        IMessageService messageService,
        IConversationService conversationService,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// User kết nối vào hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            // Add connection to user mapping
            UserConnections.AddOrUpdate(userId, 
                new HashSet<string> { Context.ConnectionId },
                (key, existingConnections) =>
                {
                    existingConnections.Add(Context.ConnectionId);
                    return existingConnections;
                });

            ConnectionUsers[Context.ConnectionId] = userId;

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);

            // Notify other users that this user is online
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// User ngắt kết nối khỏi hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = ConnectionUsers.GetValueOrDefault(Context.ConnectionId);
        if (!string.IsNullOrEmpty(userId))
        {
            // Remove connection
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (!connections.Any())
                {
                    UserConnections.TryRemove(userId, out _);
                    // Notify others that user is offline
                    await Clients.Others.SendAsync("UserOffline", userId);
                }
            }

            ConnectionUsers.TryRemove(Context.ConnectionId, out _);

            _logger.LogInformation("User {UserId} disconnected with connection {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Tham gia group conversation
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        try
        {
            // Verify user is participant in conversation
            var conversation = await _conversationService.GetByIdAsync(conversationId);
            if (conversation == null || !conversation.Participants.Contains(userId))
            {
                await Clients.Caller.SendAsync("Error", "Không có quyền truy cập conversation này");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
            await Clients.Caller.SendAsync("JoinedConversation", conversationId);

            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId} for user {UserId}", conversationId, userId);
            await Clients.Caller.SendAsync("Error", "Lỗi khi tham gia conversation");
        }
    }

    /// <summary>
    /// Rời group conversation
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        await Clients.Caller.SendAsync("LeftConversation", conversationId);

        var userId = GetUserId();
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId, conversationId);
    }

    /// <summary>
    /// Gửi tin nhắn real-time
    /// </summary>
    public async Task SendMessage(SendMessageHub request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        try
        {
            // Create message through service
            var messageRequest = new CreateMessageRequest
            {
                ConversationId = request.ConversationId,
                SenderId = userId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                Type = MessageType.Text
            };

            var message = await _messageService.CreateAsync(messageRequest);

            // Send to conversation group
            var groupName = GetConversationGroupName(request.ConversationId);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                MessageId = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Type = message.Type,
                CreatedAt = message.CreatedAt,
                Status = message.Status
            });

            _logger.LogInformation("Message sent via SignalR: {MessageId} in conversation {ConversationId}", 
                message.Id, request.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for user {UserId} in conversation {ConversationId}", 
                userId, request.ConversationId);
            await Clients.Caller.SendAsync("Error", "Lỗi khi gửi tin nhắn");
        }
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task MarkMessageAsRead(string messageId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        try
        {
            var request = new MarkMessageAsReadRequest { MessageId = messageId };
            var result = await _messageService.MarkAsReadAsync(request);

            if (result)
            {
                // Get message to find conversation
                var message = await _messageService.GetByIdAsync(messageId);
                if (message != null)
                {
                    var groupName = GetConversationGroupName(message.ConversationId);
                    await Clients.Group(groupName).SendAsync("MessageRead", new
                    {
                        MessageId = messageId,
                        ReadBy = userId,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId} by user {UserId}", messageId, userId);
            await Clients.Caller.SendAsync("Error", "Lỗi khi đánh dấu tin nhắn đã đọc");
        }
    }

    /// <summary>
    /// Đánh dấu tất cả tin nhắn trong conversation đã đọc
    /// </summary>
    public async Task MarkAllMessagesAsRead(string conversationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        try
        {
            var request = new MarkAllMessagesAsReadRequest 
            { 
                ConversationId = conversationId,
                UserId = userId
            };
            
            var result = await _messageService.MarkAllAsReadAsync(request);

            if (result)
            {
                var groupName = GetConversationGroupName(conversationId);
                await Clients.Group(groupName).SendAsync("AllMessagesRead", new
                {
                    ConversationId = conversationId,
                    ReadBy = userId,
                    ReadAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all messages as read in conversation {ConversationId} by user {UserId}", 
                conversationId, userId);
            await Clients.Caller.SendAsync("Error", "Lỗi khi đánh dấu tất cả tin nhắn đã đọc");
        }
    }

    /// <summary>
    /// User đang gõ tin nhắn
    /// </summary>
    public async Task StartTyping(string conversationId)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            var groupName = GetConversationGroupName(conversationId);
            await Clients.OthersInGroup(groupName).SendAsync("UserStartedTyping", new
            {
                UserId = userId,
                ConversationId = conversationId
            });
        }
    }

    /// <summary>
    /// User dừng gõ tin nhắn
    /// </summary>
    public async Task StopTyping(string conversationId)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            var groupName = GetConversationGroupName(conversationId);
            await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
            {
                UserId = userId,
                ConversationId = conversationId
            });
        }
    }

    /// <summary>
    /// Lấy danh sách users online
    /// </summary>
    public async Task GetOnlineUsers()
    {
        var onlineUsers = UserConnections.Keys.ToList();
        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
    }

    #region Private Methods

    /// <summary>
    /// Lấy User ID từ context
    /// </summary>
    private string GetUserId()
    {
        return Context.User?.FindFirst("sub")?.Value 
            ?? Context.User?.FindFirst("userId")?.Value
            ?? Context.User?.FindFirst("id")?.Value
            ?? string.Empty;
    }

    /// <summary>
    /// Tạo tên group cho conversation
    /// </summary>
    private static string GetConversationGroupName(string conversationId)
    {
        return $"conversation_{conversationId}";
    }

    /// <summary>
    /// Kiểm tra user có online không
    /// </summary>
    public static bool IsUserOnline(string userId)
    {
        return UserConnections.ContainsKey(userId);
    }

    /// <summary>
    /// Lấy connection IDs của user
    /// </summary>
    public static HashSet<string> GetUserConnections(string userId)
    {
        return UserConnections.GetValueOrDefault(userId, new HashSet<string>());
    }

    #endregion
}

/// <summary>
/// DTO cho gửi tin nhắn qua SignalR
/// </summary>
public class SendMessageHub
{
    public string ConversationId { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}