using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho CallLog repository
/// </summary>
public interface ICallLogRepository
{
    /// <summary>
    /// L?y call log theo ID
    /// </summary>
    Task<CallLogEntity?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách call logs theo user ID
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// L?y danh sách call logs theo conversation ID
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20);

    /// <summary>
    /// T?o call log m?i
    /// </summary>
    Task<CallLogEntity> CreateAsync(CallLogEntity callLog);

    /// <summary>
    /// C?p nh?t call log
    /// </summary>
    Task<CallLogEntity> UpdateAsync(CallLogEntity callLog);

    /// <summary>
    /// Xóa call log
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// L?y th?ng kê cu?c g?i theo user
    /// </summary>
    Task<CallStatistics> GetCallStatisticsAsync(string userId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// L?y danh sách cu?c g?i theo tr?ng thái
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByStatusAsync(string userId, CallStatus status, int page = 1, int pageSize = 20);
}

/// <summary>
/// Model th?ng kê cu?c g?i
/// </summary>
public class CallStatistics
{
    public long TotalCalls { get; set; }
    public long AcceptedCalls { get; set; }
    public long MissedCalls { get; set; }
    public long RejectedCalls { get; set; }
    public long VideoCalls { get; set; }
    public long AudioCalls { get; set; }
    public int TotalDuration { get; set; } // T?ng th?i l??ng tính b?ng phút
}