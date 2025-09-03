using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation c?a Conversation service v?i BaseService
/// </summary>
public class ConversationService : BaseService, IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public ConversationService(
        IConversationRepository conversationRepository,
        IMapper mapper,
        ILogger<ConversationService> logger) : base(logger)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// T?o cu?c h?i tho?i m?i
    /// </summary>
    public async Task<ConversationResponse> CreateAsync(CreateConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u t?o cu?c h?i tho?i v?i {Count} thành viên", null, request.Participants.Count);

            // Validation
            ValidateRequired(request, nameof(request));
            if (request.Participants == null || request.Participants.Count < 2)
            {
                throw new ArgumentException("Cu?c h?i tho?i ph?i có ít nh?t 2 thành viên");
            }

            // Ki?m tra xem conversation gi?a 2 user ?ã t?n t?i ch?a (n?u là chat 1-1)
            if (request.Participants.Count == 2)
            {
                var existingConversation = await _conversationRepository
                    .GetConversationBetweenUsersAsync(request.Participants[0], request.Participants[1]);
                
                if (existingConversation != null)
                {
                    LogInfo("Cu?c h?i tho?i gi?a 2 user ?ã t?n t?i: {ConversationId}", null, existingConversation.Id);
                    return _mapper.Map<ConversationResponse>(existingConversation);
                }
            }

            // T?o conversation m?i
            var conversationEntity = _mapper.Map<ConversationEntity>(request);
            var createdConversation = await _conversationRepository.CreateAsync(conversationEntity);

            LogInfo("T?o cu?c h?i tho?i thành công v?i ID: {ConversationId}", null, createdConversation.Id);
            return _mapper.Map<ConversationResponse>(createdConversation);
        }, "CreateConversation");
    }

    /// <summary>
    /// L?y cu?c h?i tho?i theo ID
    /// </summary>
    public async Task<ConversationResponse?> GetByIdAsync(string id)
    {
        var conversation = await _conversationRepository.GetByIdAsync(id);
        return conversation != null ? _mapper.Map<ConversationResponse>(conversation) : null;
    }

    /// <summary>
    /// L?y danh sách cu?c h?i tho?i c?a user
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(userId, page, pageSize);
        return _mapper.Map<IEnumerable<ConversationResponse>>(conversations);
    }

    /// <summary>
    /// Tìm cu?c h?i tho?i gi?a 2 ng??i dùng
    /// </summary>
    public async Task<ConversationResponse?> GetConversationBetweenUsersAsync(string userId1, string userId2)
    {
        var conversation = await _conversationRepository.GetConversationBetweenUsersAsync(userId1, userId2);
        return conversation != null ? _mapper.Map<ConversationResponse>(conversation) : null;
    }

    /// <summary>
    /// Xóa cu?c h?i tho?i
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u xóa cu?c h?i tho?i: {ConversationId}", null, id);

            ValidateRequiredString(id, nameof(id));

            var result = await _conversationRepository.DeleteAsync(id);
            if (result)
            {
                LogInfo("Xóa cu?c h?i tho?i thành công: {ConversationId}", null, id);
            }
            else
            {
                LogWarning("Không th? xóa cu?c h?i tho?i: {ConversationId}", null, id);
            }

            return result;
        }, "DeleteConversation");
    }

    /// <summary>
    /// C?p nh?t tin nh?n cu?i cùng
    /// </summary>
    public async Task<bool> UpdateLastMessageAsync(string conversationId, string messageId, string content, string senderId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("C?p nh?t tin nh?n cu?i cho conversation: {ConversationId}", null, conversationId);

            ValidateRequiredString(conversationId, nameof(conversationId));
            ValidateRequiredString(messageId, nameof(messageId));
            ValidateRequiredString(content, nameof(content));
            ValidateRequiredString(senderId, nameof(senderId));

            var lastMessage = new LastMessage
            {
                MessageId = messageId,
                Content = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                SenderId = senderId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _conversationRepository.UpdateLastMessageAsync(conversationId, lastMessage);
            
            if (result)
            {
                LogInfo("C?p nh?t tin nh?n cu?i thành công cho conversation: {ConversationId}", null, conversationId);
            }

            return result;
        }, "UpdateLastMessage");
    }

    /// <summary>
    /// Ch?n cu?c h?i tho?i
    /// </summary>
    public async Task<bool> BlockConversationAsync(BlockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Ch?n cu?c h?i tho?i: {ConversationId} b?i user: {UserId}", null, request.ConversationId, request.BlockedBy);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.BlockedBy, nameof(request.BlockedBy));

            var result = await _conversationRepository.BlockConversationAsync(request.ConversationId, request.BlockedBy);
            
            if (result)
            {
                LogInfo("Ch?n cu?c h?i tho?i thành công: {ConversationId}", null, request.ConversationId);
            }

            return result;
        }, "BlockConversation");
    }

    /// <summary>
    /// B? ch?n cu?c h?i tho?i
    /// </summary>
    public async Task<bool> UnblockConversationAsync(UnblockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B? ch?n cu?c h?i tho?i: {ConversationId}", null, request.ConversationId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));

            var result = await _conversationRepository.UnblockConversationAsync(request.ConversationId);
            
            if (result)
            {
                LogInfo("B? ch?n cu?c h?i tho?i thành công: {ConversationId}", null, request.ConversationId);
            }

            return result;
        }, "UnblockConversation");
    }

    /// <summary>
    /// Ki?m tra cu?c h?i tho?i có b? ch?n không
    /// </summary>
    public async Task<bool> IsConversationBlockedAsync(string conversationId)
    {
        return await _conversationRepository.IsConversationBlockedAsync(conversationId);
    }
}