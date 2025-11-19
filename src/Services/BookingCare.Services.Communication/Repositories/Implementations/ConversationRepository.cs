using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Utils;
using MongoDB.Driver;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation của Conversation repository sử dụng MongoDB
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<ConversationEntity> _conversations;

    public ConversationRepository(CommunicationDbContext context)
    {
        _conversations = context.Conversations;
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    public async Task<ConversationEntity?> GetByIdAsync(string id)
    {
        return await _conversations.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Lấy danh sách cuộc hội thoại của user với phân trang
    /// </summary>
    public async Task<IEnumerable<ConversationEntity>> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20
    )
    {
        // Normalize userId to uppercase for case-insensitive comparison
        var normalizedUserId = userId.ToUpperInvariant();
        var skip = (page - 1) * pageSize;
        return await _conversations
            .Find(c => c.Participants.Contains(normalizedUserId) && c.IsActive)
            .SortByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 người dùng
    /// </summary>
    public async Task<ConversationEntity?> GetConversationBetweenUsersAsync(
        string userId1,
        string userId2
    )
    {
        // Normalize userIds to uppercase for case-insensitive comparison
        var normalizedUserId1 = userId1.ToUpperInvariant();
        var normalizedUserId2 = userId2.ToUpperInvariant();
        return await _conversations
            .Find(c =>
                c.Participants.Contains(normalizedUserId1)
                && c.Participants.Contains(normalizedUserId2)
                && c.Participants.Count == 2
                && c.IsActive
            )
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversations.InsertOneAsync(conversation);
        return conversation;
    }

    /// <summary>
    /// Cập nhật cuộc hội thoại
    /// </summary>
    public async Task<ConversationEntity> UpdateAsync(ConversationEntity conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversations.ReplaceOneAsync(c => c.Id == conversation.Id, conversation);
        return conversation;
    }

    /// <summary>
    /// Xóa cuộc hội thoại (soft delete)
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<ConversationEntity>
            .Update.Set(c => c.IsActive, false)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == id, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Cập nhật tin nhắn cuối cùng
    /// </summary>
    public async Task<bool> UpdateLastMessageAsync(string conversationId, LastMessage lastMessage)
    {
        var update = Builders<ConversationEntity>
            .Update.Set(c => c.LastMessage, lastMessage)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> BlockConversationAsync(string conversationId, string blockedBy)
    {
        var blockedInfo = new BlockedInfo { By = blockedBy, At = DateTime.UtcNow };

        var update = Builders<ConversationEntity>
            .Update.Set(c => c.Blocked, blockedInfo)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    public async Task<bool> UnblockConversationAsync(string conversationId)
    {
        var update = Builders<ConversationEntity>
            .Update.Unset(c => c.Blocked)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Kiểm tra cuộc hội thoại có bị chặn không
    /// </summary>
    public async Task<bool> IsConversationBlockedAsync(string conversationId)
    {
        var conversation = await _conversations
            .Find(c => c.Id == conversationId && c.Blocked != null)
            .FirstOrDefaultAsync();

        return conversation != null;
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo user ID với cursor-based pagination
    /// </summary>
    public async Task<IEnumerable<ConversationEntity>> GetByUserIdWithCursorAsync(
        string userId,
        string? before = null,
        string? after = null,
        int limit = 20
    )
    {
        // Normalize userId to uppercase for case-insensitive comparison
        var normalizedUserId = userId.ToUpperInvariant();
        var filterBuilder = Builders<ConversationEntity>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.AnyEq(c => c.Participants, normalizedUserId),
            filterBuilder.Eq(c => c.IsActive, true)
        );

        // Parse cursors if provided
        if (!string.IsNullOrEmpty(before))
        {
            try
            {
                var (timestamp, conversationId) = CursorHelper.ParseCursor(before);
                // Get conversations older than the cursor
                var beforeFilter = filterBuilder.Or(
                    filterBuilder.Lt(c => c.UpdatedAt, timestamp),
                    filterBuilder.And(
                        filterBuilder.Eq(c => c.UpdatedAt, timestamp),
                        filterBuilder.Lt(c => c.Id, conversationId)
                    )
                );
                filter = filterBuilder.And(filter, beforeFilter);
            }
            catch
            {
                throw new ArgumentException("Invalid 'before' cursor format");
            }
        }

        if (!string.IsNullOrEmpty(after))
        {
            try
            {
                var (timestamp, conversationId) = CursorHelper.ParseCursor(after);
                // Get conversations newer than the cursor
                var afterFilter = filterBuilder.Or(
                    filterBuilder.Gt(c => c.UpdatedAt, timestamp),
                    filterBuilder.And(
                        filterBuilder.Eq(c => c.UpdatedAt, timestamp),
                        filterBuilder.Gt(c => c.Id, conversationId)
                    )
                );
                filter = filterBuilder.And(filter, afterFilter);
            }
            catch
            {
                throw new ArgumentException("Invalid 'after' cursor format");
            }
        }

        // Sort by updatedAt descending (newest first) for consistent ordering
        return await _conversations
            .Find(filter)
            .SortByDescending(c => c.UpdatedAt)
            .ThenByDescending(c => c.Id)
            .Limit(limit)
            .ToListAsync();
    }
}
