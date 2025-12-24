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
    /// Chi tiết thông tin users (lazy loaded từ Auth Service)
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
/// Chi tiết participant với thông tin đầy đủ từ Auth Service
/// </summary>
public class ConversationParticipant
{
    /// <summary>
    /// Account ID (Guid string)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Họ và tên đầy đủ
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email của user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// URL avatar
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// Role của user (PATIENT, DOCTOR, STAFF, ADMIN)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái online (sẽ được populate bởi presence service nếu cần)
    /// </summary>
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// Thời gian online cuối cùng (tùy chọn)
    /// </summary>
    public DateTime? LastOnlineAt { get; set; }
}

/// <summary>
/// Response cho tin nhắn cuối cùng
/// </summary>
public class LastMessageResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response cho thông tin chặn
/// </summary>
public class BlockedInfoResponse
{
    public string By { get; set; } = string.Empty;
    public DateTime At { get; set; }
}

/// <summary>
/// Metadata bổ sung cho conversation
/// </summary>
public class ConversationMetadata
{
    public long TotalMessages { get; set; }
    public long TotalFiles { get; set; }
    public long TotalImages { get; set; }
    public DateTime? FirstMessageDate { get; set; }
    public List<string> CommonFiles { get; set; } = new();
}

/// <summary>
/// Options cho việc load dữ liệu conversation (lazy loading)
/// </summary>
public class ConversationLoadOptions
{
    /// <summary>
    /// Load chi tiết thông tin participants từ Auth Service
    /// </summary>
    public bool IncludeParticipantDetails { get; set; } = false;

    /// <summary>
    /// Load số tin nhắn chưa đọc
    /// </summary>
    public bool IncludeUnreadCount { get; set; } = false;

    /// <summary>
    /// Load metadata bổ sung (total messages, files, etc.)
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;

    /// <summary>
    /// Load online status từ presence service
    /// </summary>
    public bool IncludeOnlineStatus { get; set; } = false;
}

/// <summary>
/// Response cho cursor-based pagination (infinite scroll friendly)
/// </summary>
public class CursorPaginatedResponse<T>
{
    /// <summary>
    /// Dữ liệu cho trang hiện tại
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Cursor để load trang trước đó (older items)
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Cursor để load trang kế tiếp (newer items)
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Có trang kế tiếp không
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Có trang trước không
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Số lượng items trong trang hiện tại
    /// </summary>
    public int Count => Data.Count;

    /// <summary>
    /// Limit được request
    /// </summary>
    public int Limit { get; set; }
}

/// <summary>
/// Response cho offset-based pagination (traditional pagination)
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Danh sách items trong trang hiện tại
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Tổng số items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Số trang hiện tại
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Số items mỗi trang
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Tổng số trang
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Có trang trước không
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Có trang kế tiếp không
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;
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
    /// ID của user thực hiện chặn
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

/// <summary>
/// Request để test SignalR connection
/// </summary>
public class TestSignalRRequest
{
    /// <summary>
    /// ID của cuộc hội thoại để test
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Tin nhắn test
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Query parameters cho endpoint GetConversationsByUserId
/// </summary>
public class GetConversationsQueryParameters
{
    /// <summary>
    /// Cursor để load conversations cũ hơn
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Cursor để load conversations mới hơn
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Số lượng conversations cần load (default: 20)
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Enable participant enrichment (load participant details from Auth Service)
    /// </summary>
    public bool IncludeParticipantDetails { get; set; } = false;

    /// <summary>
    /// Include unread message count
    /// </summary>
    public bool IncludeUnreadCount { get; set; } = true;

    /// <summary>
    /// Include metadata
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;

    /// <summary>
    /// Include online status
    /// </summary>
    public bool IncludeOnlineStatus { get; set; } = false;
}
