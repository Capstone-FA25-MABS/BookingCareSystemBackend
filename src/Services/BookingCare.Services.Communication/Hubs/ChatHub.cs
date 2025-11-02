using System.Collections.Concurrent;
using BookingCare.Services.Communication.Constants;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Communication.Hubs;

/// <summary>
/// SignalR Hub cho real-time chat communication
/// </summary>
// [Authorize] // ⚠️ TODO: Temporarily disabled for testing SignalR connection
[AllowAnonymous] // ⚠️ TODO: Enable authentication after connection test passes
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
        ILogger<ChatHub> logger
    )
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
            UserConnections.AddOrUpdate(
                userId,
                new HashSet<string> { Context.ConnectionId },
                (key, existingConnections) =>
                {
                    existingConnections.Add(Context.ConnectionId);
                    return existingConnections;
                }
            );

            ConnectionUsers[Context.ConnectionId] = userId;

            // Add to user group for personal notifications
            var userGroupName = GetUserGroupName(userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
            _logger.LogInformation("➕ Added connection to user group: {UserGroup}", userGroupName);

            _logger.LogInformation(
                "User {UserId} connected with connection {ConnectionId}",
                userId,
                Context.ConnectionId
            );

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

            _logger.LogInformation(
                "User {UserId} disconnected with connection {ConnectionId}",
                userId,
                Context.ConnectionId
            );
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
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            // Verify user is participant in conversation
            var conversation = await _conversationService.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                await Clients.Caller.SendAsync(
                    HubConstants.ErrorMessage,
                    "Conversation không tồn tại"
                );
                _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
                return;
            }

            // Case-insensitive comparison for userId
            var isParticipant = conversation.Participants.Any(p =>
                string.Equals(p, userId, StringComparison.OrdinalIgnoreCase)
            );

            if (!isParticipant)
            {
                await Clients.Caller.SendAsync(
                    HubConstants.ErrorMessage,
                    "Không có quyền truy cập conversation này"
                );
                _logger.LogWarning(
                    "User {UserId} is not participant in conversation {ConversationId}. Participants: {Participants}",
                    userId,
                    conversationId,
                    string.Join(", ", conversation.Participants)
                );
                return;
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetConversationGroupName(conversationId)
            );
            await Clients.Caller.SendAsync("JoinedConversation", conversationId);

            _logger.LogInformation(
                "User {UserId} joined conversation {ConversationId}",
                userId,
                conversationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error joining conversation {ConversationId} for user {UserId}",
                conversationId,
                userId
            );
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                "Lỗi khi tham gia conversation"
            );
        }
    }

    /// <summary>
    /// Rời group conversation
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(conversationId)
        );
        await Clients.Caller.SendAsync("LeftConversation", conversationId);

        var userId = GetUserId();
        _logger.LogInformation(
            "User {UserId} left conversation {ConversationId}",
            userId,
            conversationId
        );
    }

    /// <summary>
    /// Gửi tin nhắn real-time
    /// </summary>
    public async Task SendMessage(SendMessageHub request)
    {
        try
        {
            _logger.LogInformation(
                "📨 SendMessage called - Raw request: ConversationId={ConversationId}, Content={Content}, ReceiverId={ReceiverId}",
                request?.ConversationId ?? "NULL",
                request?.Content?.Substring(0, Math.Min(50, request?.Content?.Length ?? 0))
                    ?? "NULL",
                request?.ReceiverId ?? "NULL"
            );

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("SendMessage rejected - User not authenticated");
                await Clients.Caller.SendAsync(
                    HubConstants.ErrorMessage,
                    HubConstants.UserNotAuthenticated
                );
                return;
            }

            try
            {
                // Validate request
                if (request == null)
                {
                    _logger.LogError("SendMessage request is NULL");
                    await Clients.Caller.SendAsync("Error", "Request không hợp lệ");
                    return;
                }

                if (string.IsNullOrEmpty(request.ConversationId))
                {
                    _logger.LogError("ConversationId is null or empty");
                    await Clients.Caller.SendAsync("Error", "ConversationId không hợp lệ");
                    return;
                }

                if (string.IsNullOrEmpty(request.Content))
                {
                    _logger.LogError("Content is null or empty");
                    await Clients.Caller.SendAsync("Error", "Nội dung tin nhắn không được trống");
                    return;
                }

                _logger.LogInformation(
                    "✅ Validation passed - Creating message for user {UserId} in conversation {ConversationId}",
                    userId,
                    request.ConversationId
                );

                // Create message through service
                var messageRequest = new CreateMessageRequest
                {
                    ConversationId = request.ConversationId,
                    SenderId = userId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    Type = MessageType.Text,
                };

                _logger.LogInformation("📝 Calling MessageService.CreateAsync...");
                var message = await _messageService.CreateAsync(messageRequest);

                if (message == null)
                {
                    _logger.LogError("❌ MessageService.CreateAsync returned null");
                    await Clients.Caller.SendAsync("Error", "Không thể tạo tin nhắn");
                    return;
                }

                _logger.LogInformation("✅ Message created with ID: {MessageId}", message.Id);

                var messagePayload = new
                {
                    MessageId = message.Id,
                    ConversationId = message.ConversationId,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    Type = message.Type,
                    CreatedAt = message.CreatedAt,
                    Status = message.Status,
                };

                // 1. Send to conversation group (for users currently in the conversation)
                var groupName = GetConversationGroupName(request.ConversationId);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", messagePayload);
                _logger.LogInformation(
                    "📤 Broadcast to conversation group: {GroupName}",
                    groupName
                );

                // 2. Send to receiver user specifically (for notifications even if not in conversation)
                if (!string.IsNullOrEmpty(request.ReceiverId))
                {
                    var receiverGroupName = GetUserGroupName(request.ReceiverId);
                    await Clients
                        .Group(receiverGroupName)
                        .SendAsync("ReceiveMessage", messagePayload);
                    _logger.LogInformation(
                        "📤 Sent notification to receiver: {ReceiverId}",
                        request.ReceiverId
                    );
                }

                _logger.LogInformation(
                    "Message sent via SignalR: {MessageId} in conversation {ConversationId}",
                    message.Id,
                    request.ConversationId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error sending message for user {UserId} in conversation {ConversationId}",
                    userId ?? "Unknown",
                    request?.ConversationId ?? "Unknown"
                );
                await Clients.Caller.SendAsync("Error", "Lỗi khi gửi tin nhắn: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌❌ FATAL: Unhandled error in SendMessage - This should never happen!"
            );
            try
            {
                await Clients.Caller.SendAsync("Error", "Lỗi nghiêm trọng khi gửi tin nhắn");
            }
            catch
            {
                // Even error notification failed - connection is dead
                _logger.LogError("Could not send error notification to client");
            }
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
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
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
                    await Clients
                        .Group(groupName)
                        .SendAsync(
                            "MessageRead",
                            new
                            {
                                MessageId = messageId,
                                ReadBy = userId,
                                ReadAt = DateTime.UtcNow,
                            }
                        );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error marking message as read: {MessageId} by user {UserId}",
                messageId,
                userId
            );
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                "Lỗi khi đánh dấu tin nhắn đã đọc"
            );
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
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            var request = new MarkAllMessagesAsReadRequest
            {
                ConversationId = conversationId,
                UserId = userId,
            };

            var result = await _messageService.MarkAllAsReadAsync(request);

            if (result)
            {
                var groupName = GetConversationGroupName(conversationId);
                await Clients
                    .Group(groupName)
                    .SendAsync(
                        "AllMessagesRead",
                        new
                        {
                            ConversationId = conversationId,
                            ReadBy = userId,
                            ReadAt = DateTime.UtcNow,
                        }
                    );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error marking all messages as read in conversation {ConversationId} by user {UserId}",
                conversationId,
                userId
            );
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                "Lỗi khi đánh dấu tất cả tin nhắn đã đọc"
            );
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
            await Clients
                .OthersInGroup(groupName)
                .SendAsync(
                    "UserStartedTyping",
                    new { UserId = userId, ConversationId = conversationId }
                );
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
            await Clients
                .OthersInGroup(groupName)
                .SendAsync(
                    "UserStoppedTyping",
                    new { UserId = userId, ConversationId = conversationId }
                );
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
    /// Tries multiple claim types to support different JWT configurations
    /// </summary>
    private string GetUserId()
    {
        // Try multiple claim types in order of preference
        var userId =
            Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst("userId")?.Value
            ?? Context.User?.FindFirst("id")?.Value
            ?? Context.User?.FindFirst("accountId")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context
                .User?.FindFirst(
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
                )
                ?.Value
            ?? string.Empty;

        // ⚠️ FALLBACK: Read from query string if claims don't work (TESTING ONLY)
        // TODO: Remove this after fixing JWT claims issue
        if (string.IsNullOrEmpty(userId))
        {
            var httpContext = Context.GetHttpContext();
            if (
                httpContext != null
                && httpContext.Request.Query.TryGetValue("userId", out var userIdFromQuery)
            )
            {
                userId = userIdFromQuery.ToString();
                _logger.LogWarning(
                    "⚠️ Using userId from query string (TESTING ONLY): {UserId}",
                    userId
                );
            }
        }

        // Debug logging
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Could not extract userId from JWT or query string. IsAuthenticated: {IsAuth}, ClaimsCount: {Count}",
                Context.User?.Identity?.IsAuthenticated,
                Context.User?.Claims?.Count() ?? 0
            );

            // Log all available claims for debugging
            if (Context.User?.Claims != null)
            {
                _logger.LogDebug("Available JWT claims:");
                foreach (var claim in Context.User.Claims)
                {
                    _logger.LogDebug("  {Type} = {Value}", claim.Type, claim.Value);
                }
            }
        }
        else
        {
            _logger.LogInformation("✅ UserId extracted: {UserId}", userId);
        }

        return userId;
    }

    /// <summary>
    /// Tạo tên group cho conversation
    /// </summary>
    private static string GetConversationGroupName(string conversationId)
    {
        return $"conversation_{conversationId}";
    }

    /// <summary>
    /// Tạo tên group cho user (để nhận notifications cá nhân)
    /// </summary>
    private static string GetUserGroupName(string userId)
    {
        return $"user_{userId}";
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
