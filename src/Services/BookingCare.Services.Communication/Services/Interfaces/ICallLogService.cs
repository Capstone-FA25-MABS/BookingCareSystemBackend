using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho CallLog service
/// </summary>
public interface ICallLogService
{
    /// <summary>
    /// T?o call log m?i
    /// </summary>
    Task<CallLogResponse> CreateAsync(CreateCallLogRequest request);

    /// <summary>
    /// C?p nh?t call log
    /// </summary>
    Task<CallLogResponse> UpdateAsync(UpdateCallLogRequest request);

    /// <summary>
    /// L?y call log theo ID
    /// </summary>
    Task<CallLogResponse?> GetByIdAsync(string id);

    /// <summary>
    /// L?y danh sách call logs theo user ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// L?y danh sách call logs theo conversation ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Xóa call log
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// L?y th?ng kê cu?c g?i
    /// </summary>
    Task<CallStatisticsResponse> GetCallStatisticsAsync(GetCallStatisticsRequest request);

    /// <summary>
    /// L?y danh sách cu?c g?i theo tr?ng thái
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByStatusAsync(string userId, Enums.CallStatus status, int page = 1, int pageSize = 20);
}