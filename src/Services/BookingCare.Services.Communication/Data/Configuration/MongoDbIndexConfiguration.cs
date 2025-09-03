using MongoDB.Driver;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Data;

namespace BookingCare.Services.Communication.Data.Configuration;

/// <summary>
/// Configuration cho MongoDB indexes ?? t?i ?u performance
/// </summary>
public static class MongoDbIndexConfiguration
{
    /// <summary>
    /// T?o các indexes c?n thi?t cho collections
    /// </summary>
    public static async Task CreateIndexesAsync(CommunicationDbContext context)
    {
        await CreateMessageIndexesAsync(context.Messages);
        await CreateConversationIndexesAsync(context.Conversations);
        await CreateCallLogIndexesAsync(context.CallLogs);
    }

    /// <summary>
    /// T?o indexes cho Messages collection
    /// </summary>
    private static async Task CreateMessageIndexesAsync(IMongoCollection<MessageEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<MessageEntity>>
        {
            // Index cho conversationId ?? query messages theo conversation
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Ascending(m => m.ConversationId)),
            
            // Compound index cho conversationId và createdAt ?? sort messages
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Descending(m => m.CreatedAt)),
            
            // Index cho senderId
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Ascending(m => m.SenderId)),
            
            // Index cho receiverId và status ?? query unread messages
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys
                    .Ascending(m => m.ReceiverId)
                    .Ascending(m => m.Status)),
            
            // Text index cho search functionality
            new CreateIndexModel<MessageEntity>(
                Builders<MessageEntity>.IndexKeys.Text(m => m.Content))
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }

    /// <summary>
    /// T?o indexes cho Conversations collection
    /// </summary>
    private static async Task CreateConversationIndexesAsync(IMongoCollection<ConversationEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<ConversationEntity>>
        {
            // Index cho participants ?? query conversations c?a user
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys.Ascending(c => c.Participants)),
            
            // Compound index cho participants và isActive
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys
                    .Ascending(c => c.Participants)
                    .Ascending(c => c.IsActive)),
            
            // Index cho updatedAt ?? sort conversations
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys.Descending(c => c.UpdatedAt)),
            
            // Compound index cho participants, isActive và updatedAt
            new CreateIndexModel<ConversationEntity>(
                Builders<ConversationEntity>.IndexKeys
                    .Ascending(c => c.Participants)
                    .Ascending(c => c.IsActive)
                    .Descending(c => c.UpdatedAt))
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }

    /// <summary>
    /// T?o indexes cho CallLogs collection
    /// </summary>
    private static async Task CreateCallLogIndexesAsync(IMongoCollection<CallLogEntity> collection)
    {
        var indexKeys = new List<CreateIndexModel<CallLogEntity>>
        {
            // Index cho callerId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.CallerId)),
            
            // Index cho receiverId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.ReceiverId)),
            
            // Index cho conversationId
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys.Ascending(c => c.ConversationId)),
            
            // Compound index cho callerId và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys
                    .Ascending(c => c.CallerId)
                    .Descending(c => c.StartedAt)),
            
            // Compound index cho receiverId và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys
                    .Ascending(c => c.ReceiverId)
                    .Descending(c => c.StartedAt)),
            
            // Compound index cho status và startedAt
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys
                    .Ascending(c => c.Status)
                    .Descending(c => c.StartedAt)),
            
            // Compound index cho type và startedAt ?? query theo lo?i call
            new CreateIndexModel<CallLogEntity>(
                Builders<CallLogEntity>.IndexKeys
                    .Ascending(c => c.Type)
                    .Descending(c => c.StartedAt))
        };

        await collection.Indexes.CreateManyAsync(indexKeys);
    }
}