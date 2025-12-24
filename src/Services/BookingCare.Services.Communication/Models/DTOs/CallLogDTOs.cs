using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request để tạo call log mới
/// </summary>
public class CreateCallLogRequest
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public required string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gọi
    /// </summary>
    public required string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận
    /// </summary>
    public required string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Loại cuộc gọi
    /// </summary>
    public required CallType Type { get; set; } = CallType.Audio;
}

/// <summary>
/// Request để cập nhật call log
/// </summary>
public class UpdateCallLogRequest
{
    /// <summary>
    /// ID của call log
    /// </summary>
    public required string Id { get; set; } = string.Empty;

    /// <summary>
    /// Thời lượng cuộc gọi (phút)
    /// </summary>
    public required int Duration { get; set; }

    /// <summary>
    /// Thời gian kết thúc
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Trạng thái cuộc gọi
    /// </summary>
    public required CallStatus Status { get; set; }
}

/// <summary>
/// Response cho call log
/// </summary>
public class CallLogResponse
{
    /// <summary>
    /// ID của call log
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người gọi
    /// </summary>
    public string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID của người nhận
    /// </summary>
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Thời lượng cuộc gọi
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Loại cuộc gọi
    /// </summary>
    public CallType Type { get; set; }

    /// <summary>
    /// Thời gian bắt đầu
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Thời gian kết thúc
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Trạng thái cuộc gọi
    /// </summary>
    public CallStatus Status { get; set; }
}

/// <summary>
/// Request để lấy thống kê cuộc gọi
/// </summary>
public class GetCallStatisticsRequest
{
    /// <summary>
    /// ID của user
    /// </summary>
    public required string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Ngày bắt đầu
    /// </summary>
    public required DateTime FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc
    /// </summary>
    public required DateTime ToDate { get; set; }
}

/// <summary>
/// Response cho thống kê cuộc gọi
/// </summary>
public class CallStatisticsResponse
{
    /// <summary>
    /// Tổng số cuộc gọi
    /// </summary>
    public long TotalCalls { get; set; }

    /// <summary>
    /// Số cuộc gọi được chấp nhận
    /// </summary>
    public long AcceptedCalls { get; set; }

    /// <summary>
    /// Số cuộc gọi bị nhỡ
    /// </summary>
    public long MissedCalls { get; set; }

    /// <summary>
    /// Số cuộc gọi bị từ chối
    /// </summary>
    public long RejectedCalls { get; set; }

    /// <summary>
    /// Số cuộc gọi video
    /// </summary>
    public long VideoCalls { get; set; }

    /// <summary>
    /// Số cuộc gọi audio
    /// </summary>
    public long AudioCalls { get; set; }

    /// <summary>
    /// Tổng thời lượng (phút)
    /// </summary>
    public int TotalDuration { get; set; }
}
