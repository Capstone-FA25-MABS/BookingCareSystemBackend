using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation của Conversation service với BaseService và Lazy Loading support
/// </summary>
public class ConversationService : BaseService, IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;

    public ConversationService(
        IConversationRepository conversationRepository,
        IMessageService messageService,
        IMapper mapper,
        ILogger<ConversationService> logger) : base(logger)
    {
        _conversationRepository = conversationRepository;
        _messageService = messageService;
        _mapper = mapper;
    }

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    public async Task<ConversationResponse> CreateAsync(CreateConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu tạo cuộc hội thoại với {Count} thành viên", null, request.Participants.Count);

            // Validation
            ValidateRequired(request, nameof(request));
            if (request.Participants == null || request.Participants.Count < 2)
            {
                throw new ArgumentException("Cuộc hội thoại phải có ít nhất 2 thành viên");
            }

            // Kiểm tra xem conversation giữa 2 user đã tồn tại chưa (nếu là chat 1-1)
            if (request.Participants.Count == 2)
            {
                var existingConversation = await _conversationRepository
                    .GetConversationBetweenUsersAsync(request.Participants[0], request.Participants[1]);
                
                if (existingConversation != null)
                {
                    LogInfo("Cuộc hội thoại giữa 2 user đã tồn tại: {ConversationId}", null, existingConversation.Id);
                    return _mapper.Map<ConversationResponse>(existingConversation);
                }
            }

            // Tạo conversation mới
            var conversationEntity = _mapper.Map<ConversationEntity>(request);
            var createdConversation = await _conversationRepository.CreateAsync(conversationEntity);

            LogInfo("Tạo cuộc hội thoại thành công với ID: {ConversationId}", null, createdConversation.Id);
            return _mapper.Map<ConversationResponse>(createdConversation);
        }, "CreateConversation");
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    public async Task<ConversationResponse?> GetByIdAsync(string id)
    {
        var conversation = await _conversationRepository.GetByIdAsync(id);
        return conversation != null ? _mapper.Map<ConversationResponse>(conversation) : null;
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user (legacy method without lazy loading)
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(userId, page, pageSize);
        return _mapper.Map<IEnumerable<ConversationResponse>>(conversations);
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với lazy loading support
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20, ConversationLoadOptions? options = null)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Lấy conversations cho user: {UserId}, page: {Page}, pageSize: {PageSize} với lazy loading", null, userId, page, pageSize);

            var conversations = await _conversationRepository.GetByUserIdAsync(userId, page, pageSize);
            var result = _mapper.Map<IEnumerable<ConversationResponse>>(conversations).ToList();

            if (options != null)
            {
                // Lazy load additional data based on options
                await LoadConversationDataAsync(result, userId, options);
            }

            LogInfo("Lấy thành công {Count} conversations cho user: {UserId}", null, result.Count, userId);
            return result;
        }, "GetConversationsByUserIdWithLazyLoading");
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại lightweight
    /// </summary>
    public async Task<IEnumerable<ConversationListResponse>> GetConversationsLightweightAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Lấy conversations lightweight cho user: {UserId}", null, userId);

            var conversations = await _conversationRepository.GetByUserIdAsync(userId, page, pageSize);
            var result = conversations.Select(c => new ConversationListResponse
            {
                Id = c.Id,
                Participants = c.Participants,
                LastMessage = _mapper.Map<LastMessageResponse>(c.LastMessage),
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive,
                IsBlocked = c.Blocked != null,
                UnreadCount = 0 // Will be loaded separately if needed
            }).ToList();

            LogInfo("Lấy thành công {Count} conversations lightweight cho user: {UserId}", null, result.Count, userId);
            return result;
        }, "GetConversationsLightweight");
    }

    /// <summary>
    /// Lấy chi tiết conversation với lazy loading
    /// </summary>
    public async Task<ConversationResponse?> GetConversationDetailsAsync(string id, ConversationLoadOptions? options = null)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Lấy chi tiết conversation: {ConversationId}", null, id);

            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null) return null;

            var result = _mapper.Map<ConversationResponse>(conversation);

            if (options != null)
            {
                await LoadConversationDataAsync(new[] { result }, "", options);
            }

            LogInfo("Lấy chi tiết conversation thành công: {ConversationId}", null, id);
            return result;
        }, "GetConversationDetails");
    }

    /// <summary>
    /// Helper method để load dữ liệu lazy loading
    /// </summary>
    private async Task LoadConversationDataAsync(IEnumerable<ConversationResponse> conversations, string currentUserId, ConversationLoadOptions options)
    {
        var conversationList = conversations.ToList();

        // Load unread count
        if (options.IncludeUnreadCount && !string.IsNullOrEmpty(currentUserId))
        {
            foreach (var conversation in conversationList)
            {
                try
                {
                    conversation.UnreadCount = await _messageService.GetUnreadCountAsync(conversation.Id, currentUserId);
                    LogDebug("Loaded unread count {Count} for conversation {ConversationId}", null, conversation.UnreadCount, conversation.Id);
                }
                catch (Exception ex)
                {
                    LogWarning("Lỗi khi load unread count cho conversation {ConversationId}: {Error}", null, conversation.Id, ex.Message);
                    conversation.UnreadCount = 0;
                }
            }
        }

        // Load recent messages
        if (options.IncludeRecentMessages)
        {
            foreach (var conversation in conversationList)
            {
                try
                {
                    var recentMessages = await _messageService.GetByConversationIdAsync(conversation.Id, 1, options.RecentMessagesCount);
                    conversation.RecentMessages = recentMessages.ToList();
                    LogDebug("Loaded {Count} recent messages for conversation {ConversationId}", null, conversation.RecentMessages.Count, conversation.Id);
                }
                catch (Exception ex)
                {
                    LogWarning("Lỗi khi load recent messages cho conversation {ConversationId}: {Error}", null, conversation.Id, ex.Message);
                    conversation.RecentMessages = new List<MessageResponse>();
                }
            }
        }

        // Load participant details (placeholder - would require User Service integration)
        if (options.IncludeParticipantDetails)
        {
            foreach (var conversation in conversationList)
            {
                try
                {
                    // Initialize empty list for now
                    conversation.ParticipantDetails = new List<ConversationParticipant>();
                    
                    // TODO: Implement User Service integration
                    // foreach (var participantId in conversation.Participants)
                    // {
                    //     var userDetails = await _userService.GetUserByIdAsync(participantId);
                    //     if (userDetails != null)
                    //     {
                    //         conversation.ParticipantDetails.Add(new ConversationParticipant
                    //         {
                    //             Id = userDetails.Id,
                    //             Name = userDetails.Name,
                    //             Avatar = userDetails.Avatar,
                    //             IsOnline = options.IncludeOnlineStatus ? await _presenceService.IsUserOnlineAsync(participantId) : false
                    //         });
                    //     }
                    // }
                    
                    LogDebug("Participant details placeholder loaded for conversation {ConversationId}", null, conversation.Id);
                }
                catch (Exception ex)
                {
                    LogWarning("Lỗi khi load participant details cho conversation {ConversationId}: {Error}", null, conversation.Id, ex.Message);
                    conversation.ParticipantDetails = new List<ConversationParticipant>();
                }
            }
        }

        // Load metadata
        if (options.IncludeMetadata)
        {
            foreach (var conversation in conversationList)
            {
                try
                {
                    // Initialize basic metadata
                    conversation.Metadata = new ConversationMetadata
                    {
                        TotalMessages = 0, // TODO: Implement repository method
                        TotalFiles = 0,    // TODO: Implement repository method
                        TotalImages = 0,   // TODO: Implement repository method
                        FirstMessageDate = null, // TODO: Implement repository method
                        CommonFiles = new List<string>()
                    };

                    // Basic implementation: get total messages from recent messages query
                    if (conversation.RecentMessages != null)
                    {
                        conversation.Metadata.TotalMessages = conversation.RecentMessages.Count;
                        conversation.Metadata.FirstMessageDate = conversation.RecentMessages
                            .OrderBy(m => m.CreatedAt)
                            .FirstOrDefault()?.CreatedAt;
                    }

                    LogDebug("Metadata loaded for conversation {ConversationId}", null, conversation.Id);
                }
                catch (Exception ex)
                {
                    LogWarning("Lỗi khi load metadata cho conversation {ConversationId}: {Error}", null, conversation.Id, ex.Message);
                    conversation.Metadata = new ConversationMetadata();
                }
            }
        }
    }

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 người dùng
    /// </summary>
    public async Task<ConversationResponse?> GetConversationBetweenUsersAsync(string userId1, string userId2)
    {
        var conversation = await _conversationRepository.GetConversationBetweenUsersAsync(userId1, userId2);
        return conversation != null ? _mapper.Map<ConversationResponse>(conversation) : null;
    }

    /// <summary>
    /// Xóa cuộc hội thoại
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bắt đầu xóa cuộc hội thoại: {ConversationId}", null, id);

            ValidateRequiredString(id, nameof(id));

            var result = await _conversationRepository.DeleteAsync(id);
            if (result)
            {
                LogInfo("Xóa cuộc hội thoại thành công: {ConversationId}", null, id);
            }
            else
            {
                LogWarning("Không thể xóa cuộc hội thoại: {ConversationId}", null, id);
            }

            return result;
        }, "DeleteConversation");
    }

    /// <summary>
    /// Cập nhật tin nhắn cuối cùng
    /// </summary>
    public async Task<bool> UpdateLastMessageAsync(string conversationId, string messageId, string content, string senderId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Cập nhật tin nhắn cuối cho conversation: {ConversationId}", null, conversationId);

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
                LogInfo("Cập nhật tin nhắn cuối thành công cho conversation: {ConversationId}", null, conversationId);
            }

            return result;
        }, "UpdateLastMessage");
    }

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> BlockConversationAsync(BlockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Chặn cuộc hội thoại: {ConversationId} bởi user: {UserId}", null, request.ConversationId, request.BlockedBy);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.BlockedBy, nameof(request.BlockedBy));

            var result = await _conversationRepository.BlockConversationAsync(request.ConversationId, request.BlockedBy);
            
            if (result)
            {
                LogInfo("Chặn cuộc hội thoại thành công: {ConversationId}", null, request.ConversationId);
            }

            return result;
        }, "BlockConversation");
    }

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> UnblockConversationAsync(UnblockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Bỏ chặn cuộc hội thoại: {ConversationId}", null, request.ConversationId);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));

            var result = await _conversationRepository.UnblockConversationAsync(request.ConversationId);
            
            if (result)
            {
                LogInfo("Bỏ chặn cuộc hội thoại thành công: {ConversationId}", null, request.ConversationId);
            }

            return result;
        }, "UnblockConversation");
    }

    /// <summary>
    /// Kiểm tra cuộc hội thoại có bị chặn không
    /// </summary>
    public async Task<bool> IsConversationBlockedAsync(string conversationId)
    {
        return await _conversationRepository.IsConversationBlockedAsync(conversationId);
    }
}