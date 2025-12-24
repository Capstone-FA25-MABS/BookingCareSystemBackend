using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity đại diện cho lịch sử cuộc gọi
/// </summary>
public class CallLogEntity
{
    /// <summary>
    /// ID duy nhất của cuộc gọi
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID của cuộc hội thoại liên quan (ObjectId của MongoDB)
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người thực hiện cuộc gọi (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("callerId")]
    public string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận cuộc gọi (Guid từ UserService được lưu dạng string)
    /// </summary>
    [BsonElement("receiverId")]
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Thời lượng cuộc gọi (tính bằng phút)
    /// </summary>
    [BsonElement("duration")]
    public int Duration { get; set; }

    /// <summary>
    /// Loại cuộc gọi (audio hoặc video)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CallType Type { get; set; } = CallType.Audio;

    /// <summary>
    /// Thời gian bắt đầu cuộc gọi
    /// </summary>
    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian kết thúc cuộc gọi
    /// </summary>
    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Trạng thái cuộc gọi
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public CallStatus Status { get; set; } = CallStatus.Missed;
}