using AutoMapper;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Utils;
using BookingCare.Shared.Common.Services;

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
        ILogger<MessageService> logger
    )
        : base(logger)
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
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting creation of message for conversation: {ConversationId}",
                    null,
                    request.ConversationId
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
                ValidateRequiredString(request.SenderId, nameof(request.SenderId));
                ValidateRequiredString(request.Content, nameof(request.Content));

                // Check if conversation exists
                var conversation = await _conversationRepository.GetByIdAsync(
                    request.ConversationId
                );
                if (conversation == null)
                {
                    throw new ArgumentException(
                        $"Conversation with ID {request.ConversationId} does not exist"
                    );
                }

                // Check if user is in the conversation (case-insensitive)
                var isParticipant = conversation.Participants.Any(p =>
                    string.Equals(p, request.SenderId, StringComparison.OrdinalIgnoreCase)
                );

                if (!isParticipant)
                {
                    throw new UnauthorizedAccessException(
                        "User is not authorized to send messages in this conversation"
                    );
                }

                // Tạo entity từ request
                var messageEntity = _mapper.Map<MessageEntity>(request);
                var createdMessage = await _messageRepository.CreateAsync(messageEntity);

                // Cập nhật last message cho conversation
                var lastMessage = new LastMessage
                {
                    MessageId = createdMessage.Id,
                    Content =
                        createdMessage.Content.Length > 100
                            ? createdMessage.Content.Substring(0, 100) + "..."
                            : createdMessage.Content,
                    SenderId = createdMessage.SenderId,
                    CreatedAt = createdMessage.CreatedAt,
                };
                await _conversationRepository.UpdateLastMessageAsync(
                    request.ConversationId,
                    lastMessage
                );

                var result = _mapper.Map<MessageResponse>(createdMessage);

                // Gửi thông báo real-time qua SignalR (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _signalRNotificationService.SendMessageToConversationAsync(
                            request.ConversationId,
                            result
                        );
                    }
                    catch (Exception ex)
                    {
                        LogWarning(
                            "Error sending SignalR notification for message {MessageId}: {Error}",
                            null,
                            result.Id,
                            ex.Message
                        );
                    }
                });

                LogInfo(
                    "Message created successfully with ID: {MessageId}",
                    null,
                    createdMessage.Id
                );
                return result;
            },
            "CreateMessage"
        );
    }

    /// <summary>
    /// Tạo tin nhắn với file upload (Complete Flow)
    /// </summary>
    public async Task<MessageResponse> CreateMessageWithFilesAsync(
        CreateMessageWithFilesRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting creation of message with files for conversation: {ConversationId}",
                    null,
                    request.ConversationId
                );

                // Validation
                ValidateCreateMessageWithFilesRequest(request);

                // Kiểm tra conversation và authorization
                await ValidateConversationAccessAsync(request.ConversationId, request.SenderId);

                // Upload files và tạo attachments
                var attachments = await ProcessFileUploadsAsync(request);

                // Create message entity
                var messageEntity = CreateMessageEntity(request, attachments);
                var createdMessage = await _messageRepository.CreateAsync(messageEntity);

                // Cập nhật last message
                await UpdateConversationLastMessageAsync(
                    request.ConversationId,
                    createdMessage,
                    attachments
                );

                var result = _mapper.Map<MessageResponse>(createdMessage);

                // Gửi thông báo real-time qua SignalR (fire and forget)
                SendSignalRNotification(request.ConversationId, result);

                LogInfo(
                    "Message with files created successfully with ID: {MessageId}",
                    null,
                    createdMessage.Id
                );
                return result;
            },
            "CreateMessageWithFiles"
        );
    }

    /// <summary>
    /// Validate request cho CreateMessageWithFiles
    /// </summary>
    private static void ValidateCreateMessageWithFilesRequest(CreateMessageWithFilesRequest request)
    {
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
    }

    /// <summary>
    /// Validate conversation existence và user authorization
    /// </summary>
    private async Task ValidateConversationAccessAsync(string conversationId, string senderId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new ArgumentException($"Conversation with ID {conversationId} does not exist");
        }

        // Case-insensitive participant check
        var isParticipant = conversation.Participants.Any(p =>
            string.Equals(p, senderId, StringComparison.OrdinalIgnoreCase)
        );

        if (!isParticipant)
        {
            throw new UnauthorizedAccessException(
                "User is not authorized to send messages in this conversation"
            );
        }
    }

    /// <summary>
    /// Process file uploads và tạo message attachments
    /// </summary>
    private async Task<List<MessageAttachment>> ProcessFileUploadsAsync(
        CreateMessageWithFilesRequest request
    )
    {
        var attachments = new List<MessageAttachment>();

        if (request.Files.Any())
        {
            var uploadResults = await _fileUploadService.UploadMultipleFilesAsync(
                request.Files,
                request.SenderId,
                request.Type
            );

            attachments = uploadResults
                .Select(result => new MessageAttachment
                {
                    Url = result.Url,
                    Name = result.FileName,
                    Size = result.Size,
                    MimeType = result.MimeType,
                    ThumbnailUrl = result.ThumbnailUrl,
                    Width = result.Width,
                    Height = result.Height,
                    Duration = result.Duration,
                })
                .ToList();
        }

        return attachments;
    }

    /// <summary>
    /// Create message entity từ request và attachments
    /// </summary>
    private static MessageEntity CreateMessageEntity(
        CreateMessageWithFilesRequest request,
        List<MessageAttachment> attachments
    )
    {
        return new MessageEntity
        {
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Content = request.Content,
            Type = request.Type,
            Attachments = attachments,
            Status = MessageStatus.SENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Update last message cho conversation
    /// </summary>
    private async Task UpdateConversationLastMessageAsync(
        string conversationId,
        MessageEntity createdMessage,
        List<MessageAttachment> attachments
    )
    {
        var lastMessage = new LastMessage
        {
            MessageId = createdMessage.Id,
            Content = attachments.Any()
                ? $"Sent {attachments.Count} file(s)"
                : createdMessage.Content,
            SenderId = createdMessage.SenderId,
            CreatedAt = createdMessage.CreatedAt,
        };

        await _conversationRepository.UpdateLastMessageAsync(conversationId, lastMessage);
    }

    /// <summary>
    /// Send SignalR notification (fire and forget)
    /// </summary>
    private void SendSignalRNotification(string conversationId, MessageResponse result)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _signalRNotificationService.SendMessageToConversationAsync(
                    conversationId,
                    result
                );
            }
            catch (Exception ex)
            {
                LogWarning(
                    "Error sending SignalR notification for message with files {MessageId}: {Error}",
                    null,
                    result.Id,
                    ex.Message
                );
            }
        });
    }

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    public async Task<MessageResponse> UpdateAsync(UpdateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
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
                existingMessage.Attachments = request
                    .Attachments.Select(a => new MessageAttachment
                    {
                        Url = a.Url,
                        Name = a.Name,
                        Size = a.Size,
                        MimeType = a.MimeType,
                    })
                    .ToList();

                var updatedMessage = await _messageRepository.UpdateAsync(existingMessage);

                LogInfo(
                    "Message updated successfully with ID: {MessageId}",
                    null,
                    updatedMessage.Id
                );
                return _mapper.Map<MessageResponse>(updatedMessage);
            },
            "UpdateMessage"
        );
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
    public async Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(
        string conversationId,
        int page = 1,
        int pageSize = 50
    )
    {
        var messages = await _messageRepository.GetByConversationIdAsync(
            conversationId,
            page,
            pageSize
        );
        return _mapper.Map<IEnumerable<MessageResponse>>(messages);
    }

    /// <summary>
    /// 🎯 NEW: Lấy danh sách tin nhắn theo conversation ID với user info enrichment
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetByConversationIdWithUserInfoAsync(
        string conversationId,
        int page = 1,
        int pageSize = 50,
        MessageLoadOptions? options = null
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching messages with user info for conversation: {ConversationId}, page: {Page}, pageSize: {PageSize}",
                    null,
                    conversationId,
                    page,
                    pageSize
                );

                ValidateRequiredString(conversationId, nameof(conversationId));

                // Lấy messages từ repository
                var messages = await _messageRepository.GetByConversationIdAsync(
                    conversationId,
                    page,
                    pageSize
                );
                var messageResponses = _mapper.Map<List<MessageResponse>>(messages);

                // Apply user info enrichment nếu options provided
                if (options != null && (options.IncludeSenderInfo || options.IncludeReceiverInfo))
                {
                    await EnrichMessageUserInfoAsync(messageResponses, options);
                }

                LogInfo(
                    "Successfully fetched {Count} messages with user info for conversation: {ConversationId}",
                    null,
                    messageResponses.Count,
                    conversationId
                );

                return messageResponses;
            },
            "GetByConversationIdWithUserInfo"
        );
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Starting deletion of message with ID: {MessageId}", null, id);

                // Lấy tin nhắn để xóa attachments
                var message = await _messageRepository.GetByIdAsync(id);
                if (message != null && message.Attachments.Any())
                {
                    // Xóa files từ cloud storage using LINQ Select
                    var attachmentUrls = message
                        .Attachments.Select(attachment => attachment.Url)
                        .ToList();
                    foreach (var url in attachmentUrls)
                    {
                        try
                        {
                            await _fileUploadService.DeleteFileAsync(url);
                        }
                        catch (Exception ex)
                        {
                            LogWarning(
                                "Failed to delete attachment {Url}: {Error}",
                                null,
                                url,
                                ex.Message
                            );
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
            },
            "DeleteMessage"
        );
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Marking message as read: {MessageId}", null, request.MessageId);

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.MessageId, nameof(request.MessageId));

                // Get message để lấy conversation ID
                var message = await _messageRepository.GetByIdAsync(request.MessageId);
                if (message == null)
                {
                    throw new ArgumentException(
                        $"Message with ID {request.MessageId} does not exist"
                    );
                }

                var result = await _messageRepository.MarkAsReadAsync(
                    request.MessageId,
                    DateTime.UtcNow
                );

                if (result)
                {
                    LogInfo(
                        "Đánh dấu tin nhắn đã đọc thành công: {MessageId}",
                        null,
                        request.MessageId
                    );

                    // Gửi thông báo real-time qua SignalR (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _signalRNotificationService.SendMessageReadNotificationAsync(
                                message.ConversationId,
                                request.MessageId,
                                message.ReceiverId ?? ""
                            );
                        }
                        catch (Exception ex)
                        {
                            LogWarning(
                                "Error sending SignalR read notification for {MessageId}: {Error}",
                                null,
                                request.MessageId,
                                ex.Message
                            );
                        }
                    });
                }

                return result;
            },
            "MarkMessageAsRead"
        );
    }

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc trong conversation là đã đọc
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(MarkAllMessagesAsReadRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Marking all messages as read for conversation: {ConversationId}, user: {UserId}",
                    null,
                    request.ConversationId,
                    request.UserId
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
                ValidateRequiredString(request.UserId, nameof(request.UserId));

                // Kiểm tra conversation có tồn tại không
                var conversation = await _conversationRepository.GetByIdAsync(
                    request.ConversationId
                );
                if (conversation == null)
                {
                    throw new ArgumentException(
                        $"Conversation with ID {request.ConversationId} does not exist"
                    );
                }

                // Kiểm tra user có trong conversation không (case-insensitive)
                var isParticipant = conversation.Participants.Any(p =>
                    string.Equals(p, request.UserId, StringComparison.OrdinalIgnoreCase)
                );

                if (!isParticipant)
                {
                    throw new UnauthorizedAccessException(
                        "User is not authorized to read messages in this conversation"
                    );
                }

                var result = await _messageRepository.MarkAllAsReadAsync(
                    request.ConversationId,
                    request.UserId,
                    DateTime.UtcNow
                );

                if (result)
                {
                    LogInfo(
                        "Đánh dấu tất cả tin nhắn đã đọc thành công cho conversation: {ConversationId}, user: {UserId}",
                        null,
                        request.ConversationId,
                        request.UserId
                    );

                    // Gửi thông báo real-time qua SignalR (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _signalRNotificationService.SendAllMessagesReadNotificationAsync(
                                request.ConversationId,
                                request.UserId
                            );
                        }
                        catch (Exception ex)
                        {
                            LogWarning(
                                "Error sending SignalR all-messages-read notification for conversation {ConversationId}: {Error}",
                                null,
                                request.ConversationId,
                                ex.Message
                            );
                        }
                    });
                }
                else
                {
                    LogInfo(
                        "No messages were marked as read (may already be read) for conversation: {ConversationId}, user: {UserId}",
                        null,
                        request.ConversationId,
                        request.UserId
                    );
                }

                return result;
            },
            "MarkAllMessagesAsRead"
        );
    }

    /// <summary>
    /// Lấy số tin nhắn chưa đọc trong một conversation
    /// </summary>
    public async Task<long> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _messageRepository.GetUnreadCountAsync(conversationId, userId);
    }

    /// <summary>
    /// Lấy tổng số tin nhắn chưa đọc của user (across all conversations)
    /// </summary>
    public async Task<long> GetTotalUnreadCountAsync(string userId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Getting total unread count for user: {UserId}", null, userId);

                // Get all conversations where user is a participant
                var conversations = await _conversationRepository.GetByUserIdAsync(userId);

                // Sum unread counts across all conversations
                long totalUnreadCount = 0;
                foreach (var conversation in conversations)
                {
                    var unreadCount = await _messageRepository.GetUnreadCountAsync(
                        conversation.Id,
                        userId
                    );
                    totalUnreadCount += unreadCount;
                }

                LogInfo(
                    "Total unread count for user {UserId}: {Count} across {ConversationCount} conversations",
                    null,
                    userId,
                    totalUnreadCount,
                    conversations.Count()
                );

                return totalUnreadCount;
            },
            "GetTotalUnreadCountAsync"
        );
    }

    /// <summary>
    /// Lấy unread count cho từng conversation của user (for displaying badges on conversation list)
    /// </summary>
    public async Task<Dictionary<string, long>> GetUnreadCountByConversationsAsync(string userId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Getting unread counts by conversations for user: {UserId}", null, userId);

                // Get all conversations where user is a participant
                var conversations = await _conversationRepository.GetByUserIdAsync(userId);

                // Build dictionary of conversationId -> unreadCount
                var unreadCountsByConversation = new Dictionary<string, long>();

                foreach (var conversation in conversations)
                {
                    var unreadCount = await _messageRepository.GetUnreadCountAsync(
                        conversation.Id,
                        userId
                    );
                    if (unreadCount > 0) // Only include conversations with unread messages
                    {
                        unreadCountsByConversation[conversation.Id] = unreadCount;
                    }
                }

                LogInfo(
                    "Found {Count} conversations with unread messages for user {UserId}",
                    null,
                    unreadCountsByConversation.Count,
                    userId
                );

                return unreadCountsByConversation;
            },
            "GetUnreadCountByConversationsAsync"
        );
    }

    /// <summary>
    /// Tìm kiếm tin nhắn
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Searching messages in conversation: {ConversationId} with term: {SearchTerm}",
                    null,
                    request.ConversationId,
                    request.SearchTerm
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
                ValidateRequiredString(request.SearchTerm, nameof(request.SearchTerm));

                var messages = await _messageRepository.SearchAsync(
                    request.ConversationId,
                    request.SearchTerm,
                    request.Page,
                    request.PageSize
                );

                LogInfo("Found {Count} messages", null, messages.Count());
                return _mapper.Map<IEnumerable<MessageResponse>>(messages);
            },
            "SearchMessages"
        );
    }

    /// <summary>
    /// Lấy tin nhắn theo loại (simplified implementation using existing methods)
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(
        String conversationId,
        MessageType messageType,
        int page = 1,
        int pageSize = 20
    )
    {
        // Lấy tất cả tin nhắn cho conversation và lọc theo loại trong bộ nhớ
        // Không tối ưu cho production nhưng tạm thời sử dụng
        var allMessages = await _messageRepository.GetByConversationIdAsync(
            conversationId,
            1,
            1000
        );
        var filteredMessages = allMessages
            .Where(m => m.Type == messageType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return _mapper.Map<IEnumerable<MessageResponse>>(filteredMessages);
    }

    /// <summary>
    /// Lấy tất cả file attachments trong conversation (simplified implementation)
    /// </summary>
    public async Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(
        string conversationId,
        MessageType? messageType = null,
        int page = 1,
        int pageSize = 50
    )
    {
        // Lấy tất cả tin nhắn và lọc những tin có attachments
        var allMessages = await _messageRepository.GetByConversationIdAsync(
            conversationId,
            1,
            1000
        );

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
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching mixed timeline for conversation: {ConversationId}, before: {Before}, after: {After}, limit: {Limit}",
                    null,
                    request.ConversationId,
                    request.Before,
                    request.After,
                    request.Limit
                );

                // Validation
                ValidateMixedTimelineRequest(request);

                // Parse cursors để lấy timestamp filter
                var (beforeTimestamp, afterTimestamp) = ParseTimelineCursors(
                    request.Before,
                    request.After
                );

                // Fetch timeline items (messages + call logs)
                var timelineItems = await FetchTimelineItemsAsync(
                    request,
                    beforeTimestamp,
                    afterTimestamp
                );

                // Sort and paginate items
                var sortedItems = SortAndLimitTimelineItems(timelineItems, request.Limit);

                // Determine pagination info
                var paginationInfo = CalculatePaginationInfo(
                    sortedItems,
                    request.Limit,
                    request.Before,
                    request.After
                );

                // Remove extra item if exists
                if (paginationInfo.HasNext)
                {
                    sortedItems.RemoveAt(sortedItems.Count - 1);
                }

                // Generate cursors
                var cursors = GenerateTimelineCursors(
                    sortedItems,
                    paginationInfo.HasNext,
                    paginationInfo.HasPrevious,
                    request.Before
                );

                var result = new MixedTimelineResponse
                {
                    Items = sortedItems,
                    NextCursor = cursors.NextCursor,
                    PreviousCursor = cursors.PreviousCursor,
                    HasNext = paginationInfo.HasNext,
                    HasPrevious = paginationInfo.HasPrevious,
                    Limit = request.Limit,
                };

                LogInfo(
                    "Successfully fetched mixed timeline with {Count} items for conversation: {ConversationId}",
                    null,
                    sortedItems.Count,
                    request.ConversationId
                );

                return result;
            },
            "GetMixedTimeline"
        );
    }

    /// <summary>
    /// Validate mixed timeline request
    /// </summary>
    private static void ValidateMixedTimelineRequest(GetMixedTimelineRequest request)
    {
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
    }

    /// <summary>
    /// Parse timeline cursors to get timestamp filters
    /// </summary>
    private static (DateTime? beforeTimestamp, DateTime? afterTimestamp) ParseTimelineCursors(
        string? before,
        string? after
    )
    {
        DateTime? beforeTimestamp = null;
        DateTime? afterTimestamp = null;

        if (!string.IsNullOrEmpty(before))
        {
            var (timestamp, _) = CursorHelper.ParseCursor(before);
            beforeTimestamp = timestamp;
        }

        if (!string.IsNullOrEmpty(after))
        {
            var (timestamp, _) = CursorHelper.ParseCursor(after);
            afterTimestamp = timestamp;
        }

        return (beforeTimestamp, afterTimestamp);
    }

    /// <summary>
    /// Fetch timeline items (messages and/or call logs)
    /// </summary>
    private async Task<List<TimelineItem>> FetchTimelineItemsAsync(
        GetMixedTimelineRequest request,
        DateTime? beforeTimestamp,
        DateTime? afterTimestamp
    )
    {
        var timelineItems = new List<TimelineItem>();

        // Lấy messages (nếu không chỉ lấy call logs)
        if (!request.CallLogsOnly)
        {
            var messageItems = await FetchMessageTimelineItemsAsync(
                request.ConversationId,
                beforeTimestamp,
                afterTimestamp,
                request.Limit + 50,
                request.MessageTypeFilter
            );

            timelineItems.AddRange(messageItems);
        }

        // Lấy call logs (nếu không chỉ lấy messages)
        if (!request.MessagesOnly)
        {
            var callLogItems = await FetchCallLogTimelineItemsAsync(
                request.ConversationId,
                beforeTimestamp,
                afterTimestamp,
                request.Limit + 50,
                request.CallTypeFilter
            );

            timelineItems.AddRange(callLogItems);
        }

        return timelineItems;
    }

    /// <summary>
    /// Fetch message timeline items
    /// </summary>
    private async Task<List<TimelineItem>> FetchMessageTimelineItemsAsync(
        string conversationId,
        DateTime? beforeTimestamp,
        DateTime? afterTimestamp,
        int limit,
        MessageType? messageTypeFilter
    )
    {
        var messages = await _messageRepository.GetByConversationIdForTimelineAsync(
            conversationId,
            beforeTimestamp,
            afterTimestamp,
            limit,
            messageTypeFilter
        );

        return messages
            .Select(m => new TimelineItem
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                CreatedAt = m.CreatedAt,
                ItemType = TimelineItemType.Message,
                Message = _mapper.Map<MessageResponse>(m),
                CallLog = null,
            })
            .ToList();
    }

    /// <summary>
    /// Fetch call log timeline items
    /// </summary>
    private async Task<List<TimelineItem>> FetchCallLogTimelineItemsAsync(
        string conversationId,
        DateTime? beforeTimestamp,
        DateTime? afterTimestamp,
        int limit,
        CallType? callTypeFilter
    )
    {
        var callLogs = await _callLogRepository.GetByConversationIdForTimelineAsync(
            conversationId,
            beforeTimestamp,
            afterTimestamp,
            limit,
            callTypeFilter
        );

        return callLogs
            .Select(c => new TimelineItem
            {
                Id = c.Id,
                ConversationId = c.ConversationId,
                CreatedAt = c.StartedAt,
                ItemType = TimelineItemType.CallLog,
                Message = null,
                CallLog = _mapper.Map<CallLogResponse>(c),
            })
            .ToList();
    }

    /// <summary>
    /// Sort and limit timeline items
    /// </summary>
    private static List<TimelineItem> SortAndLimitTimelineItems(
        List<TimelineItem> timelineItems,
        int limit
    )
    {
        return timelineItems
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(limit + 1) // +1 để check hasNext
            .ToList();
    }

    /// <summary>
    /// Calculate pagination info
    /// </summary>
    private static (bool HasNext, bool HasPrevious) CalculatePaginationInfo(
        List<TimelineItem> sortedItems,
        int limit,
        string? before,
        string? after
    )
    {
        var hasNext = sortedItems.Count > limit;
        var hasPrevious = !string.IsNullOrEmpty(before) || !string.IsNullOrEmpty(after);

        return (hasNext, hasPrevious);
    }

    /// <summary>
    /// Generate timeline cursors
    /// </summary>
    private static (string? NextCursor, string? PreviousCursor) GenerateTimelineCursors(
        List<TimelineItem> sortedItems,
        bool hasNext,
        bool hasPrevious,
        string? before
    )
    {
        if (!sortedItems.Any())
        {
            return (null, null);
        }

        string? nextCursor = null;
        string? previousCursor = null;

        if (hasNext)
        {
            var lastItem = sortedItems[sortedItems.Count - 1];
            nextCursor = CursorHelper.GenerateCursor(lastItem.Id, lastItem.CreatedAt);
        }

        if (hasPrevious || !string.IsNullOrEmpty(before))
        {
            var firstItem = sortedItems[0];
            previousCursor = CursorHelper.GenerateCursor(firstItem.Id, firstItem.CreatedAt);
        }

        return (nextCursor, previousCursor);
    }

    /// <summary>
    /// 🎯 NEW: Lấy mixed timeline với user info enrichment
    /// </summary>
    public async Task<MixedTimelineResponse> GetMixedTimelineWithUserInfoAsync(
        GetMixedTimelineRequest request,
        MessageLoadOptions? options = null
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching mixed timeline with user info for conversation: {ConversationId}, options: {Options}",
                    null,
                    request.ConversationId,
                    options?.IncludeSenderInfo
                );

                // Lấy mixed timeline bình thường
                var result = await GetMixedTimelineAsync(request);

                // Apply user info enrichment nếu options provided
                if (options != null && (options.IncludeSenderInfo || options.IncludeReceiverInfo))
                {
                    // Extract messages from timeline items
                    var messages = result
                        .Items.Where(item =>
                            item.ItemType == TimelineItemType.Message && item.Message != null
                        )
                        .Select(item => item.Message!)
                        .ToList();

                    if (messages.Any())
                    {
                        await EnrichMessageUserInfoAsync(messages, options);

                        // Count enrichment statistics
                        var messagesWithSenderInfo = messages.Count(m => m.SenderInfo != null);
                        var messagesWithReceiverInfo = messages.Count(m => m.ReceiverInfo != null);
                        var totalUsersEnriched = messages
                            .SelectMany(m => new[] { m.SenderInfo, m.ReceiverInfo })
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
                            MessagesWithReceiverInfo = messagesWithReceiverInfo,
                        };

                        LogInfo(
                            "Enriched mixed timeline with {TotalUsers} users, {SenderCount} sender info, {ReceiverCount} receiver info",
                            null,
                            totalUsersEnriched,
                            messagesWithSenderInfo,
                            messagesWithReceiverInfo
                        );
                    }
                }

                return result;
            },
            "GetMixedTimelineWithUserInfo"
        );
    }

    #region Private Helper Methods

    /// <summary>
    /// Enrichment user info cho danh sách messages
    /// </summary>
    private async Task EnrichMessageUserInfoAsync(
        IEnumerable<MessageResponse> messages,
        MessageLoadOptions options
    )
    {
        var messageList = messages.ToList();
        if (!messageList.Any())
            return;

        LogDebug("Starting enrichment of user info for {Count} messages", null, messageList.Count);

        // Collect unique user IDs cần enrichment
        var userIds = CollectUserIdsForEnrichment(messageList, options);

        if (!userIds.Any())
        {
            LogDebug("No user IDs to enrich", null);
            return;
        }

        LogDebug(
            "Enriching user info for {Count} unique users: {UserIds}",
            null,
            userIds.Count,
            string.Join(", ", userIds.Take(5))
        );

        try
        {
            // Lấy user details từ ParticipantEnrichmentService
            var userDetails = await _participantEnrichmentService.GetAccountDetailsAsync(userIds);

            // Apply user info cho từng message
            ApplyUserInfoToMessages(messageList, userDetails, options);

            LogEnrichmentCompletion(messageList);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error enriching user info for messages", null);
            // Continue without user enrichment
        }
    }

    /// <summary>
    /// Collect unique user IDs cần enrichment
    /// </summary>
    private static HashSet<string> CollectUserIdsForEnrichment(
        List<MessageResponse> messageList,
        MessageLoadOptions options
    )
    {
        var userIds = new HashSet<string>();

        if (options.IncludeSenderInfo)
        {
            CollectSenderIds(messageList, userIds);
        }

        if (options.IncludeReceiverInfo)
        {
            CollectReceiverIds(messageList, userIds);
        }

        return userIds;
    }

    /// <summary>
    /// Collect sender IDs từ messages
    /// </summary>
    private static void CollectSenderIds(List<MessageResponse> messageList, HashSet<string> userIds)
    {
        var senderIds = messageList
            .Where(m => !string.IsNullOrEmpty(m.SenderId))
            .Select(m => m.SenderId.ToLowerInvariant());

        userIds.UnionWith(senderIds);
    }

    /// <summary>
    /// Collect receiver IDs từ messages
    /// </summary>
    private static void CollectReceiverIds(
        List<MessageResponse> messageList,
        HashSet<string> userIds
    )
    {
        var receiverIds = messageList
            .Where(m => !string.IsNullOrEmpty(m.ReceiverId))
            .Select(m => m.ReceiverId.ToLowerInvariant());

        userIds.UnionWith(receiverIds);
    }

    /// <summary>
    /// Apply user info cho từng message
    /// </summary>
    private static void ApplyUserInfoToMessages(
        List<MessageResponse> messageList,
        Dictionary<string, ConversationParticipant> userDetails,
        MessageLoadOptions options
    )
    {
        foreach (var message in messageList)
        {
            // Enrich sender info
            if (options.IncludeSenderInfo)
            {
                EnrichSenderInfo(message, userDetails);
            }

            // Enrich receiver info
            if (options.IncludeReceiverInfo)
            {
                EnrichReceiverInfo(message, userDetails);
            }
        }
    }

    /// <summary>
    /// Enrich sender info cho message
    /// </summary>
    private static void EnrichSenderInfo(
        MessageResponse message,
        Dictionary<string, ConversationParticipant> userDetails
    )
    {
        if (string.IsNullOrEmpty(message.SenderId))
            return;

        var normalizedSenderId = message.SenderId.ToLowerInvariant();
        if (userDetails.TryGetValue(normalizedSenderId, out var senderDetail))
        {
            message.SenderInfo = CreateMessageSenderInfo(senderDetail);
        }
    }

    /// <summary>
    /// Enrich receiver info cho message
    /// </summary>
    private static void EnrichReceiverInfo(
        MessageResponse message,
        Dictionary<string, ConversationParticipant> userDetails
    )
    {
        if (string.IsNullOrEmpty(message.ReceiverId))
            return;

        var normalizedReceiverId = message.ReceiverId.ToLowerInvariant();
        if (userDetails.TryGetValue(normalizedReceiverId, out var receiverDetail))
        {
            message.ReceiverInfo = CreateMessageSenderInfo(receiverDetail);
        }
    }

    /// <summary>
    /// Create MessageSenderInfo từ ConversationParticipant
    /// </summary>
    private static MessageSenderInfo CreateMessageSenderInfo(ConversationParticipant participant)
    {
        return new MessageSenderInfo
        {
            Id = participant.Id,
            FullName = participant.FullName,
            Email = participant.Email,
            AvatarUrl = participant.AvatarUrl,
            Role = participant.Role,
            IsOnline = participant.IsOnline,
        };
    }

    /// <summary>
    /// Log enrichment completion statistics
    /// </summary>
    private void LogEnrichmentCompletion(List<MessageResponse> messageList)
    {
        var enrichedSenders = messageList.Count(m => m.SenderInfo != null);
        var enrichedReceivers = messageList.Count(m => m.ReceiverInfo != null);

        LogDebug(
            "Completed enrichment of user info: {Senders} sender info, {Receivers} receiver info",
            null,
            enrichedSenders,
            enrichedReceivers
        );
    }

    #endregion

    #region Message Recall

    /// <summary>
    /// Thu hồi tin nhắn - chỉ cho phép trong 1 giờ sau khi gửi và phải là người gửi
    /// </summary>
    public async Task<MessageResponse?> RecallMessageAsync(RecallMessageRequest request)
    {
        LogInfo(
            "Starting recall of message: {MessageId} by user: {UserId}",
            null,
            request.MessageId ?? string.Empty,
            request.UserId ?? string.Empty
        );

        // Validation
        ValidateRequired(request, nameof(request));
        ValidateRequiredString(request.MessageId, nameof(request.MessageId));
        ValidateRequiredString(request.UserId, nameof(request.UserId));

        // Get message
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
        {
            throw new ArgumentException($"Tin nhắn với ID {request.MessageId} không tồn tại");
        }

        // Check if user is the sender (case-insensitive)
        if (!string.Equals(message.SenderId, request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Bạn không thể thu hồi tin nhắn của người khác");
        }

        // Check if message was sent within 1 hour
        var hoursSinceSent = (DateTime.UtcNow - message.CreatedAt).TotalHours;
        if (hoursSinceSent > 1)
        {
            throw new InvalidOperationException(
                "Chỉ có thể thu hồi tin nhắn trong vòng 1 giờ sau khi gửi"
            );
        }

        // Check if message is already recalled
        if (message.Status == MessageStatus.RECALLED)
        {
            throw new InvalidOperationException("Tin nhắn này đã được thu hồi trước đó");
        }

        // Update message content and status
        message.Content = "Tin nhắn đã được thu hồi";
        message.Status = MessageStatus.RECALLED;
        message.UpdatedAt = DateTime.UtcNow;

        // Save to database
        var updatedMessage = await _messageRepository.UpdateAsync(message);

        // Map to response
        var messageResponse = _mapper.Map<MessageResponse>(updatedMessage);

        // Send real-time notification via SignalR
        try
        {
            await _signalRNotificationService.SendMessageRecalledNotificationAsync(
                message.ConversationId,
                messageResponse
            );
            LogInfo(
                "Successfully sent recall notification for message: {MessageId}",
                null,
                messageResponse.Id
            );
        }
        catch (Exception ex)
        {
            LogWarning(
                "Failed to send SignalR notification for recalled message: {MessageId}. Error: {Error}",
                null,
                messageResponse.Id,
                ex.Message
            );
            // Don't throw - message is already updated, notification is optional
        }

        // Update conversation's LastMessage if this was the last message
        var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId);
        if (conversation?.LastMessage != null && conversation.LastMessage.MessageId == message.Id)
        {
            conversation.LastMessage.Content = "Tin nhắn đã được thu hồi";
            await _conversationRepository.UpdateAsync(conversation);
        }

        LogInfo(
            "Successfully recalled message: {MessageId}",
            null,
            request.MessageId ?? string.Empty
        );

        return messageResponse;
    }

    #endregion
}
