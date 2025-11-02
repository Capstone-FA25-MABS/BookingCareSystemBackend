using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Base interface cho timeline items (messages và call logs)
/// </summary>
public interface ITimelineItem
{
    string Id { get; set; }
    string ConversationId { get; set; }
    DateTime CreatedAt { get; set; }
    TimelineItemType ItemType { get; set; }
}

/// <summary>
/// Loại item trong timeline
/// </summary>
public enum TimelineItemType
{
    Message,
    CallLog
}

/// <summary>
/// Response cho mixed timeline (messages + call logs)
/// </summary>
public class MixedTimelineResponse
{
    /// <summary>
    /// Danh sách items trong timeline (messages + call logs)
    /// </summary>
    public List<TimelineItem> Items { get; set; } = new();

    /// <summary>
    /// Cursor để load items cũ hơn
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Cursor để load items mới hơn
    /// </summary>
    public string? PreviousCursor { get; set; }

    /// <summary>
    /// Có items cũ hơn không
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Có items mới hơn không
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Tổng số items trong response
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Limit đã request
    /// </summary>
    public int Limit { get; set; }

    // 🎯 NEW: Enrichment Info for Messages
    /// <summary>
    /// Thông tin enrichment cho messages trong timeline
    /// </summary>
    public MessageEnrichmentInfo? EnrichmentInfo { get; set; }
}

/// <summary>
/// Timeline item wrapper cho message hoặc call log
/// </summary>
public class TimelineItem : ITimelineItem
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public TimelineItemType ItemType { get; set; }

    /// <summary>
    /// Message data (null nếu ItemType = CallLog)
    /// </summary>
    public MessageResponse? Message { get; set; }

    /// <summary>
    /// Call log data (null nếu ItemType = Message)
    /// </summary>
    public CallLogResponse? CallLog { get; set; }
}

/// <summary>
/// Request để lấy mixed timeline
/// </summary>
public class GetMixedTimelineRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Cursor để load items cũ hơn
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Cursor để load items mới hơn
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Số lượng items cần load
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Chỉ lấy messages (bỏ qua call logs)
    /// </summary>
    public bool MessagesOnly { get; set; } = false;

    /// <summary>
    /// Chỉ lấy call logs (bỏ qua messages)
    /// </summary>
    public bool CallLogsOnly { get; set; } = false;

    /// <summary>
    /// Lọc theo loại call (Video/Audio)
    /// </summary>
    public CallType? CallTypeFilter { get; set; }

    /// <summary>
    /// Lọc theo loại message
    /// </summary>
    public MessageType? MessageTypeFilter { get; set; }
}

/// <summary>
/// Thông tin enrichment cho messages timeline
/// </summary>
public class MessageEnrichmentInfo
{
    /// <summary>
    /// Sender info có được load không
    /// </summary>
    public bool SenderInfoLoaded { get; set; }

    /// <summary>
    /// Receiver info có được load không  
    /// </summary>
    public bool ReceiverInfoLoaded { get; set; }

    /// <summary>
    /// Online status có được load không
    /// </summary>
    public bool OnlineStatusLoaded { get; set; }

    /// <summary>
    /// Tổng số users được enriched
    /// </summary>
    public int TotalUsersEnriched { get; set; }

    /// <summary>
    /// Tổng số messages có sender info
    /// </summary>
    public int MessagesWithSenderInfo { get; set; }

    /// <summary>
    /// Tổng số messages có receiver info
    /// </summary>
    public int MessagesWithReceiverInfo { get; set; }
}

/// <summary>
/// Query parameters cho endpoint GetMessagesByConversationId
/// </summary>
public class GetMessagesQueryParameters
{
    /// <summary>
    /// Cursor để load items cũ hơn
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Cursor để load items mới hơn
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Số lượng items cần load (default: 50)
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Chỉ lấy messages (bỏ qua call logs)
    /// </summary>
    public bool MessagesOnly { get; set; } = false;

    /// <summary>
    /// Chỉ lấy call logs (bỏ qua messages)
    /// </summary>
    public bool CallLogsOnly { get; set; } = false;

    /// <summary>
    /// Lọc theo loại call (Video/Audio)
    /// </summary>
    public CallType? CallTypeFilter { get; set; }

    /// <summary>
    /// Lọc theo loại message
    /// </summary>
    public MessageType? MessageTypeFilter { get; set; }

    /// <summary>
    /// Include sender user info
    /// </summary>
    public bool IncludeSenderInfo { get; set; } = false;

    /// <summary>
    /// Include receiver user info
    /// </summary>
    public bool IncludeReceiverInfo { get; set; } = false;

    /// <summary>
    /// Include online status
    /// </summary>
    public bool IncludeOnlineStatus { get; set; } = false;
}