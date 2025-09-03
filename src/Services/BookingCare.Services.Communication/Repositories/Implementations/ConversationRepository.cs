using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation c?a Conversation repository s? d?ng MongoDB
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<ConversationEntity> _conversations;

    public ConversationRepository(CommunicationDbContext context)
    {
        _conversations = context.Conversations;
    }

    /// <summary>
    /// L?y cu?c h?i tho?i theo ID
    /// </summary>
    public async Task<ConversationEntity?> GetByIdAsync(string id)
    {
        return await _conversations.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// L?y danh sách cu?c h?i tho?i c?a user v?i phân trang
    /// </summary>
    public async Task<IEnumerable<ConversationEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _conversations
            .Find(c => c.Participants.Contains(userId) && c.IsActive)
            .SortByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Tìm cu?c h?i tho?i gi?a 2 ng??i dùng
    /// </summary>
    public async Task<ConversationEntity?> GetConversationBetweenUsersAsync(string userId1, string userId2)
    {
        return await _conversations
            .Find(c => c.Participants.Contains(userId1) && 
                      c.Participants.Contains(userId2) && 
                      c.Participants.Count == 2 &&
                      c.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// T?o cu?c h?i tho?i m?i
    /// </summary>
    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversations.InsertOneAsync(conversation);
        return conversation;
    }

    /// <summary>
    /// C?p nh?t cu?c h?i tho?i
    /// </summary>
    public async Task<ConversationEntity> UpdateAsync(ConversationEntity conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversations.ReplaceOneAsync(c => c.Id == conversation.Id, conversation);
        return conversation;
    }

    /// <summary>
    /// Xóa cu?c h?i tho?i (soft delete)
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<ConversationEntity>.Update
            .Set(c => c.IsActive, false)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == id, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// C?p nh?t tin nh?n cu?i cùng
    /// </summary>
    public async Task<bool> UpdateLastMessageAsync(string conversationId, LastMessage lastMessage)
    {
        var update = Builders<ConversationEntity>.Update
            .Set(c => c.LastMessage, lastMessage)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Ch?n cu?c h?i tho?i
    /// </summary>
    public async Task<bool> BlockConversationAsync(string conversationId, string blockedBy)
    {
        var blockedInfo = new BlockedInfo
        {
            By = blockedBy,
            At = DateTime.UtcNow
        };

        var update = Builders<ConversationEntity>.Update
            .Set(c => c.Blocked, blockedInfo)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// B? ch?n cu?c h?i tho?i
    /// </summary>
    public async Task<bool> UnblockConversationAsync(string conversationId)
    {
        var update = Builders<ConversationEntity>.Update
            .Unset(c => c.Blocked)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Ki?m tra cu?c h?i tho?i có b? ch?n không
    /// </summary>
    public async Task<bool> IsConversationBlockedAsync(string conversationId)
    {
        var conversation = await _conversations
            .Find(c => c.Id == conversationId && c.Blocked != null)
            .FirstOrDefaultAsync();
        
        return conversation != null;
    }
}