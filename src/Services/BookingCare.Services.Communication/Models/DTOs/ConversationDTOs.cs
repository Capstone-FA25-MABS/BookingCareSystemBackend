namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request để tạo cuộc hội thoại mới
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// Danh sách ID của các thành viên
    /// </summary>
    public List<string> Participants { get; set; } = new();
}

/// <summary>
/// Response cho cuộc hội thoại
/// </summary>
public class ConversationResponse
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách thành viên
    /// </summary>
    public List<string> Participants { get; set; } = new();

    /// <summary>
    /// Tin nhắn cuối cùng
    /// </summary>
    public LastMessageResponse? LastMessage { get; set; }

    /// <summary>
    /// Thông tin chặn
    /// </summary>
    public BlockedInfoResponse? Blocked { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Response cho tin nhắn cuối cùng
/// </summary>
public class LastMessageResponse
{
    /// <summary>
    /// ID của tin nhắn
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung preview
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian gửi
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response cho thông tin chặn
/// </summary>
public class BlockedInfoResponse
{
    /// <summary>
    /// ID người thực hiện chặn
    /// </summary>
    public string By { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian chặn
    /// </summary>
    public DateTime At { get; set; }
}

/// <summary>
/// Request để chặn cuộc hội thoại
/// </summary>
public class BlockConversationRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID người thực hiện chặn
    /// </summary>
    public string BlockedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request để bỏ chặn cuộc hội thoại
/// </summary>
public class UnblockConversationRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
}