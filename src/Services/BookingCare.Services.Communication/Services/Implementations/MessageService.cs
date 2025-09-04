using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Triển khai Message service với BaseService và tích hợp File Upload
/// </summary>
public class MessageService : BaseService, IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IMapper _mapper;

    public MessageService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IFileUploadService fileUploadService,
        ISignalRNotificationService signalRNotificationService,
        IMapper mapper,
        ILogger<MessageService> logger) : base(logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _fileUploadService = fileUploadService;
        _signalRNotificationService = signalRNotificationService;
        _mapper = mapper;
    }

    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    public async Task<MessageResponse> CreateAsync(CreateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu tạo tin nhắn cho conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SenderId, nameof(request.SenderId));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // Kiểm tra conversation có tồn tại không
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation với ID {request.ConversationId} không tồn tại");
            }

            // Kiểm tra user có trong conversation không
            if (!conversation.Participants.Contains(request.SenderId))
            {
                throw new UnauthorizedAccessException("User không có quyền gửi tin nhắn trong conversation này");
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
                    LogWarning("Lỗi khi gửi thông báo SignalR cho tin nhắn {MessageId}: {Error}", 
                        null, result.Id, ex.Message);
                }
            });

            LogInfo("Tạo tin nhắn thành công với ID: {MessageId}", null, createdMessage.Id);
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
            LogInfo("Bắt đầu tạo tin nhắn với files cho conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SenderId, nameof(request.SenderId));

            if (request.Type == MessageType.Text && request.Files.Any())
            {
                throw new ArgumentException("Tin nhắn text không được có files");
            }

            if (request.Type != MessageType.Text && !request.Files.Any())
            {
                throw new ArgumentException($"Tin nhắn loại {request.Type} yêu cầu phải có files");
            }

            // Kiểm tra conversation
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation với ID {request.ConversationId} không tồn tại");
            }

            if (!conversation.Participants.Contains(request.SenderId))
            {
                throw new UnauthorizedAccessException("User không có quyền gửi tin nhắn trong conversation này");
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

            // Tạo message entity
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
                Content = attachments.Any() ? $"Đã gửi {attachments.Count} file(s)" : createdMessage.Content,
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
                    LogWarning("Lỗi khi gửi thông báo SignalR cho tin nhắn với files {MessageId}: {Error}", 
                        null, result.Id, ex.Message);
                }
            });

            LogInfo("Tạo tin nhắn với files thành công với ID: {MessageId}", null, createdMessage.Id);
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
            LogInfo("Bắt đầu cập nhật tin nhắn với ID: {MessageId}", null, request.Id);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Id, nameof(request.Id));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // Lấy tin nhắn hiện tại
            var existingMessage = await _messageRepository.GetByIdAsync(request.Id);
            if (existingMessage == null)
            {
                throw new ArgumentException($"Tin nhắn với ID {request.Id} không tồn tại");
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

            LogInfo("Cập nhật tin nhắn thành công với ID: {MessageId}", null, updatedMessage.Id);
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
    /// Xóa tin nhắn
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu xóa tin nhắn với ID: {MessageId}", null, id);

            // Lấy tin nhắn để xóa attachments
            var message = await _messageRepository.GetByIdAsync(id);
            if (message != null && message.Attachments.Any())
            {
                // Xóa files từ cloud storage
                foreach (var attachment in message.Attachments)
                {
                    try
                    {
                        await _fileUploadService.DeleteFileAsync(attachment.Url);
                    }
                    catch (Exception ex)
                    {
                        LogWarning("Không thể xóa attachment {Url}: {Error}", null, attachment.Url, ex.Message);
                    }
                }
            }

            var result = await _messageRepository.DeleteAsync(id);
            
            if (result)
            {
                LogInfo("Xóa tin nhắn thành công với ID: {MessageId}", null, id);
            }
            else
            {
                LogWarning("Không thể xóa tin nhắn với ID: {MessageId}", null, id);
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
            LogInfo("Đánh dấu tin nhắn đã đọc: {MessageId}", null, request.MessageId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.MessageId, nameof(request.MessageId));

            // Get message để lấy conversation ID
            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
            {
                throw new ArgumentException($"Tin nhắn với ID {request.MessageId} không tồn tại");
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
                        LogWarning("Lỗi khi gửi thông báo đọc tin nhắn SignalR cho {MessageId}: {Error}", 
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
            LogInfo("Đánh dấu tất cả tin nhắn đã đọc cho conversation: {ConversationId}, user: {UserId}", 
                null, request.ConversationId, request.UserId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.UserId, nameof(request.UserId));

            // Kiểm tra conversation có tồn tại không
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation với ID {request.ConversationId} không tồn tại");
            }

            // Kiểm tra user có trong conversation không
            if (!conversation.Participants.Contains(request.UserId))
            {
                throw new UnauthorizedAccessException("User không có quyền đọc tin nhắn trong conversation này");
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
                        LogWarning("Lỗi khi gửi thông báo đọc tất cả tin nhắn SignalR cho conversation {ConversationId}: {Error}", 
                            null, request.ConversationId, ex.Message);
                    }
                });
            }
            else
            {
                LogInfo("Không có tin nhắn nào được đánh dấu là đã đọc (có thể đã đọc hết) cho conversation: {ConversationId}, user: {UserId}", 
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
            LogInfo("Tìm kiếm tin nhắn trong conversation: {ConversationId} với từ khóa: {SearchTerm}", 
                null, request.ConversationId, request.SearchTerm);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SearchTerm, nameof(request.SearchTerm));

            var messages = await _messageRepository.SearchAsync(request.ConversationId, request.SearchTerm, request.Page, request.PageSize);

            LogInfo("Tìm thấy {Count} tin nhắn", null, messages.Count());
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
}