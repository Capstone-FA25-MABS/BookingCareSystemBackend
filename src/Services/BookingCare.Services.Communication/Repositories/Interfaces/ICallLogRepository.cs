using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Repositories.Interfaces;

/// <summary>
/// Interface cho CallLog repository
/// </summary>
public interface ICallLogRepository
{
    /// <summary>
    /// Lấy call log theo ID
    /// </summary>
    Task<CallLogEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách call logs theo user ID
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy danh sách call logs theo conversation ID
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Tạo call log mới
    /// </summary>
    Task<CallLogEntity> CreateAsync(CallLogEntity callLog);

    /// <summary>
    /// Cập nhật call log
    /// </summary>
    Task<CallLogEntity> UpdateAsync(CallLogEntity callLog);

    /// <summary>
    /// Xóa call log
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Lấy thống kê cuộc gọi theo user
    /// </summary>
    Task<CallStatistics> GetCallStatisticsAsync(string userId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Lấy danh sách cuộc gọi theo trạng thái
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByStatusAsync(string userId, CallStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy call logs cho timeline với filter options
    /// </summary>
    Task<IEnumerable<CallLogEntity>> GetByConversationIdForTimelineAsync(string conversationId, DateTime? before = null, DateTime? after = null, int limit = 50, CallType? callTypeFilter = null);
}

/// <summary>
/// Model thống kê cuộc gọi
/// </summary>
public class CallStatistics
{
    public long TotalCalls { get; set; }
    public long AcceptedCalls { get; set; }
    public long MissedCalls { get; set; }
    public long RejectedCalls { get; set; }
    public long VideoCalls { get; set; }
    public long AudioCalls { get; set; }
    public int TotalDuration { get; set; } // Tổng thời lượng tính bằng phút
}