using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Services.Communication.Utils;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation of the Message service with BaseService and file upload integration
/// </summary>
public class MessageService : BaseService, IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICallLogRepository _callLogRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IParticipantEnrichmentService _participantEnrichmentService;
    private readonly IMapper _mapper;

    public MessageService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        ICallLogRepository callLogRepository,
        IFileUploadService fileUploadService,
        ISignalRNotificationService signalRNotificationService,
        IParticipantEnrichmentService participantEnrichmentService,
        IMapper mapper,
        ILogger<MessageService> logger) : base(logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _callLogRepository = callLogRepository;
        _fileUploadService = fileUploadService;
        _signalRNotificationService = signalRNotificationService;
        _participantEnrichmentService = participantEnrichmentService;
        _mapper = mapper;
    }

    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    public async Task<MessageResponse> CreateAsync(CreateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting creation of message for conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SenderId, nameof(request.SenderId));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // Check if conversation exists
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation with ID {request.ConversationId} does not exist");
            }

            // Check if user is in the conversation
            if (!conversation.Participants.Contains(request.SenderId))
            {
                throw new UnauthorizedAccessException("User is not authorized to send messages in this conversation");
            }

            // Tạo entity từ request
            var messageEntity = _mapper.Map<MessageEntity>(request);
            var createdMessage = await _messageRepository.CreateAsync(messageEntity);

            // Cập nhật last message cho conversation
            var lastMessage = new LastMessage
            {
                MessageId = createdMessage.Id,
                Content = createdMessage.Content.Length > 100 ?
                    createdMessage.Content.Substring(0, 100) + "..." : createdMessage.Content,
                SenderId = createdMessage.SenderId,
                CreatedAt = createdMessage.CreatedAt
            };
            await _conversationRepository.UpdateLastMessageAsync(request.ConversationId, lastMessage);

            var result = _mapper.Map<MessageResponse>(createdMessage);

            // Gửi thông báo real-time qua SignalR (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _signalRNotificationService.SendMessageToConversationAsync(request.ConversationId, result);
                }
                catch (Exception ex)
                {
                    LogWarning("Error sending SignalR notification for message {MessageId}: {Error}",
                        null, result.Id, ex.Message);
                }
            });

            LogInfo("Message created successfully with ID: {MessageId}", null, createdMessage.Id);
            return result;
        }, "CreateMessage");
    }

    /// <summary>
    /// Tạo tin nhắn với file upload (Complete Flow)
    /// </summary>
    public async Task<MessageResponse> CreateMessageWithFilesAsync(CreateMessageWithFilesRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting creation of message with files for conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SenderId, nameof(request.SenderId));

            if (request.Type == MessageType.Text && request.Files.Any())
            {
                throw new ArgumentException("Text messages must not include files");
            }

            if (request.Type != MessageType.Text && !request.Files.Any())
            {
                throw new ArgumentException($"Messages of type {request.Type} require files");
            }

            // Kiểm tra conversation
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation with ID {request.ConversationId} does not exist");
            }

            if (!conversation.Participants.Contains(request.SenderId))
            {
                throw new UnauthorizedAccessException("User is not authorized to send messages in this conversation");
            }

            // Upload files
            var attachments = new List<MessageAttachment>();
            if (request.Files.Any())
            {
                var uploadResults = await _fileUploadService.UploadMultipleFilesAsync(request.Files, request.SenderId, request.Type);

                attachments = uploadResults.Select(result => new MessageAttachment
                {
                    Url = result.Url,
                    Name = result.FileName,
                    Size = result.Size,
                    MimeType = result.MimeType,
                    ThumbnailUrl = result.ThumbnailUrl,
                    Width = result.Width,
                    Height = result.Height,
                    Duration = result.Duration
                }).ToList();
            }

            // Create message entity
            var messageEntity = new MessageEntity
            {
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                Type = request.Type,
                Attachments = attachments,
                Status = MessageStatus.SENT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdMessage = await _messageRepository.CreateAsync(messageEntity);

            // Cập nhật last message
            var lastMessage = new LastMessage
            {
                MessageId = createdMessage.Id,
                Content = attachments.Any() ? $"Sent {attachments.Count} file(s)" : createdMessage.Content,
                SenderId = createdMessage.SenderId,
                CreatedAt = createdMessage.CreatedAt
            };
            await _conversationRepository.UpdateLastMessageAsync(request.ConversationId, lastMessage);

            var result = _mapper.Map<MessageResponse>(createdMessage);

            // Gửi thông báo real-time qua SignalR (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _signalRNotificationService.SendMessageToConversationAsync(request.ConversationId, result);
                }
                catch (Exception ex)
                {
                    LogWarning("Error sending SignalR notification for message with files {MessageId}: {Error}",
                        null, result.Id, ex.Message);
                }
            });

            LogInfo("Message with files created successfully with ID: {MessageId}", null, createdMessage.Id);
            return result;
        }, "CreateMessageWithFiles");
    }

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    public async Task<MessageResponse> UpdateAsync(UpdateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting update of message with ID: {MessageId}", null, request.Id);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Id, nameof(request.Id));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // Lấy tin nhắn hiện tại
            var existingMessage = await _messageRepository.GetByIdAsync(request.Id);
            if (existingMessage == null)
            {
                throw new ArgumentException($"Message with ID {request.Id} does not exist");
            }

            // Cập nhật thông tin
            existingMessage.Content = request.Content;
            existingMessage.Type = request.Type;
            existingMessage.UpdatedAt = DateTime.UtcNow;
            existingMessage.Attachments = request.Attachments.Select(a => new MessageAttachment
            {
                Url = a.Url,
                Name = a.Name,
                Size = a.Size,
                MimeType = a.MimeType
            }).ToList();

            var updatedMessage = await _messageRepository.UpdateAsync(existingMessage);

            LogInfo("Message updated successfully with ID: {MessageId}", null, updatedMessage.Id);
            return _mapper.Map<MessageResponse>(updatedMessage);
        }, "UpdateMessage");
    }

    /// <summary>
    /// Lấy tin nhắn theo ID
    /// </summary>
    public async Task<MessageResponse?> GetByIdAsync(string id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        return message != null ? _mapper.Map<MessageResponse>(message) : null;
    }

    /// <summary>
    /// Lấy danh sách tin nhắn theo conversation ID
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50)
    {
        var messages = await _messageRepository.GetByConversationIdAsync(conversationId, page, pageSize);
        return _mapper.Map<IEnumerable<MessageResponse>>(messages);
    }

    /// <summary>
    /// 🎯 NEW: Lấy danh sách tin nhắn theo conversation ID với user info enrichment
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetByConversationIdWithUserInfoAsync(string conversationId, int page = 1, int pageSize = 50, MessageLoadOptions? options = null)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Fetching messages with user info for conversation: {ConversationId}, page: {Page}, pageSize: {PageSize}",
                null, conversationId, page, pageSize);

            ValidateRequiredString(conversationId, nameof(conversationId));

            // Lấy messages từ repository
            var messages = await _messageRepository.GetByConversationIdAsync(conversationId, page, pageSize);
            var messageResponses = _mapper.Map<List<MessageResponse>>(messages);

            // Apply user info enrichment nếu options provided
            if (options != null && (options.IncludeSenderInfo || options.IncludeReceiverInfo))
            {
                await EnrichMessageUserInfoAsync(messageResponses, options);
            }

            LogInfo("Successfully fetched {Count} messages with user info for conversation: {ConversationId}",
                null, messageResponses.Count, conversationId);

            return messageResponses;
        }, "GetByConversationIdWithUserInfo");
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Starting deletion of message with ID: {MessageId}", null, id);

            // Lấy tin nhắn để xóa attachments
            var message = await _messageRepository.GetByIdAsync(id);
            if (message != null && message.Attachments.Any())
            {
                // Xóa files từ cloud storage using LINQ Select
                var attachmentUrls = message.Attachments.Select(attachment => attachment.Url).ToList();
                foreach (var url in attachmentUrls)
                {
                    try
                    {
                        await _fileUploadService.DeleteFileAsync(url);
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Failed to delete attachment {Url}: {Error}", null, url, ex.Message);
                    }
                }
            }

            var result = await _messageRepository.DeleteAsync(id);

            if (result)
            {
                LogInfo("Message deleted successfully with ID: {MessageId}", null, id);
            }
            else
            {
                LogWarning("Failed to delete message with ID: {MessageId}", null, id);
            }

            return result;
        }, "DeleteMessage");
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Marking message as read: {MessageId}", null, request.MessageId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.MessageId, nameof(request.MessageId));

            // Get message để lấy conversation ID
            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
            {
                throw new ArgumentException($"Message with ID {request.MessageId} does not exist");
            }

            var result = await _messageRepository.MarkAsReadAsync(request.MessageId, DateTime.UtcNow);

            if (result)
            {
                LogInfo("Đánh dấu tin nhắn đã đọc thành công: {MessageId}", null, request.MessageId);

                // Gửi thông báo real-time qua SignalR (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _signalRNotificationService.SendMessageReadNotificationAsync(
                            message.ConversationId, request.MessageId, message.ReceiverId ?? "");
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Error sending SignalR read notification for {MessageId}: {Error}",
                            null, request.MessageId, ex.Message);
                    }
                });
            }

            return result;
        }, "MarkMessageAsRead");
    }

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc trong conversation là đã đọc
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(MarkAllMessagesAsReadRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Marking all messages as read for conversation: {ConversationId}, user: {UserId}",
                null, request.ConversationId, request.UserId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.UserId, nameof(request.UserId));

            // Kiểm tra conversation có tồn tại không
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation with ID {request.ConversationId} does not exist");
            }

            // Kiểm tra user có trong conversation không
            if (!conversation.Participants.Contains(request.UserId))
            {
                throw new UnauthorizedAccessException("User is not authorized to read messages in this conversation");
            }

            var result = await _messageRepository.MarkAllAsReadAsync(request.ConversationId, request.UserId, DateTime.UtcNow);

            if (result)
            {
                LogInfo("Đánh dấu tất cả tin nhắn đã đọc thành công cho conversation: {ConversationId}, user: {UserId}",
                    null, request.ConversationId, request.UserId);

                // Gửi thông báo real-time qua SignalR (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _signalRNotificationService.SendAllMessagesReadNotificationAsync(
                            request.ConversationId, request.UserId);
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Error sending SignalR all-messages-read notification for conversation {ConversationId}: {Error}",
                            null, request.ConversationId, ex.Message);
                    }
                });
            }
            else
            {
                LogInfo("No messages were marked as read (may already be read) for conversation: {ConversationId}, user: {UserId}",
                    null, request.ConversationId, request.UserId);
            }

            return result;
        }, "MarkAllMessagesAsRead");
    }

    /// <summary>
    /// Lấy số tin nhắn chưa đọc
    /// </summary>
    public async Task<long> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _messageRepository.GetUnreadCountAsync(conversationId, userId);
    }

    /// <summary>
    /// Tìm kiếm tin nhắn
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Searching messages in conversation: {ConversationId} with term: {SearchTerm}",
                null, request.ConversationId, request.SearchTerm);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SearchTerm, nameof(request.SearchTerm));

            var messages = await _messageRepository.SearchAsync(request.ConversationId, request.SearchTerm, request.Page, request.PageSize);

            LogInfo("Found {Count} messages", null, messages.Count());
            return _mapper.Map<IEnumerable<MessageResponse>>(messages);
        }, "SearchMessages");
    }

    /// <summary>
    /// Lấy tin nhắn theo loại (simplified implementation using existing methods)
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(String conversationId, MessageType messageType, int page = 1, int pageSize = 20)
    {
        // Lấy tất cả tin nhắn cho conversation và lọc theo loại trong bộ nhớ
        // Không tối ưu cho production nhưng tạm thời sử dụng
        var allMessages = await _messageRepository.GetByConversationIdAsync(conversationId, 1, 1000);
        var filteredMessages = allMessages
            .Where(m => m.Type == messageType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return _mapper.Map<IEnumerable<MessageResponse>>(filteredMessages);
    }

    /// <summary>
    /// Lấy tất cả file attachments trong conversation (simplified implementation)
    /// </summary>
    public async Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(string conversationId, MessageType? messageType = null, int page = 1, int pageSize = 50)
    {
        // Lấy tất cả tin nhắn và lọc những tin có attachments
        var allMessages = await _messageRepository.GetByConversationIdAsync(conversationId, 1, 1000);

        var messagesWithAttachments = messageType.HasValue
            ? allMessages.Where(m => m.Type == messageType.Value && m.Attachments.Any())
            : allMessages.Where(m => m.Attachments.Any());

        var attachments = messagesWithAttachments
            .SelectMany(m => m.Attachments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return _mapper.Map<IEnumerable<MessageAttachmentResponse>>(attachments);
    }


    /// <summary>
    /// Lấy mixed timeline (messages + call logs) cho conversation
    /// </summary>
    public async Task<MixedTimelineResponse> GetMixedTimelineAsync(GetMixedTimelineRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Fetching mixed timeline for conversation: {ConversationId}, before: {Before}, after: {After}, limit: {Limit}",
                null, request.ConversationId, request.Before, request.After, request.Limit);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));

            if (request.Limit <= 0 || request.Limit > 200)
            {
                throw new ArgumentException("Limit must be between 1 and 200");
            }

            if (request.MessagesOnly && request.CallLogsOnly)
            {
                throw new ArgumentException("Cannot select both MessagesOnly and CallLogsOnly");
            }

            // Parse cursors để lấy timestamp filter
            DateTime? beforeTimestamp = null;
            DateTime? afterTimestamp = null;

            if (!string.IsNullOrEmpty(request.Before))
            {
                var (timestamp, _) = CursorHelper.ParseCursor(request.Before);
                beforeTimestamp = timestamp;
            }

            if (!string.IsNullOrEmpty(request.After))
            {
                var (timestamp, _) = CursorHelper.ParseCursor(request.After);
                afterTimestamp = timestamp;
            }

            var timelineItems = new List<TimelineItem>();

            // Lấy messages (nếu không chỉ lấy call logs)
            if (!request.CallLogsOnly)
            {
                var messages = await _messageRepository.GetByConversationIdForTimelineAsync(
                    request.ConversationId,
                    beforeTimestamp,
                    afterTimestamp,
                    request.Limit + 50, // Lấy thêm để đảm bảo có đủ khi merge
                    request.MessageTypeFilter
                );

                timelineItems.AddRange(messages.Select(m => new TimelineItem
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    CreatedAt = m.CreatedAt,
                    ItemType = TimelineItemType.Message,
                    Message = _mapper.Map<MessageResponse>(m),
                    CallLog = null
                }));
            }

            // Lấy call logs (nếu không chỉ lấy messages)
            if (!request.MessagesOnly)
            {
                var callLogs = await _callLogRepository.GetByConversationIdForTimelineAsync(
                    request.ConversationId,
                    beforeTimestamp,
                    afterTimestamp,
                    request.Limit + 50, // Lấy thêm để đảm bảo có đủ khi merge
                    request.CallTypeFilter
                );

                timelineItems.AddRange(callLogs.Select(c => new TimelineItem
                {
                    Id = c.Id,
                    ConversationId = c.ConversationId,
                    CreatedAt = c.StartedAt,
                    ItemType = TimelineItemType.CallLog,
                    Message = null,
                    CallLog = _mapper.Map<CallLogResponse>(c)
                }));
            }

            // Sort by time descending and take the required number
            var sortedItems = timelineItems
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(request.Limit + 1) // +1 để check hasNext
                .ToList();

            // Determine pagination info
            var hasNext = sortedItems.Count > request.Limit;
            var hasPrevious = !string.IsNullOrEmpty(request.Before) || !string.IsNullOrEmpty(request.After);

            // Remove extra item if exists
            if (hasNext)
            {
                sortedItems.RemoveAt(sortedItems.Count - 1);
            }

            // Generate cursors
            string? nextCursor = null;
            string? previousCursor = null;

            if (sortedItems.Any())
            {
                if (hasNext)
                {
                    var lastItem = sortedItems[sortedItems.Count - 1];
                    nextCursor = CursorHelper.GenerateCursor(lastItem.Id, lastItem.CreatedAt);
                }

                if (hasPrevious || !string.IsNullOrEmpty(request.Before))
                {
                    var firstItem = sortedItems[0];
                    previousCursor = CursorHelper.GenerateCursor(firstItem.Id, firstItem.CreatedAt);
                }
            }

            var result = new MixedTimelineResponse
            {
                Items = sortedItems,
                NextCursor = nextCursor,
                PreviousCursor = previousCursor,
                HasNext = hasNext,
                HasPrevious = hasPrevious,
                Limit = request.Limit
            };

            LogInfo("Successfully fetched mixed timeline with {Count} items for conversation: {ConversationId}",
                null, sortedItems.Count, request.ConversationId);

            return result;
        }, "GetMixedTimeline");
    }

    /// <summary>
    /// 🎯 NEW: Lấy mixed timeline với user info enrichment
    /// </summary>
    public async Task<MixedTimelineResponse> GetMixedTimelineWithUserInfoAsync(GetMixedTimelineRequest request, MessageLoadOptions? options = null)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Fetching mixed timeline with user info for conversation: {ConversationId}, options: {Options}",
                null, request.ConversationId, options?.IncludeSenderInfo);

            // Lấy mixed timeline bình thường
            var result = await GetMixedTimelineAsync(request);

            // Apply user info enrichment nếu options provided
            if (options != null && (options.IncludeSenderInfo || options.IncludeReceiverInfo))
            {
                // Extract messages from timeline items
                var messages = result.Items
                    .Where(item => item.ItemType == TimelineItemType.Message && item.Message != null)
                    .Select(item => item.Message!)
                    .ToList();

                if (messages.Any())
                {
                    await EnrichMessageUserInfoAsync(messages, options);

                    // Count enrichment statistics
                    var messagesWithSenderInfo = messages.Count(m => m.SenderInfo != null);
                    var messagesWithReceiverInfo = messages.Count(m => m.ReceiverInfo != null);
                    var totalUsersEnriched = messages.SelectMany(m => new[] { m.SenderInfo, m.ReceiverInfo })
                        .Where(info => info != null)
                        .Select(info => info!.Id)
                        .Distinct()
                        .Count();

                    // Add enrichment info
                    result.EnrichmentInfo = new MessageEnrichmentInfo
                    {
                        SenderInfoLoaded = options.IncludeSenderInfo,
                        ReceiverInfoLoaded = options.IncludeReceiverInfo,
                        OnlineStatusLoaded = options.IncludeOnlineStatus,
                        TotalUsersEnriched = totalUsersEnriched,
                        MessagesWithSenderInfo = messagesWithSenderInfo,
                        MessagesWithReceiverInfo = messagesWithReceiverInfo
                    };

                    LogInfo("Enriched mixed timeline with {TotalUsers} users, {SenderCount} sender info, {ReceiverCount} receiver info",
                        null, totalUsersEnriched, messagesWithSenderInfo, messagesWithReceiverInfo);
                }
            }

            return result;
        }, "GetMixedTimelineWithUserInfo");
    }

    #region Private Helper Methods

    /// <summary>
    /// Enrichment user info cho danh sách messages
    /// </summary>
    private async Task EnrichMessageUserInfoAsync(IEnumerable<MessageResponse> messages, MessageLoadOptions options)
    {
        var messageList = messages.ToList();
        if (!messageList.Any()) return;

        LogDebug("Starting enrichment of user info for {Count} messages", null, messageList.Count);

        // Collect unique user IDs cần enrichment
        var userIds = new HashSet<string>();

        if (options.IncludeSenderInfo)
        {
            foreach (var message in messageList)
            {
                if (!string.IsNullOrEmpty(message.SenderId))
                {
                    userIds.Add(message.SenderId.ToLowerInvariant());
                }
            }
        }

        if (options.IncludeReceiverInfo)
        {
            foreach (var message in messageList)
            {
                if (!string.IsNullOrEmpty(message.ReceiverId))
                {
                    userIds.Add(message.ReceiverId.ToLowerInvariant());
                }
            }
        }

        if (!userIds.Any())
        {
            LogDebug("No user IDs to enrich", null);
            return;
        }

        LogDebug("Enriching user info for {Count} unique users: {UserIds}",
            null, userIds.Count, string.Join(", ", userIds.Take(5)));

        try
        {
            // Lấy user details từ ParticipantEnrichmentService
            var userDetails = await _participantEnrichmentService.GetAccountDetailsAsync(userIds);

            // Apply user info cho từng message
            foreach (var message in messageList)
            {
                // Enrich sender info
                if (options.IncludeSenderInfo && !string.IsNullOrEmpty(message.SenderId))
                {
                    var normalizedSenderId = message.SenderId.ToLowerInvariant();
                    if (userDetails.TryGetValue(normalizedSenderId, out var senderDetail))
                    {
                        message.SenderInfo = new MessageSenderInfo
                        {
                            Id = senderDetail.Id,
                            FullName = senderDetail.FullName,
                            Email = senderDetail.Email,
                            AvatarUrl = senderDetail.AvatarUrl,
                            Role = senderDetail.Role,
                            IsOnline = senderDetail.IsOnline
                        };
                    }
                }

                // Enrich receiver info
                if (options.IncludeReceiverInfo && !string.IsNullOrEmpty(message.ReceiverId))
                {
                    var normalizedReceiverId = message.ReceiverId.ToLowerInvariant();
                    if (userDetails.TryGetValue(normalizedReceiverId, out var receiverDetail))
                    {
                        message.ReceiverInfo = new MessageSenderInfo
                        {
                            Id = receiverDetail.Id,
                            FullName = receiverDetail.FullName,
                            Email = receiverDetail.Email,
                            AvatarUrl = receiverDetail.AvatarUrl,
                            Role = receiverDetail.Role,
                            IsOnline = receiverDetail.IsOnline
                        };
                    }
                }
            }

            var enrichedSenders = messageList.Count(m => m.SenderInfo != null);
            var enrichedReceivers = messageList.Count(m => m.ReceiverInfo != null);

            LogDebug("Completed enrichment of user info: {Senders} sender info, {Receivers} receiver info",
                null, enrichedSenders, enrichedReceivers);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error enriching user info for messages", null);
            // Continue without user enrichment
        }
    }

    #endregion
}