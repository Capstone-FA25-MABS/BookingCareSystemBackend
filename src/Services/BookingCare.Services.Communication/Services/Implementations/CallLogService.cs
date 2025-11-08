using AutoMapper;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.AspNetCore.SignalR;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation of the CallLog service with BaseService
/// </summary>
public class CallLogService : BaseService, ICallLogService
{
    private readonly ICallLogRepository _callLogRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;
    private readonly IHubContext<ChatHub> _hubContext;

    public CallLogService(
        ICallLogRepository callLogRepository,
        IConversationRepository conversationRepository,
        IMapper mapper,
        IHubContext<ChatHub> hubContext,
        ILogger<CallLogService> logger
    )
        : base(logger)
    {
        _callLogRepository = callLogRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Tạo call log mới
    /// </summary>
    public async Task<CallLogResponse> CreateAsync(CreateCallLogRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting creation of call log for conversation: {ConversationId}",
                    correlationId: null,
                    args: new object[] { request.ConversationId }
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.ConversationId, nameof(request.ConversationId));
                ValidateRequiredString(request.CallerId, nameof(request.CallerId));
                ValidateRequiredString(request.ReceiverId, nameof(request.ReceiverId));

                if (request.CallerId == request.ReceiverId)
                {
                    throw new ArgumentException("CallerId and ReceiverId must not be the same");
                }

                // Kiểm tra conversation có tồn tại không
                var conversation = await _conversationRepository.GetByIdAsync(
                    request.ConversationId
                );
                if (conversation == null)
                {
                    throw new ArgumentException(
                        $"Conversation with ID {request.ConversationId} does not exist"
                    );
                }

                // Kiểm tra users có trong conversation không
                if (
                    !conversation.Participants.Contains(request.CallerId)
                    || !conversation.Participants.Contains(request.ReceiverId)
                )
                {
                    throw new UnauthorizedAccessException(
                        "Users are not authorized to perform calls in this conversation"
                    );
                }

                // Tạo entity từ request
                var callLogEntity = _mapper.Map<CallLogEntity>(request);
                var createdCallLog = await _callLogRepository.CreateAsync(callLogEntity);

                // ✅ Update LastMessage trong Conversation để call log hiển thị trong conversation list
                var lastMessage = new LastMessage
                {
                    MessageId = createdCallLog.Id,
                    Content =
                        request.Type == CallType.Video ? "📹 Cuộc gọi video" : "📞 Cuộc gọi thoại",
                    SenderId = request.CallerId,
                    CreatedAt = createdCallLog.StartedAt,
                };
                await _conversationRepository.UpdateLastMessageAsync(
                    request.ConversationId,
                    lastMessage
                );

                LogInfo(
                    "Call log created successfully with ID: {CallLogId}",
                    correlationId: null,
                    args: new object[] { createdCallLog.Id }
                );
                return _mapper.Map<CallLogResponse>(createdCallLog);
            },
            "CreateCallLog"
        );
    }

    /// <summary>
    /// Cập nhật call log
    /// </summary>
    public async Task<CallLogResponse> UpdateAsync(UpdateCallLogRequest request)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting update of call log: {CallLogId}",
                    correlationId: null,
                    args: new object[] { request.Id }
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.Id, nameof(request.Id));

                var existingCallLog = await _callLogRepository.GetByIdAsync(request.Id);
                if (existingCallLog == null)
                {
                    throw new ArgumentException($"Call log with ID {request.Id} does not exist");
                }

                // Validate duration và status logic
                if (request.Status == CallStatus.Accepted && request.Duration <= 0)
                {
                    throw new ArgumentException(
                        "Duration must be greater than 0 when status is Accepted"
                    );
                }

                if (request.Status != CallStatus.Accepted && request.Duration > 0)
                {
                    LogWarning(
                        "Duration set for a call that is not Accepted: {CallLogId}",
                        correlationId: null,
                        args: new object[] { request.Id }
                    );
                }

                // Cập nhật thông tin
                _mapper.Map(request, existingCallLog);
                var updatedCallLog = await _callLogRepository.UpdateAsync(existingCallLog);

                // 🆕 Update LastMessage in conversation
                var lastMessageContent = GetCallLogMessageContent(
                    updatedCallLog.Type,
                    updatedCallLog.Status,
                    updatedCallLog.Duration
                );
                var lastMessage = new LastMessage
                {
                    MessageId = updatedCallLog.Id,
                    Content = lastMessageContent,
                    SenderId = updatedCallLog.CallerId,
                    CreatedAt = updatedCallLog.StartedAt,
                };
                await _conversationRepository.UpdateLastMessageAsync(
                    updatedCallLog.ConversationId,
                    lastMessage
                );

                LogInfo(
                    "Updated LastMessage for conversation {ConversationId} with call status {Status}",
                    correlationId: null,
                    args: new object[] { updatedCallLog.ConversationId, updatedCallLog.Status }
                );

                // 🆕 Broadcast SignalR event to conversation participants
                var callLogResponse = _mapper.Map<CallLogResponse>(updatedCallLog);

                // Broadcast to conversation group (for users currently viewing the conversation)
                var conversationGroupName = $"conversation_{updatedCallLog.ConversationId}";
                await _hubContext
                    .Clients.Group(conversationGroupName)
                    .SendAsync("CallLogUpdated", callLogResponse);

                LogInfo(
                    "📤 Broadcast CallLogUpdated to conversation group: {GroupName}",
                    correlationId: null,
                    args: new object[] { conversationGroupName }
                );

                // 🆕 Also broadcast to individual users (in case they left conversation page during call)
                var callerGroupName = $"user_{updatedCallLog.CallerId.ToUpperInvariant()}";
                var receiverGroupName = $"user_{updatedCallLog.ReceiverId.ToUpperInvariant()}";

                await _hubContext
                    .Clients.Group(callerGroupName)
                    .SendAsync("CallLogUpdated", callLogResponse);

                await _hubContext
                    .Clients.Group(receiverGroupName)
                    .SendAsync("CallLogUpdated", callLogResponse);

                LogInfo(
                    "📤 Broadcast CallLogUpdated to users: {CallerId} and {ReceiverId}",
                    correlationId: null,
                    args: new object[] { updatedCallLog.CallerId, updatedCallLog.ReceiverId }
                );

                LogInfo(
                    "Call log updated successfully with ID: {CallLogId}",
                    correlationId: null,
                    args: new object[] { updatedCallLog.Id }
                );
                return callLogResponse;
            },
            "UpdateCallLog"
        );
    }

    /// <summary>
    /// Helper method to generate call log message content based on type, status, and duration
    /// </summary>
    private static string GetCallLogMessageContent(CallType type, CallStatus status, int duration)
    {
        var callTypeIcon = type == CallType.Video ? "📹" : "📞";
        var callTypeName = type == CallType.Video ? "Cuộc gọi video" : "Cuộc gọi thoại";

        if (status == CallStatus.Accepted && duration > 0)
        {
            var minutes = duration / 60;
            var seconds = duration % 60;
            var durationText = minutes > 0 ? $"{minutes} phút {seconds} giây" : $"{seconds} giây";
            return $"{callTypeIcon} {callTypeName} ({durationText})";
        }

        return $"{callTypeIcon} {callTypeName}";
    }

    /// <summary>
    /// Lấy call log theo ID
    /// </summary>
    public async Task<CallLogResponse?> GetByIdAsync(string id)
    {
        var callLog = await _callLogRepository.GetByIdAsync(id);
        return callLog != null ? _mapper.Map<CallLogResponse>(callLog) : null;
    }

    /// <summary>
    /// Lấy danh sách call logs theo user ID
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20
    )
    {
        var callLogs = await _callLogRepository.GetByUserIdAsync(userId, page, pageSize);
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }

    /// <summary>
    /// Lấy danh sách call logs theo conversation ID
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByConversationIdAsync(
        string conversationId,
        int page = 1,
        int pageSize = 20
    )
    {
        var callLogs = await _callLogRepository.GetByConversationIdAsync(
            conversationId,
            page,
            pageSize
        );
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }

    /// <summary>
    /// Xóa call log
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Starting deletion of call log: {CallLogId}",
                    correlationId: null,
                    args: new object[] { id }
                );

                ValidateRequiredString(id, nameof(id));

                var result = await _callLogRepository.DeleteAsync(id);
                if (result)
                {
                    LogInfo(
                        "Call log deleted successfully: {CallLogId}",
                        correlationId: null,
                        args: new object[] { id }
                    );
                }
                else
                {
                    LogWarning(
                        "Failed to delete call log: {CallLogId}",
                        correlationId: null,
                        args: new object[] { id }
                    );
                }

                return result;
            },
            "DeleteCallLog"
        );
    }

    /// <summary>
    /// Lấy thống kê cuộc gọi
    /// </summary>
    public async Task<CallStatisticsResponse> GetCallStatisticsAsync(
        GetCallStatisticsRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Fetching call statistics for user: {UserId} from {FromDate} to {ToDate}",
                    correlationId: null,
                    args: new object[] { request.UserId, request.FromDate, request.ToDate }
                );

                ValidateRequired(request, nameof(request));
                ValidateRequiredString(request.UserId, nameof(request.UserId));

                if (request.FromDate > request.ToDate)
                {
                    throw new ArgumentException("FromDate must be less than or equal to ToDate");
                }

                var statistics = await _callLogRepository.GetCallStatisticsAsync(
                    request.UserId,
                    request.FromDate,
                    request.ToDate
                );

                LogInfo(
                    "Successfully fetched call statistics for user: {UserId}",
                    correlationId: null,
                    args: new object[] { request.UserId }
                );
                return _mapper.Map<CallStatisticsResponse>(statistics);
            },
            "GetCallStatistics"
        );
    }

    /// <summary>
    /// Lấy danh sách cuộc gọi theo trạng thái
    /// </summary>
    public async Task<IEnumerable<CallLogResponse>> GetByStatusAsync(
        string userId,
        CallStatus status,
        int page = 1,
        int pageSize = 20
    )
    {
        var callLogs = await _callLogRepository.GetByStatusAsync(userId, status, page, pageSize);
        return _mapper.Map<IEnumerable<CallLogResponse>>(callLogs);
    }
}
