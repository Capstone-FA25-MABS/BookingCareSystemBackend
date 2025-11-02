using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Utils;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation của Message repository sử dụng MongoDB
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly IMongoCollection<MessageEntity> _messages;

    public MessageRepository(CommunicationDbContext context)
    {
        _messages = context.Messages;
    }

    /// <summary>
    /// Lấy tin nhắn theo ID
    /// </summary>
    public async Task<MessageEntity?> GetByIdAsync(string id)
    {
        return await _messages.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Lấy danh sách tin nhắn theo conversation ID với phân trang
    /// </summary>
    public async Task<IEnumerable<MessageEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 50)
    {
        var skip = (page - 1) * pageSize;
        return await _messages
            .Find(m => m.ConversationId == conversationId)
            .SortByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    public async Task<MessageEntity> CreateAsync(MessageEntity message)
    {
        message.CreatedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;
        await _messages.InsertOneAsync(message);
        return message;
    }

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    public async Task<MessageEntity> UpdateAsync(MessageEntity message)
    {
        message.UpdatedAt = DateTime.UtcNow;
        await _messages.ReplaceOneAsync(m => m.Id == message.Id, message);
        return message;
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _messages.DeleteOneAsync(m => m.Id == id);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task<bool> MarkAsReadAsync(string messageId, DateTime readAt)
    {
        var update = Builders<MessageEntity>.Update
            .Set(m => m.Status, MessageStatus.READ)
            .Set(m => m.ReadAt, readAt)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        var result = await _messages.UpdateOneAsync(m => m.Id == messageId, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc của user trong conversation là đã đọc
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(string conversationId, string userId, DateTime readAt)
    {
        var filter = Builders<MessageEntity>.Filter.And(
            Builders<MessageEntity>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<MessageEntity>.Filter.Eq(m => m.ReceiverId, userId),
            Builders<MessageEntity>.Filter.Eq(m => m.Status, MessageStatus.UNREAD)
        );

        var update = Builders<MessageEntity>.Update
            .Set(m => m.Status, MessageStatus.READ)
            .Set(m => m.ReadAt, readAt)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        var result = await _messages.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Lấy số tin nhắn chưa đọc theo conversation
    /// </summary>
    public async Task<long> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _messages.CountDocumentsAsync(m =>
            m.ConversationId == conversationId &&
            m.ReceiverId == userId &&
            m.Status == MessageStatus.UNREAD);
    }

    /// <summary>
    /// Tìm kiếm tin nhắn theo nội dung
    /// </summary>
    public async Task<IEnumerable<MessageEntity>> SearchAsync(string conversationId, string searchTerm, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        var filter = Builders<MessageEntity>.Filter.And(
            Builders<MessageEntity>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<MessageEntity>.Filter.Text(searchTerm)
        );

        return await _messages
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }


    /// <summary>
    /// Lấy tin nhắn cho timeline với filter options
    /// </summary>
    public async Task<IEnumerable<MessageEntity>> GetByConversationIdForTimelineAsync(string conversationId, DateTime? before = null, DateTime? after = null, int limit = 50, MessageType? messageTypeFilter = null)
    {
        var filterBuilder = Builders<MessageEntity>.Filter;
        var filter = filterBuilder.Eq(m => m.ConversationId, conversationId);

        // Apply message type filter
        if (messageTypeFilter.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(m => m.Type, messageTypeFilter.Value));
        }

        // Apply time range filters
        if (before.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Lt(m => m.CreatedAt, before.Value));
        }

        if (after.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Gt(m => m.CreatedAt, after.Value));
        }

        return await _messages
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Limit(limit)
            .ToListAsync();
    }
}