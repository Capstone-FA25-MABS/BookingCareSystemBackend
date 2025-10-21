using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface for CallLog service
/// </summary>
public interface ICallLogService
{
    /// <summary>
    /// <summary>
    /// Create a new call log
    /// </summary>
    Task<CallLogResponse> CreateAsync(CreateCallLogRequest request);

    /// <summary>
    /// <summary>
    /// Update a call log
    /// </summary>
    Task<CallLogResponse> UpdateAsync(UpdateCallLogRequest request);

    /// <summary>
    /// <summary>
    /// Get a call log by ID
    /// </summary>
    Task<CallLogResponse?> GetByIdAsync(string id);

    /// <summary>
    /// <summary>
    /// Get call logs by user ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// <summary>
    /// Get call logs by conversation ID
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20);

    /// <summary>
    /// <summary>
    /// Delete a call log
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// <summary>
    /// Get call statistics
    /// </summary>
    Task<CallStatisticsResponse> GetCallStatisticsAsync(GetCallStatisticsRequest request);

    /// <summary>
    /// <summary>
    /// Get call logs by status
    /// </summary>
    Task<IEnumerable<CallLogResponse>> GetByStatusAsync(string userId, Enums.CallStatus status, int page = 1, int pageSize = 20);
}