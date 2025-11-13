using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using MongoDB.Driver;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation của Tag service - quản lý tags/labels cho cuộc hội thoại
/// </summary>
public class TagService : BaseService, ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public TagService(
        ITagRepository tagRepository,
        IConversationRepository conversationRepository,
        IMapper mapper,
        ILogger<TagService> logger
    )
        : base(logger)
    {
        _tagRepository = tagRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    #region Tag Management

    /// <summary>
    /// Tạo tag mới
    /// </summary>
    public async Task<TagDto> CreateTagAsync(string userId, CreateTagDto dto)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Creating new tag for user {UserId}",
                    correlationId: null,
                    args: new object[] { userId }
                );

                ValidateRequired(dto, nameof(dto));
                ValidateRequired(userId, nameof(userId));

                // Kiểm tra xem tag với tên này đã tồn tại chưa
                var existingTag = await _tagRepository.GetByNameAsync(userId, dto.Name);
                if (existingTag != null)
                {
                    throw new InvalidOperationException($"Tag với tên '{dto.Name}' đã tồn tại");
                }

                var tagEntity = _mapper.Map<TagEntity>(dto);
                tagEntity.UserId = userId;
                tagEntity.ConversationCount = 0;

                var createdTag = await _tagRepository.CreateAsync(tagEntity);

                LogInfo(
                    "Tag {TagId} created successfully for user {UserId}",
                    correlationId: null,
                    args: new object[] { createdTag.Id, userId }
                );

                return _mapper.Map<TagDto>(createdTag);
            },
            nameof(CreateTagAsync)
        );
    }

    /// <summary>
    /// Cập nhật tag
    /// </summary>
    public async Task<TagDto?> UpdateTagAsync(string userId, string tagId, UpdateTagDto dto)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Updating tag {TagId} for user {UserId}",
                    correlationId: null,
                    args: new object[] { tagId, userId }
                );

                ValidateRequired(dto, nameof(dto));
                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagId, nameof(tagId));

                // Kiểm tra quyền sở hữu
                if (!await _tagRepository.IsTagOwnedByUserAsync(tagId, userId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật tag này");
                }

                var existingTag = await _tagRepository.GetByIdAsync(tagId);
                if (existingTag == null)
                {
                    LogWarning(
                        "Tag {TagId} not found",
                        correlationId: null,
                        args: new object[] { tagId }
                    );
                    return null;
                }

                // Kiểm tra trùng tên (nếu đổi tên)
                if (!string.IsNullOrEmpty(dto.Name) && dto.Name != existingTag.Name)
                {
                    var duplicateTag = await _tagRepository.GetByNameAsync(userId, dto.Name);
                    if (duplicateTag != null)
                    {
                        throw new InvalidOperationException($"Tag với tên '{dto.Name}' đã tồn tại");
                    }
                }

                // Cập nhật các trường
                if (!string.IsNullOrEmpty(dto.Name))
                    existingTag.Name = dto.Name;
                if (dto.Description != null)
                    existingTag.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.Color))
                    existingTag.Color = dto.Color;
                if (dto.Icon != null)
                    existingTag.Icon = dto.Icon;
                if (dto.Type.HasValue)
                    existingTag.Type = dto.Type.Value;
                if (dto.Order.HasValue)
                    existingTag.Order = dto.Order.Value;
                if (dto.IsPinned.HasValue)
                    existingTag.IsPinned = dto.IsPinned.Value;
                if (dto.IsActive.HasValue)
                    existingTag.IsActive = dto.IsActive.Value;

                var updatedTag = await _tagRepository.UpdateAsync(tagId, existingTag);

                LogInfo(
                    "Tag {TagId} updated successfully",
                    correlationId: null,
                    args: new object[] { tagId }
                );

                return updatedTag != null ? _mapper.Map<TagDto>(updatedTag) : null;
            },
            nameof(UpdateTagAsync)
        );
    }

    /// <summary>
    /// Xóa tag
    /// </summary>
    public async Task<bool> DeleteTagAsync(string userId, string tagId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Deleting tag {TagId} for user {UserId}",
                    correlationId: null,
                    args: new object[] { tagId, userId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagId, nameof(tagId));

                // Kiểm tra quyền sở hữu
                if (!await _tagRepository.IsTagOwnedByUserAsync(tagId, userId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa tag này");
                }

                // Xóa tag khỏi tất cả conversations của user
                var conversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    1,
                    int.MaxValue
                );
                foreach (var conversation in conversations)
                {
                    if (
                        conversation.UserTags.ContainsKey(userId)
                        && conversation.UserTags[userId].Contains(tagId)
                    )
                    {
                        conversation.UserTags[userId].Remove(tagId);
                        if (conversation.UserTags[userId].Count == 0)
                        {
                            conversation.UserTags.Remove(userId);
                        }
                        await _conversationRepository.UpdateAsync(conversation);
                    }
                }

                var result = await _tagRepository.DeleteAsync(tagId);

                LogInfo(
                    "Tag {TagId} deleted successfully",
                    correlationId: null,
                    args: new object[] { tagId }
                );

                return result;
            },
            nameof(DeleteTagAsync)
        );
    }

    /// <summary>
    /// Lấy tag theo ID
    /// </summary>
    public async Task<TagDto?> GetTagByIdAsync(string tagId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(tagId, nameof(tagId));

                var tag = await _tagRepository.GetByIdAsync(tagId);
                return tag != null ? _mapper.Map<TagDto>(tag) : null;
            },
            nameof(GetTagByIdAsync)
        );
    }

    /// <summary>
    /// Lấy danh sách tag của user
    /// </summary>
    public async Task<IEnumerable<TagDto>> GetUserTagsAsync(
        string userId,
        bool includeSystem = true
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(userId, nameof(userId));

                var tags = await _tagRepository.GetSortedTagsAsync(userId, includeSystem);
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            },
            nameof(GetUserTagsAsync)
        );
    }

    /// <summary>
    /// Lấy danh sách system tags
    /// </summary>
    public async Task<IEnumerable<TagDto>> GetSystemTagsAsync()
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                var tags = await _tagRepository.GetSystemTagsAsync();
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            },
            nameof(GetSystemTagsAsync)
        );
    }

    #endregion

    #region Conversation-Tag Association

    /// <summary>
    /// Gán tag vào cuộc hội thoại
    /// </summary>
    public async Task<bool> AddTagToConversationAsync(
        string userId,
        string conversationId,
        List<string> tagIds
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Adding tags to conversation {ConversationId} for user {UserId}",
                    correlationId: null,
                    args: new object[] { conversationId, userId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(conversationId, nameof(conversationId));
                ValidateRequired(tagIds, nameof(tagIds));

                // Normalize IDs to uppercase for consistency
                userId = userId.ToUpperInvariant();
                var normalizedTagIds = tagIds.Select(t => t.ToUpperInvariant()).ToList();

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new InvalidOperationException("Cuộc hội thoại không tồn tại");
                }

                // Kiểm tra user có trong conversation không (case-insensitive)
                if (
                    !conversation.Participants.Any(p =>
                        p.Equals(userId, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền truy cập cuộc hội thoại này"
                    );
                }

                // Validate tags
                if (!await ValidateTagsAsync(userId, normalizedTagIds))
                {
                    throw new InvalidOperationException("Một hoặc nhiều tag không hợp lệ");
                }

                // Find or create user key in UserTags (case-insensitive)
                var userKey =
                    conversation.UserTags.Keys.FirstOrDefault(k =>
                        k.Equals(userId, StringComparison.OrdinalIgnoreCase)
                    ) ?? userId;

                // Khởi tạo UserTags cho user nếu chưa có
                if (!conversation.UserTags.ContainsKey(userKey))
                {
                    conversation.UserTags[userKey] = new List<string>();
                }

                // Thêm tags mới (không trùng lặp, case-insensitive)
                var addedCount = 0;
                foreach (var tagId in normalizedTagIds)
                {
                    if (
                        !conversation
                            .UserTags[userKey]
                            .Any(t => t.Equals(tagId, StringComparison.OrdinalIgnoreCase))
                    )
                    {
                        conversation.UserTags[userKey].Add(tagId);
                        await _tagRepository.IncrementConversationCountAsync(tagId);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _conversationRepository.UpdateAsync(conversation);
                    LogInfo(
                        "Added {Count} tags to conversation {ConversationId} for user {UserId}",
                        correlationId: null,
                        args: new object[] { addedCount, conversationId, userId }
                    );
                }

                return true;
            },
            nameof(AddTagToConversationAsync)
        );
    }

    /// <summary>
    /// Xóa tag khỏi cuộc hội thoại
    /// </summary>
    public async Task<bool> RemoveTagFromConversationAsync(
        string userId,
        string conversationId,
        string tagId
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Removing tag {TagId} from conversation {ConversationId} for user {UserId}",
                    correlationId: null,
                    args: new object[] { tagId, conversationId, userId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(conversationId, nameof(conversationId));
                ValidateRequired(tagId, nameof(tagId));

                // Normalize IDs to uppercase for consistency
                userId = userId.ToUpperInvariant();
                tagId = tagId.ToUpperInvariant();

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new InvalidOperationException("Cuộc hội thoại không tồn tại");
                }

                // Kiểm tra user có trong conversation không
                if (
                    !conversation.Participants.Any(p =>
                        p.Equals(userId, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền truy cập cuộc hội thoại này"
                    );
                }

                // Find the correct user key (case-insensitive)
                var userKey = conversation.UserTags.Keys.FirstOrDefault(k =>
                    k.Equals(userId, StringComparison.OrdinalIgnoreCase)
                );

                if (userKey != null && conversation.UserTags[userKey] != null)
                {
                    // Find and remove the tag (case-insensitive)
                    var tagToRemove = conversation
                        .UserTags[userKey]
                        .FirstOrDefault(t => t.Equals(tagId, StringComparison.OrdinalIgnoreCase));

                    if (tagToRemove != null)
                    {
                        conversation.UserTags[userKey].Remove(tagToRemove);

                        if (conversation.UserTags[userKey].Count == 0)
                        {
                            conversation.UserTags.Remove(userKey);
                        }

                        await _conversationRepository.UpdateAsync(conversation);
                        await _tagRepository.DecrementConversationCountAsync(tagId);

                        LogInfo(
                            "Successfully removed tag {TagId} from conversation {ConversationId} for user {UserId}",
                            correlationId: null,
                            args: new object[] { tagId, conversationId, userId }
                        );
                        return true;
                    }
                    else
                    {
                        LogWarning(
                            "Tag {TagId} not found in conversation {ConversationId} for user {UserId}. Available tags: {Tags}",
                            correlationId: null,
                            args: new object[]
                            {
                                tagId,
                                conversationId,
                                userId,
                                string.Join(", ", conversation.UserTags[userKey]),
                            }
                        );
                    }
                }
                else
                {
                    LogWarning(
                        "User {UserId} has no tags in conversation {ConversationId}",
                        correlationId: null,
                        args: new object[] { userId, conversationId }
                    );
                }

                return false;
            },
            nameof(RemoveTagFromConversationAsync)
        );
    }

    /// <summary>
    /// Xóa tất cả tag khỏi cuộc hội thoại
    /// </summary>
    public async Task<bool> RemoveAllTagsFromConversationAsync(string userId, string conversationId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Removing all tags from conversation {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversationId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(conversationId, nameof(conversationId));

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new InvalidOperationException("Cuộc hội thoại không tồn tại");
                }

                // Kiểm tra user có trong conversation không
                if (!conversation.Participants.Contains(userId))
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền truy cập cuộc hội thoại này"
                    );
                }

                // Giảm count cho tất cả tags của user
                if (conversation.UserTags.ContainsKey(userId))
                {
                    foreach (var tagId in conversation.UserTags[userId])
                    {
                        await _tagRepository.DecrementConversationCountAsync(tagId);
                    }
                    conversation.UserTags.Remove(userId);
                    await _conversationRepository.UpdateAsync(conversation);
                }

                LogInfo(
                    "Removed all tags from conversation {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversationId }
                );

                return true;
            },
            nameof(RemoveAllTagsFromConversationAsync)
        );
    }

    /// <summary>
    /// Cập nhật danh sách tag cho cuộc hội thoại (replace toàn bộ)
    /// </summary>
    public async Task<bool> UpdateConversationTagsAsync(
        string userId,
        string conversationId,
        List<string> tagIds
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Updating tags for conversation {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversationId }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(conversationId, nameof(conversationId));
                ValidateRequired(tagIds, nameof(tagIds));

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new InvalidOperationException("Cuộc hội thoại không tồn tại");
                }

                // Kiểm tra user có trong conversation không
                if (!conversation.Participants.Contains(userId))
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền truy cập cuộc hội thoại này"
                    );
                }

                // Validate tags
                if (!await ValidateTagsAsync(userId, tagIds))
                {
                    throw new InvalidOperationException("Một hoặc nhiều tag không hợp lệ");
                }

                // Giảm count cho tags cũ của user
                if (conversation.UserTags.ContainsKey(userId))
                {
                    foreach (var oldTagId in conversation.UserTags[userId])
                    {
                        await _tagRepository.DecrementConversationCountAsync(oldTagId);
                    }
                }

                // Tăng count cho tags mới
                foreach (var newTagId in tagIds)
                {
                    await _tagRepository.IncrementConversationCountAsync(newTagId);
                }

                // Cập nhật tags cho user
                conversation.UserTags[userId] = tagIds;
                await _conversationRepository.UpdateAsync(conversation);

                LogInfo(
                    "Updated tags for conversation {ConversationId}",
                    correlationId: null,
                    args: new object[] { conversationId }
                );

                return true;
            },
            nameof(UpdateConversationTagsAsync)
        );
    }

    /// <summary>
    /// Lấy danh sách tag của một cuộc hội thoại
    /// </summary>
    public async Task<IEnumerable<TagDto>> GetConversationTagsAsync(string conversationId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(conversationId, nameof(conversationId));

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    return Enumerable.Empty<TagDto>();
                }

                // Lấy tất cả tags từ tất cả users (có thể filter theo userId nếu cần)
                var allTagIds = conversation
                    .UserTags.Values.SelectMany(tags => tags)
                    .Distinct()
                    .ToList();
                if (allTagIds.Count == 0)
                {
                    return Enumerable.Empty<TagDto>();
                }

                var tags = await _tagRepository.GetByIdsAsync(allTagIds);
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            },
            nameof(GetConversationTagsAsync)
        );
    }

    /// <summary>
    /// Lấy danh sách tag của user cho một cuộc hội thoại cụ thể
    /// </summary>
    public async Task<IEnumerable<TagDto>> GetUserConversationTagsAsync(
        string userId,
        string conversationId
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(userId, nameof(userId));
                ValidateRequired(conversationId, nameof(conversationId));

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null || !conversation.UserTags.ContainsKey(userId))
                {
                    return Enumerable.Empty<TagDto>();
                }

                var userTagIds = conversation.UserTags[userId];
                if (userTagIds.Count == 0)
                {
                    return Enumerable.Empty<TagDto>();
                }

                var tags = await _tagRepository.GetByIdsAsync(userTagIds);
                return _mapper.Map<IEnumerable<TagDto>>(tags);
            },
            nameof(GetUserConversationTagsAsync)
        );
    }

    #endregion

    #region Tag Statistics & Filtering

    /// <summary>
    /// Lấy thống kê của tag
    /// </summary>
    public async Task<TagStatisticsDto?> GetTagStatisticsAsync(string userId, string tagId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagId, nameof(tagId));

                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    return null;
                }

                var conversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    1,
                    int.MaxValue
                );
                var taggedConversations = conversations
                    .Where(c =>
                        c.UserTags.ContainsKey(userId) && c.UserTags[userId].Contains(tagId)
                    )
                    .ToList();

                var statistics = new TagStatisticsDto
                {
                    Tag = _mapper.Map<TagDto>(tag),
                    TotalConversations = taggedConversations.Count,
                    LastMessageAt = taggedConversations.Max(c => c.LastMessage?.CreatedAt),
                };

                // Đếm tin nhắn chưa đọc
                var unreadCount = 0;
                // TODO: Implement GetUnreadMessagesAsync trong IMessageRepository
                // foreach (var conversation in taggedConversations)
                // {
                //     var messages = await _messageRepository.GetUnreadMessagesAsync(
                //         conversation.Id,
                //         userId
                //     );
                //     unreadCount += messages.Count();
                // }
                statistics.UnreadMessagesCount = unreadCount;

                return statistics;
            },
            nameof(GetTagStatisticsAsync)
        );
    }

    /// <summary>
    /// Lấy danh sách thống kê của tất cả tag của user
    /// </summary>
    public async Task<IEnumerable<TagStatisticsDto>> GetAllTagsStatisticsAsync(string userId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(userId, nameof(userId));

                var tags = await _tagRepository.GetSortedTagsAsync(userId, includeSystem: true);
                var statistics = new List<TagStatisticsDto>();

                foreach (var tag in tags)
                {
                    var stat = await GetTagStatisticsAsync(userId, tag.Id);
                    if (stat != null)
                    {
                        statistics.Add(stat);
                    }
                }

                return statistics;
            },
            nameof(GetAllTagsStatisticsAsync)
        );
    }

    /// <summary>
    /// Lọc cuộc hội thoại theo tag
    /// </summary>
    public async Task<PaginatedResponse<ConversationResponse>> FilterConversationsByTagsAsync(
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
                    "Filtering conversations by tags for user {UserId}",
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

                IEnumerable<ConversationEntity> filteredConversations;
                if (filterMode.ToLower() == "all")
                {
                    // Conversation phải có TẤT CẢ các tag (trong UserTags của user)
                    filteredConversations = allConversations.Where(c =>
                        c.UserTags.ContainsKey(userId)
                        && tagIds.All(tagId => c.UserTags[userId].Contains(tagId))
                    );
                }
                else // "any"
                {
                    // Conversation có ÍT NHẤT 1 tag (trong UserTags của user)
                    filteredConversations = allConversations.Where(c =>
                        c.UserTags.ContainsKey(userId)
                        && c.UserTags[userId].Any(tagId => tagIds.Contains(tagId))
                    );
                }

                var totalCount = filteredConversations.Count();
                var pagedConversations = filteredConversations
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var conversationResponses = _mapper.Map<List<ConversationResponse>>(
                    pagedConversations
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
            nameof(FilterConversationsByTagsAsync)
        );
    }

    /// <summary>
    /// Lấy số lượng cuộc hội thoại theo từng tag
    /// </summary>
    public async Task<Dictionary<string, int>> GetConversationCountByTagsAsync(string userId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                ValidateRequired(userId, nameof(userId));

                var tags = await _tagRepository.GetSortedTagsAsync(userId, includeSystem: true);
                var counts = new Dictionary<string, int>();

                var conversations = await _conversationRepository.GetByUserIdAsync(
                    userId,
                    1,
                    int.MaxValue
                );

                foreach (var tag in tags)
                {
                    var count = conversations.Count(c =>
                        c.UserTags.ContainsKey(userId) && c.UserTags[userId].Contains(tag.Id)
                    );
                    counts[tag.Id] = count;
                }

                return counts;
            },
            nameof(GetConversationCountByTagsAsync)
        );
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Gán một tag cho nhiều cuộc hội thoại
    /// </summary>
    public async Task<int> BulkAddTagToConversationsAsync(
        string userId,
        string tagId,
        List<string> conversationIds
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Bulk adding tag {TagId} to {Count} conversations",
                    correlationId: null,
                    args: new object[] { tagId, conversationIds.Count }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagId, nameof(tagId));
                ValidateRequired(conversationIds, nameof(conversationIds));

                if (!await ValidateTagOwnershipAsync(userId, tagId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền sử dụng tag này");
                }

                var successCount = 0;
                foreach (var conversationId in conversationIds)
                {
                    try
                    {
                        var conversation = await _conversationRepository.GetByIdAsync(
                            conversationId
                        );
                        if (conversation != null && conversation.Participants.Contains(userId))
                        {
                            if (!conversation.UserTags.ContainsKey(userId))
                            {
                                conversation.UserTags[userId] = new List<string>();
                            }

                            if (!conversation.UserTags[userId].Contains(tagId))
                            {
                                conversation.UserTags[userId].Add(tagId);
                                await _conversationRepository.UpdateAsync(conversation);
                                await _tagRepository.IncrementConversationCountAsync(tagId);
                                successCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning(
                            "Failed to add tag to conversation {ConversationId}: {Error}",
                            correlationId: null,
                            args: new object[] { conversationId, ex.Message }
                        );
                    }
                }

                LogInfo(
                    "Successfully added tag {TagId} to {Count} conversations",
                    correlationId: null,
                    args: new object[] { tagId, successCount }
                );

                return successCount;
            },
            nameof(BulkAddTagToConversationsAsync)
        );
    }

    /// <summary>
    /// Xóa một tag khỏi nhiều cuộc hội thoại
    /// </summary>
    public async Task<int> BulkRemoveTagFromConversationsAsync(
        string userId,
        string tagId,
        List<string> conversationIds
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Bulk removing tag {TagId} from {Count} conversations",
                    correlationId: null,
                    args: new object[] { tagId, conversationIds.Count }
                );

                ValidateRequired(userId, nameof(userId));
                ValidateRequired(tagId, nameof(tagId));
                ValidateRequired(conversationIds, nameof(conversationIds));

                var successCount = 0;
                foreach (var conversationId in conversationIds)
                {
                    try
                    {
                        var conversation = await _conversationRepository.GetByIdAsync(
                            conversationId
                        );
                        if (conversation != null && conversation.Participants.Contains(userId))
                        {
                            if (
                                conversation.UserTags.ContainsKey(userId)
                                && conversation.UserTags[userId].Remove(tagId)
                            )
                            {
                                if (conversation.UserTags[userId].Count == 0)
                                {
                                    conversation.UserTags.Remove(userId);
                                }
                                await _conversationRepository.UpdateAsync(conversation);
                                await _tagRepository.DecrementConversationCountAsync(tagId);
                                successCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning(
                            "Failed to remove tag from conversation {ConversationId}: {Error}",
                            correlationId: null,
                            args: new object[] { conversationId, ex.Message }
                        );
                    }
                }

                LogInfo(
                    "Successfully removed tag {TagId} from {Count} conversations",
                    correlationId: null,
                    args: new object[] { tagId, successCount }
                );

                return successCount;
            },
            nameof(BulkRemoveTagFromConversationsAsync)
        );
    }

    #endregion

    #region Validation & Helper Methods

    /// <summary>
    /// Kiểm tra tag có tồn tại và thuộc về user không
    /// </summary>
    public async Task<bool> ValidateTagOwnershipAsync(string userId, string tagId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    return false;
                }

                // System tag có thể được sử dụng bởi tất cả users
                if (tag.UserId == null)
                {
                    return true;
                }

                return tag.UserId == userId;
            },
            nameof(ValidateTagOwnershipAsync)
        );
    }

    /// <summary>
    /// Kiểm tra danh sách tag có hợp lệ không
    /// </summary>
    public async Task<bool> ValidateTagsAsync(string userId, List<string> tagIds)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                foreach (var tagId in tagIds)
                {
                    if (!await ValidateTagOwnershipAsync(userId, tagId))
                    {
                        return false;
                    }
                }
                return true;
            },
            nameof(ValidateTagsAsync)
        );
    }

    #endregion
}
