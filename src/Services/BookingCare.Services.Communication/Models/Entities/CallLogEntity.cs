using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity ??i di?n cho l?ch s? cu?c g?i
/// </summary>
public class CallLogEntity
{
    /// <summary>
    /// ID duy nh?t c?a cu?c g?i
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a cu?c h?i tho?i liên quan (ObjectId c?a MongoDB)
    /// </summary>
    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i th?c hi?n cu?c g?i (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("callerId")]
    public string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n cu?c g?i (Guid t? UserService ???c l?u d?ng string)
    /// </summary>
    [BsonElement("receiverId")]
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Th?i l??ng cu?c g?i (tính b?ng phút)
    /// </summary>
    [BsonElement("duration")]
    public int Duration { get; set; }

    /// <summary>
    /// Lo?i cu?c g?i (audio ho?c video)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CallType Type { get; set; } = CallType.Audio;

    /// <summary>
    /// Th?i gian b?t ??u cu?c g?i
    /// </summary>
    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Th?i gian k?t thúc cu?c g?i
    /// </summary>
    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Tr?ng thái cu?c g?i
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public CallStatus Status { get; set; } = CallStatus.Missed;
}