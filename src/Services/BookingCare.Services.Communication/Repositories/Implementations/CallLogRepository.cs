using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Repositories.Implementations;

/// <summary>
/// Implementation c?a CallLog repository s? d?ng MongoDB
/// </summary>
public class CallLogRepository : ICallLogRepository
{
    private readonly IMongoCollection<CallLogEntity> _callLogs;

    public CallLogRepository(CommunicationDbContext context)
    {
        _callLogs = context.CallLogs;
    }

    /// <summary>
    /// L?y call log theo ID
    /// </summary>
    public async Task<CallLogEntity?> GetByIdAsync(string id)
    {
        return await _callLogs.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// L?y danh sách call logs theo user ID v?i phân trang
    /// </summary>
    public async Task<IEnumerable<CallLogEntity>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _callLogs
            .Find(c => c.CallerId == userId || c.ReceiverId == userId)
            .SortByDescending(c => c.StartedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// L?y danh sách call logs theo conversation ID v?i phân trang
    /// </summary>
    public async Task<IEnumerable<CallLogEntity>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _callLogs
            .Find(c => c.ConversationId == conversationId)
            .SortByDescending(c => c.StartedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// T?o call log m?i
    /// </summary>
    public async Task<CallLogEntity> CreateAsync(CallLogEntity callLog)
    {
        callLog.StartedAt = DateTime.UtcNow;
        await _callLogs.InsertOneAsync(callLog);
        return callLog;
    }

    /// <summary>
    /// C?p nh?t call log
    /// </summary>
    public async Task<CallLogEntity> UpdateAsync(CallLogEntity callLog)
    {
        await _callLogs.ReplaceOneAsync(c => c.Id == callLog.Id, callLog);
        return callLog;
    }

    /// <summary>
    /// Xóa call log
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _callLogs.DeleteOneAsync(c => c.Id == id);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// L?y th?ng kê cu?c g?i theo user trong kho?ng th?i gian
    /// </summary>
    public async Task<CallStatistics> GetCallStatisticsAsync(string userId, DateTime fromDate, DateTime toDate)
    {
        var filter = Builders<CallLogEntity>.Filter.And(
            Builders<CallLogEntity>.Filter.Or(
                Builders<CallLogEntity>.Filter.Eq(c => c.CallerId, userId),
                Builders<CallLogEntity>.Filter.Eq(c => c.ReceiverId, userId)
            ),
            Builders<CallLogEntity>.Filter.Gte(c => c.StartedAt, fromDate),
            Builders<CallLogEntity>.Filter.Lte(c => c.StartedAt, toDate)
        );

        var callLogs = await _callLogs.Find(filter).ToListAsync();

        return new CallStatistics
        {
            TotalCalls = callLogs.Count,
            AcceptedCalls = callLogs.Count(c => c.Status == CallStatus.Accepted),
            MissedCalls = callLogs.Count(c => c.Status == CallStatus.Missed),
            RejectedCalls = callLogs.Count(c => c.Status == CallStatus.Rejected),
            VideoCalls = callLogs.Count(c => c.Type == CallType.Video),
            AudioCalls = callLogs.Count(c => c.Type == CallType.Audio),
            TotalDuration = callLogs.Where(c => c.Status == CallStatus.Accepted).Sum(c => c.Duration)
        };
    }

    /// <summary>
    /// L?y danh sách cu?c g?i theo tr?ng thái v?i phân trang
    /// </summary>
    public async Task<IEnumerable<CallLogEntity>> GetByStatusAsync(string userId, CallStatus status, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _callLogs
            .Find(c => (c.CallerId == userId || c.ReceiverId == userId) && c.Status == status)
            .SortByDescending(c => c.StartedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }
}