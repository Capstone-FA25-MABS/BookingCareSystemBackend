using BookingCare.Shared.Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Notification.Models.Entities;

/// <summary>
/// Notification entity stored in MongoDB
/// Represents in-app notifications for users
/// </summary>
public class NotificationEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// User ID who receives this notification (Guid as string)
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.String)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification type (BookingConfirmation, PaymentReminder, etc.)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public NotificationType Type { get; set; } = NotificationType.General;

    /// <summary>
    /// Notification title in Vietnamese
    /// </summary>
    [BsonElement("titleVi")]
    public string TitleVi { get; set; } = string.Empty;

    /// <summary>
    /// Notification title in English
    /// </summary>
    [BsonElement("titleEn")]
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// Notification content/message in Vietnamese
    /// </summary>
    [BsonElement("contentVi")]
    public string ContentVi { get; set; } = string.Empty;

    /// <summary>
    /// Notification content/message in English
    /// </summary>
    [BsonElement("contentEn")]
    public string ContentEn { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata stored as BsonDocument for flexibility
    /// Can contain: bookingId, doctorId, appointmentDate, etc.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Action URL when user clicks the notification
    /// </summary>
    [BsonElement("actionUrl")]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Icon class or URL for the notification
    /// </summary>
    [BsonElement("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was read
    /// </summary>
    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expiration date for auto-cleanup via TTL index
    /// Default: 30 days from creation
    /// </summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    /// <summary>
    /// Priority level: High, Normal, Low, Urgent
    /// </summary>
    [BsonElement("priority")]
    [BsonRepresentation(BsonType.String)]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

