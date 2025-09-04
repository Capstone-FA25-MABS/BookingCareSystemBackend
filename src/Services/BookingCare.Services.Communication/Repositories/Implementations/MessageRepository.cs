using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation c?a Message repository s? d?ng MongoDB
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly IMongoCollection<MessageEntity> _messages;

    public MessageRepository(CommunicationDbContext context)
    {
        _messages = context.Messages;
    }

    /// <summary>
    /// L?y tin nh?n theo ID
    /// </summary>
    public async Task<MessageEntity?> GetByIdAsync(string id)
    {
        return await _messages.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// L?y danh sách tin nh?n theo conversation ID v?i phân trang
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
    /// T?o tin nh?n m?i
    /// </summary>
    public async Task<MessageEntity> CreateAsync(MessageEntity message)
    {
        message.CreatedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;
        await _messages.InsertOneAsync(message);
        return message;
    }

    /// <summary>
    /// C?p nh?t tin nh?n
    /// </summary>
    public async Task<MessageEntity> UpdateAsync(MessageEntity message)
    {
        message.UpdatedAt = DateTime.UtcNow;
        await _messages.ReplaceOneAsync(m => m.Id == message.Id, message);
        return message;
    }

    /// <summary>
    /// Xóa tin nh?n
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
    /// L?y s? tin nh?n ch?a ??c theo conversation
    /// </summary>
    public async Task<long> GetUnreadCountAsync(string conversationId, string userId)
    {
        return await _messages.CountDocumentsAsync(m => 
            m.ConversationId == conversationId && 
            m.ReceiverId == userId && 
            m.Status == MessageStatus.UNREAD);
    }

    /// <summary>
    /// Tìm ki?m tin nh?n theo n?i dung
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
}