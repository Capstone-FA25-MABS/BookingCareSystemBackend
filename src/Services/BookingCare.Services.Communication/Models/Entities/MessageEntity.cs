using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity ??i di?n cho m?t tin nh?n trong h? th?ng chat
/// </summary>
public class MessageEntity
{
    /// <summary>
    /// ID duy nh?t c?a tin nh?n
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a cu?c h?i tho?i ch?a tin nh?n này
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i tin nh?n (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n tin nh?n (Guid t? UserService ???c l?u d?ng string, dùng cho chat 1-1)
    /// </summary>
    [BsonElement("receiverId")]
    public string? ReceiverId { get; set; }

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n (Text, Image, File, Video, Audio, System)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file ?ính kèm
    /// </summary>
    [BsonElement("attachments")]
    public List<MessageAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Th?i gian t?o tin nh?n
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Th?i gian c?p nh?t tin nh?n
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tr?ng thái tin nh?n
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public MessageStatus Status { get; set; } = MessageStatus.UNREAD;

    /// <summary>
    /// Th?i gian tin nh?n ???c ??c
    /// </summary>
    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Model cho file ?ính kèm trong tin nh?n
/// </summary>
public class MessageAttachment
{
    /// <summary>
    /// URL c?a file ?ính kèm
    /// </summary>
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích th??c file (bytes)
    /// </summary>
    [BsonElement("size")]
    public long Size { get; set; }

    /// <summary>
    /// MIME type c?a file
    /// </summary>
    [BsonElement("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// URL thumbnail cho images/videos
    /// </summary>
    [BsonElement("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Chi?u r?ng (cho images/videos)
    /// </summary>
    [BsonElement("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Chi?u cao (cho images/videos)
    /// </summary>
    [BsonElement("height")]
    public int? Height { get; set; }

    /// <summary>
    /// Th?i l??ng (cho videos/audios) tính b?ng giây
    /// </summary>
    [BsonElement("duration")]
    public int? Duration { get; set; }
}