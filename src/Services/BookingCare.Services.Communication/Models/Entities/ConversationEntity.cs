using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity đại diện cho một cuộc hội thoại trong hệ thống
/// </summary>
public class ConversationEntity
{
    /// <summary>
    /// ID duy nhất của cuộc hội thoại
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách ID của các thành viên tham gia cuộc hội thoại (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("participants")]
    public List<string> Participants { get; set; } = new();

    /// <summary>
    /// Thông tin tin nhắn cuối cùng
    /// </summary>
    [BsonElement("lastMessage")]
    public LastMessage? LastMessage { get; set; }

    /// <summary>
    /// Thông tin chặn cuộc hội thoại
    /// </summary>
    [BsonElement("blocked")]
    public BlockedInfo? Blocked { get; set; }

    /// <summary>
    /// Tags riêng tư của từng user cho cuộc hội thoại
    /// Key: UserId, Value: Danh sách TagId
    /// Mỗi user có thể gán tags riêng mà không ảnh hưởng đến người khác
    /// </summary>
    [BsonElement("userTags")]
    public Dictionary<string, List<string>> UserTags { get; set; } = new();

    /// <summary>
    /// Thời gian tạo cuộc hội thoại
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật cuộc hội thoại
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Trạng thái hoạt động của cuộc hội thoại
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Model cho tin nhắn cuối cùng trong cuộc hội thoại
/// </summary>
public class LastMessage
{
    /// <summary>
    /// ID của tin nhắn cuối (ObjectId của MongoDB)
    /// </summary>
    [BsonElement("messageId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung preview của tin nhắn cuối
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID người gửi tin nhắn cuối (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian gửi tin nhắn cuối
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Model cho thông tin chặn cuộc hội thoại
/// </summary>
public class BlockedInfo
{
    /// <summary>
    /// ID của người dùng thực hiện chặn (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("by")]
    public string By { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian thực hiện chặn
    /// </summary>
    [BsonElement("at")]
    public DateTime At { get; set; } = DateTime.UtcNow;
}
