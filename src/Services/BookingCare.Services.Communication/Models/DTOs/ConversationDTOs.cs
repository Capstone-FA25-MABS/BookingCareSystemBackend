namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request ?? t?o cu?c h?i tho?i m?i
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// Danh sách ID c?a các thành viên
    /// </summary>
    public List<string> Participants { get; set; } = new();
}

/// <summary>
/// Response cho cu?c h?i tho?i
/// </summary>
public class ConversationResponse
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách thành viên
    /// </summary>
    public List<string> Participants { get; set; } = new();

    /// <summary>
    /// Tin nh?n cu?i cùng
    /// </summary>
    public LastMessageResponse? LastMessage { get; set; }

    /// <summary>
    /// Thông tin ch?n
    /// </summary>
    public BlockedInfoResponse? Blocked { get; set; }

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Th?i gian c?p nh?t
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Tr?ng thái ho?t ??ng
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Response cho tin nh?n cu?i cùng
/// </summary>
public class LastMessageResponse
{
    /// <summary>
    /// ID c?a tin nh?n
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// N?i dung preview
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID ng??i g?i
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian g?i
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response cho thông tin ch?n
/// </summary>
public class BlockedInfoResponse
{
    /// <summary>
    /// ID ng??i th?c hi?n ch?n
    /// </summary>
    public string By { get; set; } = string.Empty;

    /// <summary>
    /// Th?i gian ch?n
    /// </summary>
    public DateTime At { get; set; }
}

/// <summary>
/// Request ?? ch?n cu?c h?i tho?i
/// </summary>
public class BlockConversationRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID ng??i th?c hi?n ch?n
    /// </summary>
    public string BlockedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request ?? b? ch?n cu?c h?i tho?i
/// </summary>
public class UnblockConversationRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
}