using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using MongoDB.Driver;

namespace BookingCare.Services.Communication.Data.Configuration;

/// <summary>
/// Configuration cho MongoDB indexes để tối ưu performance
/// </summary>
public static class MongoDbIndexConfiguration
{
    /// <summary>
    /// Tạo các indexes cần thiết cho collections
    /// </summary>
    public static async Task CreateIndexesAsync(CommunicationDbContext context)
    {
        await CreateMessageIndexesAsync(context.Messages);
        await CreateConversationIndexesAsync(context.Conversations);
        await CreateCallLogIndexesAsync(context.CallLogs);
        await CreateTagIndexesAsync(context.Tags);
    }

    /// <summary>
    /// Tạo indexes cho Messages collection
    /// </summary>
    private static async Task CreateMessageIndexesAsync(IMongoCollection<MessageEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<MessageEntity>>
        {
            // Index cho conversationId để query messages theo conversation
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Ascending(m => m.ConversationId)
            ),
            // Compound index cho conversationId và createdAt để sort messages
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>
                    .IndexKeys.Ascending(m => m.ConversationId)
                    .Descending(m => m.CreatedAt)
            ),
            // Index cho senderId
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Ascending(m => m.SenderId)
            ),
            // Index cho receiverId và status để query unread messages
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>
                    .IndexKeys.Ascending(m => m.ReceiverId)
                    .Ascending(m => m.Status)
            ),
            // Text index cho search functionality
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Text(m => m.Content)
            ),
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }

    /// <summary>
    /// Tạo indexes cho Conversations collection
    /// </summary>
    private static async Task CreateConversationIndexesAsync(
        IMongoCollection<ConversationEntity> collection
    )
    {
        var indexKeys = new List<CreateIndexModel<ConversationEntity>>
        {
            // Index cho participants để query conversations của user
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys.Ascending(c => c.Participants)
            ),
            // Compound index cho participants và isActive
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>
                    .IndexKeys.Ascending(c => c.Participants)
                    .Ascending(c => c.IsActive)
            ),
            // Index cho updatedAt để sort conversations
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys.Descending(c => c.UpdatedAt)
            ),
            // Compound index cho participants, isActive và updatedAt
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>
                    .IndexKeys.Ascending(c => c.Participants)
                    .Ascending(c => c.IsActive)
                    .Descending(c => c.UpdatedAt)
            ),
            // Index cho UserTags keys để query conversations theo userId và tags
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys.Ascending("userTags")
            ),
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }

    /// <summary>
    /// Tạo indexes cho CallLogs collection
    /// </summary>
    private static async Task CreateCallLogIndexesAsync(IMongoCollection<CallLogEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<CallLogEntity>>
        {
            // Index cho callerId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.CallerId)
            ),
            // Index cho receiverId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.ReceiverId)
            ),
            // Index cho conversationId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.ConversationId)
            ),
            // Compound index cho callerId và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>
                    .IndexKeys.Ascending(c => c.CallerId)
                    .Descending(c => c.StartedAt)
            ),
            // Compound index cho receiverId và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>
                    .IndexKeys.Ascending(c => c.ReceiverId)
                    .Descending(c => c.StartedAt)
            ),
            // Compound index cho status và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>
                    .IndexKeys.Ascending(c => c.Status)
                    .Descending(c => c.StartedAt)
            ),
            // Compound index cho type và startedAt để query theo loại call
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>
                    .IndexKeys.Ascending(c => c.Type)
                    .Descending(c => c.StartedAt)
            ),
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }

    /// <summary>
    /// Tạo indexes cho Tags collection
    /// </summary>
    private static async Task CreateTagIndexesAsync(IMongoCollection<TagEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<TagEntity>>
        {
            // Index cho userId để query tags của user
            new CreateIndexModel<TagEntity>(Builders<TagEntity>.IndexKeys.Ascending(t => t.UserId)),
            // Compound index cho userId và isActive
            new CreateIndexModel<TagEntity>(
                Builders<TagEntity>.IndexKeys.Ascending(t => t.UserId).Ascending(t => t.IsActive)
            ),
            // ✅ FIXED: Partial unique index - chỉ áp dụng cho active tags
            // Điều này cho phép tạo lại tag cùng tên sau khi xóa (soft delete)
            new CreateIndexModel<TagEntity>(
                Builders<TagEntity>.IndexKeys.Ascending(t => t.UserId).Ascending(t => t.Name),
                new CreateIndexOptions<TagEntity>
                {
                    Unique = true,
                    Name = "userId_name_active_unique",
                    // Partial filter: chỉ enforce unique constraint cho tags đang active
                    PartialFilterExpression = Builders<TagEntity>.Filter.Eq(t => t.IsActive, true),
                }
            ),
            // Compound index cho userId, isPinned, order để sorting
            new CreateIndexModel<TagEntity>(
                Builders<TagEntity>
                    .IndexKeys.Ascending(t => t.UserId)
                    .Descending(t => t.IsPinned)
                    .Ascending(t => t.Order)
            ),
            // Index cho type để query system tags
            new CreateIndexModel<TagEntity>(Builders<TagEntity>.IndexKeys.Ascending(t => t.Type)),
            // Text index cho search tag by name
            new CreateIndexModel<TagEntity>(Builders<TagEntity>.IndexKeys.Text(t => t.Name)),
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }
}
