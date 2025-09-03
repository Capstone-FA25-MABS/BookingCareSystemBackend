using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity ??i di?n cho m?t cu?c h?i tho?i trong h? th?ng
/// </summary>
public class ConversationEntity
{
    /// <summary>
    /// ID duy nh?t c?a cu?c h?i tho?i
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách ID c?a các thành viên tham gia cu?c h?i tho?i (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("participants")]
    public List<string> Participants { get; set; } = new();

    /// <summary>
    /// Thông tin tin nh?n cu?i cùng
    /// </summary>
    [BsonElement("lastMessage")]
    public LastMessage? LastMessage { get; set; }

    /// <summary>
    /// Thông tin ch?n cu?c h?i tho?i
    /// </summary>
    [BsonElement("blocked")]
    public BlockedInfo? Blocked { get; set; }

    /// <summary>
    /// Th?i gian t?o cu?c h?i tho?i
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Th?i gian c?p nh?t cu?c h?i tho?i
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tr?ng thái ho?t ??ng c?a cu?c h?i tho?i
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Model cho tin nh?n cu?i cùng trong cu?c h?i tho?i
/// </summary>
public class LastMessage
{
    /// <summary>
    /// ID c?a tin nh?n cu?i (ObjectId c?a MongoDB)
    /// </summary>
    [BsonElement("messageId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// N?i dung preview c?a tin nh?n cu?i
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID ng??i g?i tin nh?n cu?i (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian g?i tin nh?n cu?i
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Model cho thông tin ch?n cu?c h?i tho?i
/// </summary>
public class BlockedInfo
{
    /// <summary>
    /// ID c?a ng??i dùng th?c hi?n ch?n (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("by")]
    public string By { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian th?c hi?n ch?n
    /// </summary>
    [BsonElement("at")]
    public DateTime At { get; set; } = DateTime.UtcNow;
}