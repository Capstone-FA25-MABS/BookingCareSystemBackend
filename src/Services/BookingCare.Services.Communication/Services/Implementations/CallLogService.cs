using AutoMapper;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation c?a CallLog service v?i BaseService
/// </summary>
public class CallLogService : BaseService, ICallLogService
{
    private readonly ICallLogRepository _callLogRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public CallLogService(
        ICallLogRepository callLogRepository,
        IConversationRepository conversationRepository,
        IMapper mapper,
        ILogger<CallLogService> logger) : base(logger)
    {
        _callLogRepository = callLogRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// T?o call log m?i
    /// </summary>
    public async Task<CallLogResponse> CreateAsync(CreateCallLogRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u t?o call log cho conversation: {ConversationId}", null, request.ConversationId);

            // Validation
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
            ValidateRequiredString(request.CallerId, nameof(request.CallerId));
            ValidateRequiredString(request.ReceiverId, nameof(request.ReceiverId));

            if (request.CallerId == request.ReceiverId)
            {
                throw new ArgumentException("CallerId và ReceiverId không ???c gi?ng nhau");
            }

            // Ki?m tra conversation có t?n t?i không
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                throw new ArgumentException($"Conversation v?i ID {request.ConversationId} không t?n t?i");
            }

            // Ki?m tra users có trong conversation không
            if (!conversation.Participants.Contains(request.CallerId) || !conversation.Participants.Contains(request.ReceiverId))
            {
                throw new UnauthorizedAccessException("Users không có quy?n th?c hi?n cu?c g?i trong conversation này");
            }

            // T?o entity t? request
            var callLogEntity = _mapper.Map<CallLogEntity>(request);
            var createdCallLog = await _callLogRepository.CreateAsync(callLogEntity);

            LogInfo("T?o call log thành công v?i ID: {CallLogId}", null, createdCallLog.Id);
            return _mapper.Map<CallLogResponse>(createdCallLog);
        }, "CreateCallLog");
    }

    /// <summary>
    /// C?p nh?t call log
    /// </summary>
    public async Task<CallLogResponse> UpdateAsync(UpdateCallLogRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u c?p nh?t call log: {CallLogId}", null, request.Id);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Id, nameof(request.Id));

            var existingCallLog = await _callLogRepository.GetByIdAsync(request.Id);
            if (existingCallLog == null)
            {
                throw new ArgumentException($"Call log v?i ID {request.Id} không t?n t?i");
            }

            // Validate duration và status logic
            if (request.Status == CallStatus.Accepted && request.Duration <= 0)
            {
                throw new ArgumentException("Duration ph?i l?n h?n 0 khi status là Accepted");
            }

            if (request.Status != CallStatus.Accepted && request.Duration > 0)
            {
                LogWarning("Duration ???c ??t cho cu?c g?i không Accepted: {CallLogId}", null, request.Id);
            }

            // C?p nh?t thông tin
            _mapper.Map(request, existingCallLog);
            var updatedCallLog = await _callLogRepository.UpdateAsync(existingCallLog);

            LogInfo("C?p nh?t call log thành công v?i ID: {CallLogId}", null, updatedCallLog.Id);
            return _mapper.Map<CallLogResponse>(updatedCallLog);
        }, "UpdateCallLog");
    }

    /// <summary>
    /// L?y call log theo ID
    /// </summary>
    public async Task<CallLogResponse?> GetByIdAsync(string id)
    {
        var callLog = await _callLogRepository.GetByIdAsync(id);
        return callLog != null ? _mapper.Map<CallLogResponse>(callLog) : null;
    }

    /// <summary>
    /// L?y danh sách call logs theo user ID
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var callLogs = await _callLogRepository.GetByUserIdAsync(userId, page, pageSize);
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }

    /// <summary>
    /// L?y danh sách call logs theo conversation ID
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByConversationIdAsync(string conversationId, int page = 1, int pageSize = 20)
    {
        var callLogs = await _callLogRepository.GetByConversationIdAsync(conversationId, page, pageSize);
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }

    /// <summary>
    /// Xóa call log
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("B?t ??u xóa call log: {CallLogId}", null, id);

            ValidateRequiredString(id, nameof(id));

            var result = await _callLogRepository.DeleteAsync(id);
            if (result)
            {
                LogInfo("Xóa call log thành công: {CallLogId}", null, id);
            }
            else
            {
                LogWarning("Không th? xóa call log: {CallLogId}", null, id);
            }

            return result;
        }, "DeleteCallLog");
    }

    /// <summary>
    /// L?y th?ng kê cu?c g?i
    /// </summary>
    public async Task<CallStatisticsResponse> GetCallStatisticsAsync(GetCallStatisticsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("L?y th?ng kê cu?c g?i cho user: {UserId} t? {FromDate} ??n {ToDate}", 
                null, request.UserId, request.FromDate, request.ToDate);

            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.UserId, nameof(request.UserId));

            if (request.FromDate > request.ToDate)
            {
                throw new ArgumentException("FromDate ph?i nh? h?n ho?c b?ng ToDate");
            }

            var statistics = await _callLogRepository.GetCallStatisticsAsync(request.UserId, request.FromDate, request.ToDate);
            
            LogInfo("L?y th?ng kê cu?c g?i thành công cho user: {UserId}", null, request.UserId);
            return _mapper.Map<CallStatisticsResponse>(statistics);
        }, "GetCallStatistics");
    }

    /// <summary>
    /// L?y danh sách cu?c g?i theo tr?ng thái
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByStatusAsync(string userId, CallStatus status, int page = 1, int pageSize = 20)
    {
        var callLogs = await _callLogRepository.GetByStatusAsync(userId, status, page, pageSize);
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }
}