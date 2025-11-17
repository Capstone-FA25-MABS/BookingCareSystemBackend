using AutoMapper;
using BookingCare.Services.Communication.Extensions;
using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Utils;
using BookingCare.Shared.Common.Services;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation of the Conversation service with BaseService, lazy loading and participant enrichment
/// </summary>
public class ConversationService : BaseService, IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageService _messageService;
    private readonly IParticipantEnrichmentService _participantEnrichmentService;
    private readonly IMapper _mapper;
    private readonly IHubContext<ChatHub> _hubContext;

    public ConversationService(
        IConversationRepository conversationRepository,
        IMessageService messageService,
        IParticipantEnrichmentService participantEnrichmentService,
        IMapper mapper,
        IHubContext<ChatHub> hubContext,
        ILogger<ConversationService> logger
    )
        : base(logger)
    {
        _conversationRepository = conversationRepository;
        _messageService = messageService;
        _participantEnrichmentService = participantEnrichmentService;
        _mapper = mapper;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    public async Task<ConversationResponse> CreateAsync(CreateConversationRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting creation of conversation with {Count} participants",
                    correlationId: null,
                    args: new object[] { request.Participants.Count }
                );

                // Validation
                ValidateRequired(request, nameof(request));
                if (request.Participants == null || request.Participants.Count < 2)
                {
                    throw new ArgumentException("Conversation must have at least 2 participants");
                }

                // 🎯 Normalize all participant IDs to uppercase for case-insensitive comparison
                var normalizedParticipants = request
                    .Participants.Select(p => p.ToUpperInvariant())
                    .ToList();

                // Kiểm tra xem conversation giữa 2 user đã tồn tại chưa (nếu là chat 1-1)
                if (normalizedParticipants.Count == 2)
                {
                    var existingConversation =
                        await _conversationRepository.GetConversationBetweenUsersAsync(
                            normalizedParticipants[0],
                            normalizedParticipants[1]
                        );

                    if (existingConversation != null)
                    {
                        LogInfo(
                            "Conversation between two users already exists: {ConversationId}",
                            correlationId: null,
                            args: new object[] { existingConversation.Id }
                        );
                        return _mapper.Map<ConversationResponse>(existingConversation);
                    }
                }

                // Tạo conversation mới với normalized participants
                var conversationEntity = _mapper.Map<ConversationEntity>(request);
                conversationEntity.Participants = normalizedParticipants;

                var createdConversation = await _conversationRepository.CreateAsync(
                    conversationEntity
                );

                LogInfo(
                    "Conversation created successfully with ID: {ConversationId}",
                    correlationId: null,
                    args: new object[] { createdConversation.Id }
                );

                // ✅ Broadcast ConversationCreated event to all participants via SignalR
                try
                {
                    var conversationResponse = _mapper.Map<ConversationResponse>(
                        createdConversation
                    );

                    // Send to each participant's personal group
                    foreach (var participantId in normalizedParticipants)
                    {
                        var userGroupName = $"user_{participantId}";
                        await _hubContext
                            .Clients.Group(userGroupName)
                            .SendAsync(
                                "ConversationCreated",
                                new
                                {
                                    conversationId = createdConversation.Id,
                                    participants = normalizedParticipants,
                                    createdAt = createdConversation.CreatedAt,
                                    conversation = conversationResponse,
                                }
                            );

                        LogInfo(
                            "📤 Sent ConversationCreated event to user {UserId} for conversation {ConversationId}",
                            correlationId: null,
                            args: new object[] { participantId, createdConversation.Id }
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the request if SignalR broadcast fails
                    LogWarning(
                        "⚠️ Failed to broadcast ConversationCreated event: {Error}",
                        correlationId: null,
                        args: new object[] { ex.Message }
                    );
                }

                return _mapper.Map<ConversationResponse>(createdConversation);
            },
            "CreateConversation"
        );
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
    public async Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20
    )
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(userId, page, pageSize);
        return _mapper.Map<IEnumerable<ConversationResponse>>(conversations);
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với lazy loading support
    /// </summary>
    public async Task<IEnumerable<ConversationResponse>> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        ConversationLoadOptions? options = null
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching conversations for user: {UserId}, page: {Page}, pageSize: {PageSize} with lazy loading",
                    correlationId: null,
                    args: new object[] { userId, page, pageSize }
                );

                var conversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    page,
                    pageSize
                );
                var result = _mapper.Map<IEnumerable<ConversationResponse>>(conversations).ToList();

                if (options != null)
                {
                    // Lazy load additional data based on options
                    await LoadConversationDataAsync(result, userId, options);
                }

                LogInfo(
                    "Successfully fetched {Count} conversations for user: {UserId}",
                    correlationId: null,
                    args: new object[] { result.Count, userId }
                );
                return result;
            },
            "GetConversationsByUserIdWithLazyLoading"
        );
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại lightweight
    /// </summary>
    public async Task<IEnumerable<ConversationListResponse>> GetConversationsLightweightAsync(
        string userId,
        int page = 1,
        int pageSize = 20
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching lightweight conversations for user: {UserId}",
                    correlationId: null,
                    args: new object[] { userId }
                );

                var conversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    page,
                    pageSize
                );
                var result = conversations
                    .Select(c => new ConversationListResponse
                    {
                        Id = c.Id,
                        Participants = c.Participants,
                        LastMessage = _mapper.Map<LastMessageResponse>(c.LastMessage),
                        UpdatedAt = c.UpdatedAt,
                        IsActive = c.IsActive,
                        IsBlocked = c.Blocked != null,
                        UnreadCount =
                            0 // Will be loaded separately if needed
                        ,
                    })
                    .ToList();

                LogInfo(
                    "Successfully fetched {Count} lightweight conversations for user: {UserId}",
                    correlationId: null,
                    args: new object[] { result.Count, userId }
                );
                return result;
            },
            "GetConversationsLightweight"
        );
    }

    /// <summary>
    /// Lấy chi tiết conversation với lazy loading
    /// </summary>
    public async Task<ConversationResponse?> GetConversationDetailsAsync(
        string id,
        ConversationLoadOptions? options = null
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching conversation details: {ConversationId}",
                    correlationId: null,
                    args: new object[] { id }
                );

                var conversation = await _conversationRepository.GetByIdAsync(id);
                if (conversation == null)
                    return null;

                var result = _mapper.Map<ConversationResponse>(conversation);

                if (options != null)
                {
                    await LoadConversationDataAsync(new[] { result }, "", options);
                }

                LogInfo(
                    "Successfully fetched conversation details: {ConversationId}",
                    correlationId: null,
                    args: new object[] { id }
                );
                return result;
            },
            "GetConversationDetails"
        );
    }

    /// <summary>
    /// Helper method để load dữ liệu lazy loading với Auth Service + Redis caching
    /// </summary>
    private async Task LoadConversationDataAsync(
        IEnumerable<ConversationResponse> conversations,
        string currentUserId,
        ConversationLoadOptions options
    )
    {
        var conversationList = conversations.ToList();

        // Load unread count
        if (options.IncludeUnreadCount && !string.IsNullOrEmpty(currentUserId))
        {
            await LoadUnreadCountsAsync(conversationList, currentUserId);
        }

        // Load participant details from Auth Service - ONLY OTHER PARTICIPANTS
        if (options.IncludeParticipantDetails)
        {
            await LoadParticipantDetailsAsync(conversationList, currentUserId);
        }

        // Load metadata
        if (options.IncludeMetadata)
        {
            await LoadMetadataAsync(conversationList);
        }
    }

    /// <summary>
    /// Load unread counts for conversations
    /// </summary>
    private async Task LoadUnreadCountsAsync(
        List<ConversationResponse> conversations,
        string currentUserId
    )
    {
        foreach (var conversation in conversations)
        {
            try
            {
                conversation.UnreadCount = await _messageService.GetUnreadCountAsync(
                    conversation.Id,
                    currentUserId
                );
                LogDebug(
                    "Loaded unread count {Count} for conversation {ConversationId}",
                    null,
                    conversation.UnreadCount,
                    conversation.Id
                );
            }
            catch (Exception ex)
            {
                LogWarning(
                    "Error loading unread count for conversation {ConversationId}: {Error}",
                    correlationId: null,
                    args: new object[] { conversation.Id, ex.Message }
                );
                conversation.UnreadCount = 0;
            }
        }
    }

    /// <summary>
    /// Load participant details from Auth Service with caching
    /// </summary>
    private async Task LoadParticipantDetailsAsync(
        List<ConversationResponse> conversations,
        string currentUserId
    )
    {
        try
        {
            if (!string.IsNullOrEmpty(currentUserId))
            {
                LogDebug(
                    "Starting enrichment of OTHER participant details for {Count} conversations (exclude current user: {CurrentUserId})",
                    correlationId: null,
                    args: new object[] { conversations.Count, currentUserId }
                );

                // OPTIMIZATION: Only enrich OTHER participants (exclude current user)
                await _participantEnrichmentService.EnrichOtherParticipantDetailsAsync(
                    conversations,
                    currentUserId
                );

                LogDebug(
                    "Completed enrichment of OTHER participant details for {Count} conversations",
                    correlationId: null,
                    args: new object[] { conversations.Count }
                );
            }
            else
            {
                LogDebug(
                    "Starting enrichment of ALL participant details for {Count} conversations (no current user specified)",
                    correlationId: null,
                    args: new object[] { conversations.Count }
                );

                // FALLBACK: If no currentUserId, load all (backward compatibility)
                await _participantEnrichmentService.EnrichParticipantDetailsAsync(conversations);

                LogDebug(
                    "Completed enrichment of ALL participant details for {Count} conversations",
                    correlationId: null,
                    args: new object[] { conversations.Count }
                );
            }
        }
        catch (Exception ex)
        {
            LogError(
                ex,
                "Error enriching participant details for conversations",
                correlationId: null
            );

            // Fallback: Initialize empty lists to avoid null reference
            foreach (var conversation in conversations)
            {
                conversation.ParticipantDetails = new List<ConversationParticipant>();
            }
        }
    }

    /// <summary>
    /// Load metadata for conversations
    /// </summary>
    private async Task LoadMetadataAsync(List<ConversationResponse> conversations)
    {
        foreach (var conversation in conversations)
        {
            try
            {
                // Initialize basic metadata
                conversation.Metadata = new ConversationMetadata
                {
                    TotalMessages = 0, //  Implement repository method to get actual count
                    TotalFiles = 0, //  Implement repository method
                    TotalImages = 0, //  Implement repository method
                    FirstMessageDate = null, //  Implement repository method
                    CommonFiles = new List<string>(),
                };

                LogDebug(
                    "Metadata loaded for conversation {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversation.Id }
                );
            }
            catch (Exception ex)
            {
                LogWarning(
                    "Error loading metadata for conversation {ConversationId}: {Error}",
                    correlationId: null,
                    args: new object[] { conversation.Id, ex.Message }
                );
                conversation.Metadata = new ConversationMetadata();
            }
        }

        await Task.CompletedTask; // Make method async
    }

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 người dùng
    /// </summary>
    public async Task<ConversationResponse?> GetConversationBetweenUsersAsync(
        string userId1,
        string userId2
    )
    {
        var conversation = await _conversationRepository.GetConversationBetweenUsersAsync(
            userId1,
            userId2
        );
        return conversation != null ? _mapper.Map<ConversationResponse>(conversation) : null;
    }

    /// <summary>
    /// Xóa cuộc hội thoại
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting deletion of conversation: {ConversationId}",
                    correlationId: null,
                    args: new object[] { id }
                );

                ValidateRequiredString(id, nameof(id));

                var result = await _conversationRepository.DeleteAsync(id);
                if (result)
                {
                    LogInfo(
                        "Conversation deleted successfully: {ConversationId}",
                        correlationId: null,
                        args: new object[] { id }
                    );
                }
                else
                {
                    LogWarning(
                        "Failed to delete conversation: {ConversationId}",
                        correlationId: null,
                        args: new object[] { id }
                    );
                }

                return result;
            },
            "DeleteConversation"
        );
    }

    /// <summary>
    /// Cập nhật tin nhắn cuối cùng
    /// </summary>
    public async Task<bool> UpdateLastMessageAsync(
        string conversationId,
        string messageId,
        string content,
        string senderId
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Updating last message for conversation: {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversationId }
                );

                ValidateRequiredString(conversationId, nameof(conversationId));
                ValidateRequiredString(messageId, nameof(messageId));
                ValidateRequiredString(content, nameof(content));
                ValidateRequiredString(senderId, nameof(senderId));

                var lastMessage = new LastMessage
                {
                    MessageId = messageId,
                    Content = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                    SenderId = senderId,
                    CreatedAt = DateTime.UtcNow,
                };

                var result = await _conversationRepository.UpdateLastMessageAsync(
                    conversationId,
                    lastMessage
                );

                if (result)
                {
                    LogInfo(
                        "Last message updated successfully for conversation: {ConversationId}",
                        correlationId: null,
                        args: new object[] { conversationId }
                    );
                }

                return result;
            },
            "UpdateLastMessage"
        );
    }

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> BlockConversationAsync(BlockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Blocking conversation: {ConversationId} by user: {UserId}",
                    correlationId: null,
                    args: new object[] { request.ConversationId, request.BlockedBy }
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
                ValidateRequiredString(request.BlockedBy, nameof(request.BlockedBy));

                var result = await _conversationRepository.BlockConversationAsync(
                    request.ConversationId,
                    request.BlockedBy
                );

                if (result)
                {
                    LogInfo(
                        "Conversation blocked successfully: {ConversationId}",
                        correlationId: null,
                        args: new object[] { request.ConversationId }
                    );
                }

                return result;
            },
            "BlockConversation"
        );
    }

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> UnblockConversationAsync(UnblockConversationRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Unblocking conversation: {ConversationId}",
                    correlationId: null,
                    args: new object[] { request.ConversationId }
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));

                var result = await _conversationRepository.UnblockConversationAsync(
                    request.ConversationId
                );

                if (result)
                {
                    LogInfo(
                        "Conversation unblocked successfully: {ConversationId}",
                        correlationId: null,
                        args: new object[] { request.ConversationId }
                    );
                }

                return result;
            },
            "UnblockConversation"
        );
    }

    /// <summary>
    /// Kiểm tra cuộc hội thoại có bị chặn không
    /// </summary>
    public async Task<bool> IsConversationBlockedAsync(string conversationId)
    {
        return await _conversationRepository.IsConversationBlockedAsync(conversationId);
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với cursor-based pagination và participant enrichment
    /// </summary>
    public async Task<CursorPaginatedResponse<ConversationResponse>> GetByUserIdWithCursorAsync(
        string userId,
        string? before = null,
        string? after = null,
        int limit = 20,
        ConversationLoadOptions? options = null
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching conversations with cursor pagination for user: {UserId}, before: {Before}, after: {After}, limit: {Limit}",
                    correlationId: null,
                    args: new object[]
                    {
                        userId,
                        before ?? string.Empty,
                        after ?? string.Empty,
                        limit,
                    }
                );

                ValidateRequiredString(userId, nameof(userId));
                ValidatePaginationLimit(limit);

                // Lấy conversations từ repository với cursor
                var conversations = await _conversationRepository.GetByUserIdWithCursorAsync(
                    userId,
                    before,
                    after,
                    limit + 1
                );
                var conversationList = conversations.ToList();

                // Xử lý pagination và convert to DTOs
                var paginationResult = ProcessPaginationResult(
                    conversationList,
                    limit,
                    before,
                    after
                );

                // Apply lazy loading if options provided
                if (options != null)
                {
                    await LoadConversationDataAsync(
                        paginationResult.ConversationDtos,
                        userId,
                        options
                    );
                }

                // Generate cursors
                var cursors = GeneratePaginationCursors(
                    paginationResult.ConversationDtos,
                    paginationResult.HasNext,
                    before
                );

                var result = new CursorPaginatedResponse<ConversationResponse>
                {
                    Data = paginationResult.ConversationDtos,
                    NextCursor = cursors.NextCursor,
                    PreviousCursor = cursors.PreviousCursor,
                    HasNext = paginationResult.HasNext,
                    HasPrevious = paginationResult.HasPrevious,
                    Limit = limit,
                };

                LogInfo(
                    "Successfully fetched {Count} conversations with cursor pagination and participant enrichment for user: {UserId}",
                    correlationId: null,
                    args: new object[] { paginationResult.ConversationDtos.Count, userId }
                );

                return result;
            },
            "GetConversationsByUserIdWithCursor"
        );
    }

    /// <summary>
    /// Validate pagination limit
    /// </summary>
    private static void ValidatePaginationLimit(int limit)
    {
        if (limit <= 0 || limit > 100)
        {
            throw new ArgumentException("Limit must be between 1 and 100");
        }
    }

    /// <summary>
    /// Process pagination result and convert to DTOs
    /// </summary>
    private (
        List<ConversationResponse> ConversationDtos,
        bool HasNext,
        bool HasPrevious
    ) ProcessPaginationResult(
        List<ConversationEntity> conversationList,
        int limit,
        string? before,
        string? after
    )
    {
        var hasNext = conversationList.Count > limit;
        var hasPrevious = !string.IsNullOrEmpty(before) || !string.IsNullOrEmpty(after);

        // Remove extra item if exists
        if (hasNext)
        {
            conversationList.RemoveAt(conversationList.Count - 1);
        }

        // Convert to DTOs
        var conversationDtos = _mapper.Map<List<ConversationResponse>>(conversationList);

        return (conversationDtos, hasNext, hasPrevious);
    }

    /// <summary>
    /// Generate pagination cursors
    /// </summary>
    private static (string? NextCursor, string? PreviousCursor) GeneratePaginationCursors(
        List<ConversationResponse> conversationDtos,
        bool hasNext,
        string? before
    )
    {
        if (!conversationDtos.Any())
        {
            return (null, null);
        }

        string? nextCursor = GenerateNextCursor(conversationDtos, hasNext);
        string? previousCursor = GeneratePreviousCursor(conversationDtos, before);

        return (nextCursor, previousCursor);
    }

    /// <summary>
    /// Generate next cursor if available
    /// </summary>
    private static string? GenerateNextCursor(
        List<ConversationResponse> conversationDtos,
        bool hasNext
    )
    {
        if (!hasNext)
        {
            return null;
        }

        var lastConversation = conversationDtos[conversationDtos.Count - 1];
        return CursorHelper.GenerateCursor(lastConversation.Id, lastConversation.UpdatedAt);
    }

    /// <summary>
    /// Generate previous cursor if available
    /// </summary>
    private static string? GeneratePreviousCursor(
        List<ConversationResponse> conversationDtos,
        string? before
    )
    {
        if (string.IsNullOrEmpty(before))
        {
            return null;
        }

        var firstConversation = conversationDtos[0];
        return CursorHelper.GenerateCursor(firstConversation.Id, firstConversation.UpdatedAt);
    }

    /// <summary>
    /// Get conversations by tag IDs
    /// </summary>
    public async Task<PaginatedResponse<ConversationResponse>> GetConversationsByTagsAsync(
        string userId,
        List<string> tagIds,
        string filterMode = "any",
        int pageNumber = 1,
        int pageSize = 20
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Getting conversations by tags for user {UserId}",
                    correlationId: null,
                    args: new object[] { userId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagIds, nameof(tagIds));

                var allConversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    1,
                    int.MaxValue
                );

                // Sử dụng extension method để filter conversations
                var filteredConversations = allConversations.FilterByTags(
                    userId,
                    tagIds,
                    filterMode
                );

                var totalCount = filteredConversations.Count();
                var pagedConversations = filteredConversations
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var conversationResponses = _mapper.Map<List<ConversationResponse>>(
                    pagedConversations
                );

                // Enrich participant info
                await _participantEnrichmentService.EnrichOtherParticipantDetailsAsync(
                    conversationResponses,
                    userId
                );

                return new PaginatedResponse<ConversationResponse>
                {
                    Items = conversationResponses,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                };
            },
            nameof(GetConversationsByTagsAsync)
        );
    }

    /// <summary>
    /// Get grouped conversations by tags
    /// </summary>
    public async Task<
        Dictionary<string, List<ConversationResponse>>
    > GetGroupedConversationsByTagsAsync(string userId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Getting grouped conversations by tags for user {UserId}",
                    correlationId: null,
                    args: new object[] { userId }
                );

                ValidateRequired(userId, nameof(userId));

                var allConversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    1,
                    int.MaxValue
                );
                var grouped = new Dictionary<string, List<ConversationResponse>>();
                var allConversationDtos = new List<ConversationResponse>();

                foreach (var conversation in allConversations)
                {
                    var conversationDto = _mapper.Map<ConversationResponse>(conversation);
                    allConversationDtos.Add(conversationDto);

                    // Lấy tags của user hiện tại
                    var userTagIds = conversation.UserTags.ContainsKey(userId)
                        ? conversation.UserTags[userId]
                        : new List<string>();

                    if (userTagIds.Count == 0)
                    {
                        // Nhóm "Không có tag"
                        if (!grouped.ContainsKey("untagged"))
                        {
                            grouped["untagged"] = new List<ConversationResponse>();
                        }
                        grouped["untagged"].Add(conversationDto);
                    }
                    else
                    {
                        // Thêm vào các nhóm theo tag
                        foreach (var tagId in userTagIds)
                        {
                            if (!grouped.ContainsKey(tagId))
                            {
                                grouped[tagId] = new List<ConversationResponse>();
                            }
                            grouped[tagId].Add(conversationDto);
                        }
                    }
                }

                // Enrich all conversations at once
                await _participantEnrichmentService.EnrichOtherParticipantDetailsAsync(
                    allConversationDtos,
                    userId
                );

                // Sắp xếp conversations trong mỗi nhóm theo thời gian cập nhật
                foreach (var key in grouped.Keys.ToList())
                {
                    grouped[key] = grouped[key].OrderByDescending(c => c.UpdatedAt).ToList();
                }

                return grouped;
            },
            nameof(GetGroupedConversationsByTagsAsync)
        );
    }
}
