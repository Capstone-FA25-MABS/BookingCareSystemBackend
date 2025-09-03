using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation c?a Message service v?i BaseService
/// </summary>
public class MessageService : BaseService, IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public MessageService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper,
        ILogger<MessageService> logger) : base(logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// T?o tin nh?n m?i
    /// </summary>
    public async Task<MessageResponse> CreateAsync(CreateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u t?o tin nh?n cho conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SenderId, nameof(request.SenderId));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // Ki?m tra conversation có t?n t?i không
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation v?i ID {request.ConversationId} không t?n t?i");
            }

            // Ki?m tra user có trong conversation không
            if (!conversation.Participants.Contains(request.SenderId))
            {
                throw new UnauthorizedAccessException("User không có quy?n g?i tin nh?n trong conversation này");
            }

            // T?o entity t? request
            var messageEntity = _mapper.Map<MessageEntity>(request);
            var createdMessage = await _messageRepository.CreateAsync(messageEntity);

            // C?p nh?t last message cho conversation
            var lastMessage = new LastMessage
            {
                MessageId = createdMessage.Id,
                Content = createdMessage.Content.Length > 100 ? 
                    createdMessage.Content.Substring(0, 100) + "..." : createdMessage.Content,
                SenderId = createdMessage.SenderId,
                CreatedAt = createdMessage.CreatedAt
            };
            await _conversationRepository.UpdateLastMessageAsync(request.ConversationId, lastMessage);

            LogInfo("T?o tin nh?n thành công v?i ID: {MessageId}", null, createdMessage.Id);
            return _mapper.Map<MessageResponse>(createdMessage);
        }, "CreateMessage");
    }

    /// <summary>
    /// C?p nh?t tin nh?n
    /// </summary>
    public async Task<MessageResponse> UpdateAsync(UpdateMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u c?p nh?t tin nh?n v?i ID: {MessageId}", null, request.Id);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Id, nameof(request.Id));
            ValidateRequiredString(request.Content, nameof(request.Content));

            // L?y tin nh?n hi?n t?i
            var existingMessage = await _messageRepository.GetByIdAsync(request.Id);
            if (existingMessage == null)
            {
                throw new ArgumentException($"Tin nh?n v?i ID {request.Id} không t?n t?i");
            }

            // C?p nh?t thông tin
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

            LogInfo("C?p nh?t tin nh?n thành công v?i ID: {MessageId}", null, updatedMessage.Id);
            return _mapper.Map<MessageResponse>(updatedMessage);
        }, "UpdateMessage");
    }

    /// <summary>
    /// L?y tin nh?n theo ID
    /// </summary>
    public async Task<MessageResponse?> GetByIdAsync(string id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        return message != null ? _mapper.Map<MessageResponse>(message) : null;
    }

    /// <summary>
    /// L?y danh sách tin nh?n theo conversation ID
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50)
    {
        var messages = await _messageRepository.GetByConversationIdAsync(conversationId, page, pageSize);
        return _mapper.Map<IEnumerable<MessageResponse>>(messages);
    }

    /// <summary>
    /// Xóa tin nh?n
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u xóa tin nh?n v?i ID: {MessageId}", null, id);

            var result = await _messageRepository.DeleteAsync(id);
            
            if (result)
            {
                LogInfo("Xóa tin nh?n thành công v?i ID: {MessageId}", null, id);
            }
            else
            {
                LogWarning("Không th? xóa tin nh?n v?i ID: {MessageId}", null, id);
            }

            return result;
        }, "DeleteMessage");
    }

    /// <summary>
    /// ?ánh d?u tin nh?n ?ã ??c
    /// </summary>
    public async Task<bool> MarkAsReadAsync(MarkMessageAsReadRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ánh d?u tin nh?n ?ã ??c: {MessageId}", null, request.MessageId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.MessageId, nameof(request.MessageId));

            var result = await _messageRepository.MarkAsReadAsync(request.MessageId, DateTime.UtcNow);

            if (result)
            {
                LogInfo("?ánh d?u tin nh?n ?ã ??c thành công: {MessageId}", null, request.MessageId);
            }

            return result;
        }, "MarkMessageAsRead");
    }

    /// <summary>
    /// L?y s? tin nh?n ch?a ??c
    /// </summary>
    public async Task<long> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _messageRepository.GetUnreadCountAsync(conversationId, userId);
    }

    /// <summary>
    /// Tìm ki?m tin nh?n
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> SearchAsync(SearchMessageRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Tìm ki?m tin nh?n trong conversation: {ConversationId} v?i t? khóa: {SearchTerm}", 
                null, request.ConversationId, request.SearchTerm);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.SearchTerm, nameof(request.SearchTerm));

            var messages = await _messageRepository.SearchAsync(request.ConversationId, request.SearchTerm, request.Page, request.PageSize);

            LogInfo("Tìm th?y {Count} tin nh?n", null, messages.Count());
            return _mapper.Map<IEnumerable<MessageResponse>>(messages);
        }, "SearchMessages");
    }

    /// <summary>
    /// L?y tin nh?n theo lo?i
    /// </summary>
    public async Task<IEnumerable<MessageResponse>> GetMessagesByTypeAsync(string conversationId, MessageType messageType, int page = 1, int pageSize = 20)
    {
        // Get all messages for the conversation and filter by type in memory
        // This is not optimal for production but works for now
        var allMessages = await _messageRepository.GetByConversationIdAsync(conversationId, 1, 1000);
        var filteredMessages = allMessages
            .Where(m => m.Type == messageType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return _mapper.Map<IEnumerable<MessageResponse>>(filteredMessages);
    }

    /// <summary>
    /// L?y t?t c? file attachments trong conversation
    /// </summary>
    public async Task<IEnumerable<MessageAttachmentResponse>> GetConversationAttachmentsAsync(string conversationId, MessageType? messageType = null, int page = 1, int pageSize = 50)
    {
        // Get all messages and filter those with attachments
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