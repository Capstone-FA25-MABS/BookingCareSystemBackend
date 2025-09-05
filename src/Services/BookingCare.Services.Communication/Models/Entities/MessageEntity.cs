using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity đại diện cho một tin nhắn trong hệ thống chat
/// </summary>
public class MessageEntity
{
    /// <summary>
    /// ID duy nhất của tin nhắn
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID của cuộc hội thoại chứa tin nhắn này
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gửi tin nhắn (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận tin nhắn (Guid từ UserService được lưu dạng string, dùng cho chat 1-1)
    /// </summary>
    [BsonElement("receiverId")]
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn (Text, Image, File, Video, Audio, System)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file đính kèm
    /// </summary>
    [BsonElement("attachments")]
    public List<MessageAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Thời gian tạo tin nhắn
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật tin nhắn
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Trạng thái tin nhắn
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public MessageStatus Status { get; set; } = MessageStatus.UNREAD;

    /// <summary>
    /// Thời gian tin nhắn được đọc
    /// </summary>
    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Model cho file đính kèm trong tin nhắn
/// </summary>
public class MessageAttachment
{
    /// <summary>
    /// URL của file đính kèm
    /// </summary>
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    [BsonElement("size")]
    public long Size { get; set; }

    /// <summary>
    /// MIME type của file
    /// </summary>
    [BsonElement("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// URL thumbnail cho images/videos
    /// </summary>
    [BsonElement("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Chiều rộng (cho images/videos)
    /// </summary>
    [BsonElement("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Chiều cao (cho images/videos)
    /// </summary>
    [BsonElement("height")]
    public int? Height { get; set; }

    /// <summary>
    /// Thời lượng (cho videos/audios) tính bằng giây
    /// </summary>
    [BsonElement("duration")]
    public int? Duration { get; set; }
}