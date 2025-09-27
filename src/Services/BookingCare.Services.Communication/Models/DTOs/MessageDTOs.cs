using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request để tạo tin nhắn mới
/// </summary>
public class CreateMessageRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn (Text, Image, File, Video, Audio, System)
    /// Server sẽ tự động detect và override nếu cần thiết
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file đính kèm
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}

/// <summary>
/// Request để cập nhật tin nhắn
/// </summary>
public class UpdateMessageRequest
{
    /// <summary>
    /// ID của tin nhắn
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file đính kèm
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}

/// <summary>
/// Response cho tin nhắn
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// ID của tin nhắn
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Danh sách file đính kèm
    /// </summary>
    public List<MessageAttachmentResponse> Attachments { get; set; } = new();

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Trạng thái tin nhắn
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// Thời gian đọc tin nhắn
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Request cho file đính kèm
/// </summary>
public class MessageAttachmentRequest
{
    /// <summary>
    /// URL của file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// MIME type của file
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Response cho file đính kèm
/// </summary>
public class MessageAttachmentResponse
{
    /// <summary>
    /// URL của file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// MIME type của file
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Request để đánh dấu tin nhắn đã đọc
/// </summary>
public class MarkMessageAsReadRequest
{
    /// <summary>
    /// ID của tin nhắn
    /// </summary>
    public string MessageId { get; set; } = string.Empty;
}

/// <summary>
/// Request để đánh dấu tất cả tin nhắn trong conversation là đã đọc
/// </summary>
public class MarkAllMessagesAsReadRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người đọc tin nhắn
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Request để tìm kiếm tin nhắn
/// </summary>
public class SearchMessageRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Từ khóa tìm kiếm
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn cần tìm (tùy chọn)
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Trang hiện tại
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số lượng items mỗi trang
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request để tạo tin nhắn với file upload
/// </summary>
public class CreateMessageWithFilesRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string? Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn (Image, File, Video, Audio)
    /// </summary>
    public MessageType Type { get; set; } = MessageType.File;

    /// <summary>
    /// Danh sách files để upload
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}

/// <summary>
/// Request để tạo tin nhắn với attachments có sẵn
/// </summary>
public class CreateMessageWithAttachmentsRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gửi
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại tin nhắn
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách attachment URLs đã upload
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}

/// <summary>
/// Request để test SignalR
/// </summary>
public class TestSignalRRequest
{
    /// <summary>
    /// ID của conversation
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Tin nhắn test
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
