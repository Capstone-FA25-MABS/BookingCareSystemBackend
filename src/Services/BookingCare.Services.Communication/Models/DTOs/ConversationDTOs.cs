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
/// Response cho cuộc hội thoại với lazy loading support
/// </summary>
public class ConversationResponse
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách thành viên (always loaded)
    /// </summary>
    public List<string> Participants { get; set; } = new();

    /// <summary>
    /// Tin nhắn cuối cùng (always loaded for conversation list)
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

    // Lazy loading properties - only loaded when requested
    /// <summary>
    /// Chi tiết thông tin users (lazy loaded)
    /// </summary>
    public List<ConversationParticipant>? ParticipantDetails { get; set; }

    /// <summary>
    /// Số tin nhắn chưa đọc (lazy loaded)
    /// </summary>
    public long? UnreadCount { get; set; }

    /// <summary>
    /// Metadata bổ sung (lazy loaded)
    /// </summary>
    public ConversationMetadata? Metadata { get; set; }
}

/// <summary>
/// Lightweight response cho danh sách conversations (no lazy loading)
/// </summary>
public class ConversationListResponse
{
    public string Id { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
    public LastMessageResponse? LastMessage { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Only essential data for list view
    public long UnreadCount { get; set; }
    public bool IsBlocked { get; set; }
}

/// <summary>
/// Chi tiết participant cho lazy loading
/// </summary>
public class ConversationParticipant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}

/// <summary>
/// Metadata bổ sung cho conversation
/// </summary>
public class ConversationMetadata
{
    public int TotalMessages { get; set; }
    public int TotalFiles { get; set; }
    public int TotalImages { get; set; }
    public DateTime? FirstMessageDate { get; set; }
    public List<string> CommonFiles { get; set; } = new();
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