using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request ?? t?o tin nh?n m?i
/// </summary>
public class CreateMessageRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n (Text, Image, File, Video, Audio, System)
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file ?ính kèm
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}

/// <summary>
/// Request ?? c?p nh?t tin nh?n
/// </summary>
public class UpdateMessageRequest
{
    /// <summary>
    /// ID c?a tin nh?n
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách file ?ính kèm
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}

/// <summary>
/// Response cho tin nh?n
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// ID c?a tin nh?n
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Danh sách file ?ính kèm
    /// </summary>
    public List<MessageAttachmentResponse> Attachments { get; set; } = new();

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Th?i gian c?p nh?t
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Tr?ng thái tin nh?n
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// Th?i gian ??c tin nh?n
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Request cho file ?ính kèm
/// </summary>
public class MessageAttachmentRequest
{
    /// <summary>
    /// URL c?a file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích th??c file
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// MIME type c?a file
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Response cho file ?ính kèm
/// </summary>
public class MessageAttachmentResponse
{
    /// <summary>
    /// URL c?a file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tên file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kích th??c file
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// MIME type c?a file
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Request ?? ?ánh d?u tin nh?n ?ã ??c
/// </summary>
public class MarkMessageAsReadRequest
{
    /// <summary>
    /// ID c?a tin nh?n
    /// </summary>
    public string MessageId { get; set; } = string.Empty;
}

/// <summary>
/// Request ?? tìm ki?m tin nh?n
/// </summary>
public class SearchMessageRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// T? khóa tìm ki?m
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n c?n tìm (tùy ch?n)
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Trang hi?n t?i
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// S? l??ng items m?i trang
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request ?? t?o tin nh?n v?i file upload
/// </summary>
public class CreateMessageWithFilesRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n (Image, File, Video, Audio)
    /// </summary>
    public MessageType Type { get; set; } = MessageType.File;

    /// <summary>
    /// Danh sách files ?? upload
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}

/// <summary>
/// Request ?? t?o tin nh?n v?i attachments có s?n
/// </summary>
public class CreateMessageWithAttachmentsRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n (dùng cho chat 1-1)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// N?i dung tin nh?n
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i tin nh?n
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Danh sách attachment URLs ?ã upload
    /// </summary>
    public List<MessageAttachmentRequest> Attachments { get; set; } = new();
}