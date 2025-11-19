using BookingCare.Services.Communication.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Communication.Models.Entities;

/// <summary>
/// Entity đại diện cho một tag/label có thể gán cho cuộc hội thoại
/// </summary>
public class TagEntity
{
    /// <summary>
    /// ID duy nhất của tag
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID của người dùng tạo tag (Guid từ UserService được lưu dạng string)
    /// Null nếu là system tag
    /// </summary>
    [BsonElement("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Tên của tag
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về tag
    /// </summary>
    [BsonElement("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Màu sắc của tag (hex color code)
    /// </summary>
    [BsonElement("color")]
    public string Color { get; set; } = "#000000";

    /// <summary>
    /// Icon của tag (emoji hoặc icon name)
    /// </summary>
    [BsonElement("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Loại tag (Custom, System, Work, Personal, VIP, Friends, Family, Group)
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ConversationTagType Type { get; set; } = ConversationTagType.Custom;

    /// <summary>
    /// Thứ tự sắp xếp của tag (số nhỏ hơn hiển thị trước)
    /// </summary>
    [BsonElement("order")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Số lượng cuộc hội thoại sử dụng tag này
    /// </summary>
    [BsonElement("conversationCount")]
    public int ConversationCount { get; set; } = 0;

    /// <summary>
    /// Tag có được ghim không (hiển thị ở đầu danh sách)
    /// </summary>
    [BsonElement("isPinned")]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Trạng thái hoạt động của tag
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Thời gian tạo tag
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật tag
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
