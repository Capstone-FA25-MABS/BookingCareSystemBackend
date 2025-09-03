using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request ?? t?o call log m?i
/// </summary>
public class CreateCallLogRequest
{
    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n
    /// </summary>
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Lo?i cu?c g?i
    /// </summary>
    public CallType Type { get; set; } = CallType.Audio;
}

/// <summary>
/// Request ?? c?p nh?t call log
/// </summary>
public class UpdateCallLogRequest
{
    /// <summary>
    /// ID c?a call log
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Th?i l??ng cu?c g?i (phút)
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Th?i gian k?t thúc
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Tr?ng thái cu?c g?i
    /// </summary>
    public CallStatus Status { get; set; }
}

/// <summary>
/// Response cho call log
/// </summary>
public class CallLogResponse
{
    /// <summary>
    /// ID c?a call log
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a cu?c h?i tho?i
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i g?i
    /// </summary>
    public string CallerId { get; set; } = string.Empty;

    /// <summary>
    /// ID c?a ng??i nh?n
    /// </summary>
    public string ReceiverId { get; set; } = string.Empty;

    /// <summary>
    /// Th?i l??ng cu?c g?i
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Lo?i cu?c g?i
    /// </summary>
    public CallType Type { get; set; }

    /// <summary>
    /// Th?i gian b?t ??u
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Th?i gian k?t thúc
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Tr?ng thái cu?c g?i
    /// </summary>
    public CallStatus Status { get; set; }
}

/// <summary>
/// Request ?? l?y th?ng kê cu?c g?i
/// </summary>
public class GetCallStatisticsRequest
{
    /// <summary>
    /// ID c?a user
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Ngày b?t ??u
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Ngày k?t thúc
    /// </summary>
    public DateTime ToDate { get; set; }
}

/// <summary>
/// Response cho th?ng kê cu?c g?i
/// </summary>
public class CallStatisticsResponse
{
    /// <summary>
    /// T?ng s? cu?c g?i
    /// </summary>
    public long TotalCalls { get; set; }

    /// <summary>
    /// S? cu?c g?i ???c ch?p nh?n
    /// </summary>
    public long AcceptedCalls { get; set; }

    /// <summary>
    /// S? cu?c g?i b? nh?
    /// </summary>
    public long MissedCalls { get; set; }

    /// <summary>
    /// S? cu?c g?i b? t? ch?i
    /// </summary>
    public long RejectedCalls { get; set; }

    /// <summary>
    /// S? cu?c g?i video
    /// </summary>
    public long VideoCalls { get; set; }

    /// <summary>
    /// S? cu?c g?i audio
    /// </summary>
    public long AudioCalls { get; set; }

    /// <summary>
    /// T?ng th?i l??ng (phút)
    /// </summary>
    public int TotalDuration { get; set; }
}