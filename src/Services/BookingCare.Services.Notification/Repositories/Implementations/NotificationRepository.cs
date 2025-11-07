using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Models.Entities;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Setting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace BookingCare.Services.Notification.Repositories.Implementations;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<NotificationEntity> _notifications;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        IOptions<MongoDbSettings> mongoDbSettings,
        ILogger<NotificationRepository> logger)
    {
        _logger = logger;
        var settings = mongoDbSettings.Value;
        var mongoClient = new MongoClient(settings.ConnectionString);
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _notifications = database.GetCollection<NotificationEntity>(settings.NotificationsCollectionName);

        // Create indexes
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // Compound index for userId + createdAt (for pagination)
            var userIdCreatedAtIndex = Builders<NotificationEntity>.IndexKeys
                .Ascending(n => n.UserId)
                .Descending(n => n.CreatedAt);
            _notifications.Indexes.CreateOne(new CreateIndexModel<NotificationEntity>(userIdCreatedAtIndex));

            // Compound index for userId + isRead (for filtering unread)
            var userIdIsReadIndex = Builders<NotificationEntity>.IndexKeys
                .Ascending(n => n.UserId)
                .Ascending(n => n.IsRead);
            _notifications.Indexes.CreateOne(new CreateIndexModel<NotificationEntity>(userIdIsReadIndex));

            // TTL index for auto-cleanup
            var expiresAtIndex = Builders<NotificationEntity>.IndexKeys.Ascending(n => n.ExpiresAt);
            var ttlIndexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            _notifications.Indexes.CreateOne(new CreateIndexModel<NotificationEntity>(expiresAtIndex, ttlIndexOptions));

            _logger.LogInformation("MongoDB indexes created successfully for notifications collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes for notifications collection. They may already exist.");
        }
    }

    public async Task<NotificationEntity> CreateAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        await _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
        return notification;
    }

    public async Task<NotificationEntity?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        // Support both ObjectId binary (from C# InsertAsync) and String (from manual insert)
        // Try ObjectId first (production case), fallback to string (test data)
        if (ObjectId.TryParse(notificationId, out var objectId))
        {
            var objectIdFilter = new BsonDocument("_id", objectId);
            var result = await _notifications.Find(objectIdFilter).FirstOrDefaultAsync(cancellationToken);

            if (result != null)
                return result;
        }

        // Fallback: try string filter for manually inserted test data
        var stringFilter = new BsonDocument("_id", notificationId);
        return await _notifications.Find(stringFilter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<NotificationEntity>> GetUserNotificationsAsync(
        string userId,
        int skip,
        int limit,
        bool? isRead = null,
        NotificationType? notificationType = null,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<NotificationEntity>.Filter;
        var filter = filterBuilder.Eq(n => n.UserId, userId);

        if (isRead.HasValue)
        {
            filter &= filterBuilder.Eq(n => n.IsRead, isRead.Value);
        }

        if (notificationType.HasValue)
        {
            filter &= filterBuilder.Eq(n => n.Type, notificationType.Value);
        }

        return await _notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        // Support both ObjectId binary (from C# InsertAsync) and String (from manual insert)
        var update = Builders<NotificationEntity>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow);

        // Try ObjectId first (production case), fallback to string (test data)
        if (ObjectId.TryParse(notificationId, out var objectId))
        {
            var objectIdFilter = new BsonDocument("_id", objectId);
            var result = await _notifications.UpdateOneAsync(
                objectIdFilter,
                update,
                cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
                return true;
        }

        // Fallback: try string filter for manually inserted test data
        var stringFilter = new BsonDocument("_id", notificationId);
        var fallbackResult = await _notifications.UpdateOneAsync(
            stringFilter,
            update,
            cancellationToken: cancellationToken);

        return fallbackResult.ModifiedCount > 0;
    }

    public async Task<long> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var update = Builders<NotificationEntity>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow);

        var result = await _notifications.UpdateManyAsync(
            n => n.UserId == userId && !n.IsRead,
            update,
            cancellationToken: cancellationToken);

        return result.ModifiedCount;
    }

    public async Task<bool> DeleteAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        // Support both ObjectId binary (from C# InsertAsync) and String (from manual insert)
        // Try ObjectId first (production case), fallback to string (test data)
        if (ObjectId.TryParse(notificationId, out var objectId))
        {
            var objectIdFilter = new BsonDocument("_id", objectId);
            var result = await _notifications.DeleteOneAsync(
                objectIdFilter,
                cancellationToken);

            if (result.DeletedCount > 0)
                return true;
        }

        // Fallback: try string filter for manually inserted test data
        var stringFilter = new BsonDocument("_id", notificationId);
        var fallbackResult = await _notifications.DeleteOneAsync(
            stringFilter,
            cancellationToken);

        return fallbackResult.DeletedCount > 0;
    }

    public async Task<long> DeleteAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await _notifications.DeleteManyAsync(
            n => n.UserId == userId,
            cancellationToken);

        return result.DeletedCount;
    }

    public async Task<long> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _notifications.CountDocumentsAsync(
            n => n.UserId == userId && !n.IsRead,
            cancellationToken: cancellationToken);
    }

    public async Task<long> GetTotalCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _notifications.CountDocumentsAsync(
            n => n.UserId == userId,
            cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<NotificationType, long>> GetCountsByTypeAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default)
    {
        // Build match condition
        var matchCondition = new BsonDocument("userId", userId);

        // Add isRead filter if specified
        if (isRead.HasValue)
        {
            matchCondition.Add("isRead", isRead.Value);
        }

        var pipeline = new[]
        {
            new BsonDocument("$match", matchCondition),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$type" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var cursor = await _notifications.AggregateAsync(pipeline, cancellationToken: cancellationToken);
        var aggregationResult = await cursor.ToListAsync(cancellationToken);

        var result = new Dictionary<NotificationType, long>();

        foreach (var doc in aggregationResult)
        {
            var typeValue = doc["_id"].AsString;
            if (Enum.TryParse<NotificationType>(typeValue, out var notificationType))
            {
                result[notificationType] = doc["count"].ToInt64();
            }
        }

        return result;
    }
}

