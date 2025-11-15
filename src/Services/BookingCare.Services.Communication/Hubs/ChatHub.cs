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
/// NOTE: [Authorize] is removed from class level to allow negotiate endpoint
/// Authorization is checked in OnConnectedAsync instead
/// </summary>
[AllowAnonymous]
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
    /// Authorization check is performed here instead of at class level
    /// </summary>
    public override async Task OnConnectedAsync()
    {


        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "❌ Connection attempt without valid userId from {ConnectionId}",
                Context.ConnectionId
            );
            Context.Abort();
            return;
        }

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
        _logger.LogInformation(
            "➕ User {UserId} connected with connection {ConnectionId} - Added to group: {UserGroup}",
            userId,
            Context.ConnectionId,
            userGroupName
        );

        // 1. Send list of currently online users to the newly connected user
        var onlineUsers = UserConnections.Keys.ToList();
        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);

        // 2. Notify other users that this user is now online
        await Clients.Others.SendAsync("UserOnline", userId);
        _logger.LogInformation(
            "📤 User {UserId} is now online - Sent online users list ({Count} users) and notified others",
            userId,
            onlineUsers.Count
        );

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
                request?.Content is not null
                    ? request.Content.Substring(0, Math.Min(50, request.Content.Length))
                    : "NULL",
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
                    await Clients.Caller.SendAsync(
                        HubConstants.ErrorMessage,
                        "Request không hợp lệ"
                    );
                    return;
                }

                if (string.IsNullOrEmpty(request.ConversationId))
                {
                    _logger.LogError("ConversationId is null or empty");
                    await Clients.Caller.SendAsync(
                        HubConstants.ErrorMessage,
                        "ConversationId không hợp lệ"
                    );
                    return;
                }

                if (string.IsNullOrEmpty(request.Content))
                {
                    _logger.LogError("Content is null or empty");
                    await Clients.Caller.SendAsync(
                        HubConstants.ErrorMessage,
                        "Nội dung tin nhắn không được trống"
                    );
                    return;
                }

                // Create message through service
                var messageRequest = new CreateMessageRequest
                {
                    ConversationId = request.ConversationId,
                    SenderId = userId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    Type = MessageType.Text,
                };

                _logger.LogInformation(
                    "✅ Validation passed - Creating message for user {UserId} in conversation {ConversationId}",
                    userId,
                    request.ConversationId
                );
                var message = await _messageService.CreateAsync(messageRequest);

                if (message == null)
                {
                    _logger.LogError("❌ MessageService.CreateAsync returned null");
                    await Clients.Caller.SendAsync(
                        HubConstants.ErrorMessage,
                        "Không thể tạo tin nhắn"
                    );
                    return;
                }

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
                    Attachments = message.Attachments, // ✅ Include attachments for real-time updates
                };

                // 1. Send to conversation group (for users currently in the conversation)
                var groupName = GetConversationGroupName(request.ConversationId);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", messagePayload);

                // 2. Send to receiver user specifically (for notifications even if not in conversation)
                if (!string.IsNullOrEmpty(request.ReceiverId))
                {
                    var receiverGroupName = GetUserGroupName(request.ReceiverId);
                    await Clients
                        .Group(receiverGroupName)
                        .SendAsync("ReceiveMessage", messagePayload);
                }

                _logger.LogInformation(
                    "✅ Message created with ID: {MessageId} and sent via SignalR to conversation {ConversationId} and receiver {ReceiverId}",
                    message.Id,
                    request.ConversationId,
                    request.ReceiverId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error sending message for user {UserId} in conversation {ConversationId}",
                    userId,
                    request?.ConversationId
                );
                await Clients.Caller.SendAsync(
                    HubConstants.ErrorMessage,
                    "Lỗi khi gửi tin nhắn: " + ex.Message
                );
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
                await Clients.Caller.SendAsync(
                    HubConstants.ErrorMessage,
                    "Lỗi nghiêm trọng khi gửi tin nhắn"
                );
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

    #region WebRTC Call Signaling

    /// <summary>
    /// Bắt đầu cuộc gọi (caller gửi tín hiệu đến callee)
    /// </summary>
    public async Task StartCall(StartCallRequest request)
    {
        var callerId = GetUserId();
        if (string.IsNullOrEmpty(callerId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "📞 StartCall: {CallerId} calling {CalleeId} in conversation {ConversationId}",
                callerId,
                request.CalleeId,
                request.ConversationId
            );

            var callData = new
            {
                CallerId = callerId,
                CalleeId = request.CalleeId,
                ConversationId = request.ConversationId,
                CallType = request.CallType, // "video" or "audio"
                CallerName = request.CallerName,
                CallerAvatar = request.CallerAvatar,
            };

            // Send to callee's user group
            var calleeGroupName = GetUserGroupName(request.CalleeId);
            await Clients.Group(calleeGroupName).SendAsync("IncomingCall", callData);

            _logger.LogInformation("📤 Sent IncomingCall to {CalleeId}", request.CalleeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error starting call from {CallerId} to {CalleeId}",
                callerId,
                request.CalleeId
            );
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi bắt đầu cuộc gọi");
        }
    }

    /// <summary>
    /// Chấp nhận cuộc gọi (callee phản hồi caller)
    /// </summary>
    public async Task AcceptCall(AcceptCallRequest request)
    {
        var calleeId = GetUserId();
        if (string.IsNullOrEmpty(calleeId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "✅ AcceptCall: {CalleeId} accepted call from {CallerId}",
                calleeId,
                request.CallerId
            );

            var acceptData = new
            {
                CalleeId = calleeId,
                CallerId = request.CallerId,
                ConversationId = request.ConversationId,
            };

            // Notify caller that call was accepted
            var callerGroupName = GetUserGroupName(request.CallerId);
            await Clients.Group(callerGroupName).SendAsync("CallAccepted", acceptData);

            _logger.LogInformation("📤 Sent CallAccepted to {CallerId}", request.CallerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting call from {CallerId}", request.CallerId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi chấp nhận cuộc gọi");
        }
    }

    /// <summary>
    /// Từ chối cuộc gọi
    /// </summary>
    public async Task DeclineCall(DeclineCallRequest request)
    {
        var calleeId = GetUserId();
        if (string.IsNullOrEmpty(calleeId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "❌ DeclineCall: {CalleeId} declined call from {CallerId}",
                calleeId,
                request.CallerId
            );

            var declineData = new
            {
                CalleeId = calleeId,
                CallerId = request.CallerId,
                Reason = request.Reason ?? "declined",
            };

            // Notify caller that call was declined
            var callerGroupName = GetUserGroupName(request.CallerId);
            await Clients.Group(callerGroupName).SendAsync("CallDeclined", declineData);

            _logger.LogInformation("📤 Sent CallDeclined to {CallerId}", request.CallerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining call from {CallerId}", request.CallerId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi từ chối cuộc gọi");
        }
    }

    /// <summary>
    /// Kết thúc cuộc gọi
    /// </summary>
    public async Task EndCall(EndCallRequest request)
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
            _logger.LogInformation(
                "📵 EndCall: {UserId} ending call with {OtherUserId}",
                userId,
                request.OtherUserId
            );

            var endData = new
            {
                UserId = userId,
                OtherUserId = request.OtherUserId,
                Reason = request.Reason ?? "ended",
            };

            // Notify other user
            var otherUserGroupName = GetUserGroupName(request.OtherUserId);
            await Clients.Group(otherUserGroupName).SendAsync("CallEnded", endData);

            _logger.LogInformation("📤 Sent CallEnded to {OtherUserId}", request.OtherUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending call with {OtherUserId}", request.OtherUserId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi kết thúc cuộc gọi");
        }
    }

    /// <summary>
    /// Gửi WebRTC Offer (SDP)
    /// </summary>
    public async Task SendOffer(WebRtcSignalRequest request)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "📡 SendOffer: {SenderId} -> {ReceiverId}",
                senderId,
                request.ReceiverId
            );

            var offerData = new
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Offer = request.Signal, // SDP offer
            };

            // Send to receiver
            var receiverGroupName = GetUserGroupName(request.ReceiverId);
            await Clients.Group(receiverGroupName).SendAsync("ReceiveOffer", offerData);

            _logger.LogInformation("📤 Sent Offer to {ReceiverId}", request.ReceiverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending offer to {ReceiverId}", request.ReceiverId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi gửi WebRTC offer");
        }
    }

    /// <summary>
    /// Gửi WebRTC Answer (SDP)
    /// </summary>
    public async Task SendAnswer(WebRtcSignalRequest request)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "📡 SendAnswer: {SenderId} -> {ReceiverId}",
                senderId,
                request.ReceiverId
            );

            var answerData = new
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Answer = request.Signal, // SDP answer
            };

            // Send to receiver
            var receiverGroupName = GetUserGroupName(request.ReceiverId);
            await Clients.Group(receiverGroupName).SendAsync("ReceiveAnswer", answerData);

            _logger.LogInformation("📤 Sent Answer to {ReceiverId}", request.ReceiverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending answer to {ReceiverId}", request.ReceiverId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi gửi WebRTC answer");
        }
    }

    /// <summary>
    /// Gửi ICE Candidate
    /// </summary>
    public async Task SendIceCandidate(IceCandidateRequest request)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId))
        {
            await Clients.Caller.SendAsync(
                HubConstants.ErrorMessage,
                HubConstants.UserNotAuthenticated
            );
            return;
        }

        try
        {
            _logger.LogInformation(
                "🧊 SendIceCandidate: {SenderId} -> {ReceiverId}",
                senderId,
                request.ReceiverId
            );

            var candidateData = new
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Candidate = request.Candidate,
            };

            // Send to receiver
            var receiverGroupName = GetUserGroupName(request.ReceiverId);
            await Clients.Group(receiverGroupName).SendAsync("ReceiveIceCandidate", candidateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ICE candidate to {ReceiverId}", request.ReceiverId);
            // Don't send error to client - ICE candidates are not critical
        }
    }

    /// <summary>
    /// Báo user đang bận (đã có cuộc gọi khác)
    /// </summary>
    public async Task CallBusy(CallBusyRequest request)
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
            _logger.LogInformation(
                "📞 CallBusy: {UserId} is busy, notifying {CallerId}",
                userId,
                request.CallerId
            );

            var busyData = new { UserId = userId, CallerId = request.CallerId };

            // Notify caller that user is busy
            var callerGroupName = GetUserGroupName(request.CallerId);
            await Clients.Group(callerGroupName).SendAsync("UserBusy", busyData);

            _logger.LogInformation("📤 Sent UserBusy to {CallerId}", request.CallerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending busy status to {CallerId}", request.CallerId);
            await Clients.Caller.SendAsync(HubConstants.ErrorMessage, "Lỗi khi gửi trạng thái bận");
        }
    }

    #endregion

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

        // ⚠️ IMPORTANT: Normalize to UPPERCASE for case-insensitive matching
        // This ensures consistent group names and dictionary lookups
        return userId?.ToUpperInvariant() ?? string.Empty;
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
    /// ⚠️ IMPORTANT: Normalize to UPPERCASE to handle case-insensitive userId matching
    /// </summary>
    private static string GetUserGroupName(string userId)
    {
        return $"user_{userId?.ToUpperInvariant() ?? string.Empty}";
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

#region WebRTC Call DTOs

/// <summary>
/// Request để bắt đầu cuộc gọi
/// </summary>
public class StartCallRequest
{
    public string CalleeId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string CallType { get; set; } = "video"; // "video" or "audio"
    public string? CallerName { get; set; }
    public string? CallerAvatar { get; set; }
}

/// <summary>
/// Request để chấp nhận cuộc gọi
/// </summary>
public class AcceptCallRequest
{
    public string CallerId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
}

/// <summary>
/// Request để từ chối cuộc gọi
/// </summary>
public class DeclineCallRequest
{
    public string CallerId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Request để kết thúc cuộc gọi
/// </summary>
public class EndCallRequest
{
    public string OtherUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Request để gửi WebRTC signal (Offer/Answer)
/// </summary>
public class WebRtcSignalRequest
{
    public string ReceiverId { get; set; } = string.Empty;
    public object Signal { get; set; } = null!; // RTCSessionDescriptionInit
}

/// <summary>
/// Request để gửi ICE Candidate
/// </summary>
public class IceCandidateRequest
{
    public string ReceiverId { get; set; } = string.Empty;
    public object Candidate { get; set; } = null!; // RTCIceCandidateInit
}

/// <summary>
/// Request để báo đang bận
/// </summary>
public class CallBusyRequest
{
    public string CallerId { get; set; } = string.Empty;
}

#endregion
