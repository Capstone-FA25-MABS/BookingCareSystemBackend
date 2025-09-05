using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho CallLog service
/// </summary>
public interface ICallLogService
{
    /// <summary>
    /// Tạo call log mới
    /// </summary>
    Task<CallLogResponse> CreateAsync(CreateCallLogRequest request);

    /// <summary>
    /// Cập nhật call log
    /// </summary>
    Task<CallLogResponse> UpdateAsync(UpdateCallLogRequest request);

    /// <summary>
    /// Lấy call log theo ID
    /// </summary>
    Task<CallLogResponse?> GetByIdAsync(string id);

    /// <summary>
    /// Lấy danh sách call logs theo user ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Lấy danh sách call logs theo conversation ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Xóa call log
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Lấy thống kê cuộc gọi
    /// </summary>
    Task<CallStatisticsResponse> GetCallStatisticsAsync(GetCallStatisticsRequest request);

    /// <summary>
    /// Lấy danh sách cuộc gọi theo trạng thái
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByStatusAsync(string userId, Enums.CallStatus status, int page = 1, int pageSize = 20);
}