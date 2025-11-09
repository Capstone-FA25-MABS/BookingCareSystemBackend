using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Data.Seeding;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller cho Communication service sử dụng BaseApiController với API versioning
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class CommunicationsController : BaseApiController
{
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly ICallLogService _callLogService;
    private readonly CommunicationDbContext _dbContext;

    public CommunicationsController(
        IMessageService messageService,
        IConversationService conversationService,
        ICallLogService callLogService,
        CommunicationDbContext dbContext
    )
    {
        _messageService = messageService;
        _conversationService = conversationService;
        _callLogService = callLogService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Health check endpoint với version info
    /// </summary>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        var healthData = new
        {
            Status = "Healthy",
            Service = "Communication",
            Version = ApiVersions.V1_0,
            Timestamp = DateTime.UtcNow,
            Features = new[]
            {
                "Real-time messaging via SignalR",
                "File upload with S3-CloudFront",
                "Voice/Video call logging",
                "Mixed timeline (messages + calls)",
                "Conversation management",
            },
        };
        return Success(healthData, "Communication service đang hoạt động bình thường");
    }

    /// <summary>
    /// Seed dữ liệu mẫu vào database (chỉ dùng trong development)
    /// </summary>
    [HttpPost("seed-data")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await CommunicationDataSeeder.SeedAsync(_dbContext);
            return Success(
                new { Message = "Dữ liệu mẫu đã được tạo thành công!" },
                "Dữ liệu mẫu đã được tạo thành công!"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi tạo dữ liệu mẫu: {ex.Message}");
        }
    }

    #region Messages

    /// <summary>
    /// Tạo tin nhắn mới
    /// </summary>
    [HttpPost("messages")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
    {
        var result = await _messageService.CreateAsync(request);
        return Created(result, "Tin nhắn đã được tạo thành công!");
    }

    /// <summary>
    /// Tạo tin nhắn với file upload (Complete Flow) - One-step upload & send
    /// </summary>
    [HttpPost("messages/with-files")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateMessageWithFiles(
        [FromForm] CreateMessageWithFilesRequest request
    )
    {
        try
        {
            // Validate request
            if (request.Type == MessageType.Text && request.Files.Any())
            {
                return BadRequest("Tin nhắn text không được có file đính kèm");
            }

            if (request.Type != MessageType.Text && !request.Files.Any())
            {
                return BadRequest($"Tin nhắn loại {request.Type} yêu cầu phải có file đính kèm");
            }

            // Use the complete file upload flow
            var result = await _messageService.CreateMessageWithFilesAsync(request);

            return Created(result, "Tin nhắn với file đã được gửi thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi tạo tin nhắn với file: {ex.Message}");
        }
    }

    /// <summary>
    /// Tạo tin nhắn với attachments đã upload sẵn
    /// </summary>
    [HttpPost("messages/with-attachments")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateMessageWithAttachments(
        [FromBody] CreateMessageWithAttachmentsRequest request
    )
    {
        try
        {
            // Validate attachments cho non-text messages
            if (
                request.Type != MessageType.Text
                && request.Type != MessageType.System
                && !request.Attachments.Any()
            )
            {
                return BadRequest($"Tin nhắn loại {request.Type} yêu cầu phải có attachments");
            }

            if (
                (request.Type == MessageType.Text || request.Type == MessageType.System)
                && request.Attachments.Any()
            )
            {
                return BadRequest($"Tin nhắn loại {request.Type} không được có attachments");
            }

            // Create message với attachments
            var result = await _messageService.CreateAsync(
                new CreateMessageRequest
                {
                    ConversationId = request.ConversationId,
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    Type = request.Type,
                    Attachments = request.Attachments,
                }
            );

            return Created(result, "Tin nhắn với attachments đã được tạo thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi tạo tin nhắn với attachments: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật tin nhắn
    /// </summary>
    [HttpPut("messages/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateMessage(
        string id,
        [FromBody] UpdateMessageRequest request
    )
    {
        if (id != request.Id)
        {
            return BadRequest("ID trong URL và request body không khớp");
        }

        var result = await _messageService.UpdateAsync(request);
        return Success(result, "Tin nhắn đã được cập nhật thành công!");
    }

    /// <summary>
    /// Lấy tin nhắn theo ID
    /// </summary>
    [HttpGet("messages/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMessage(string id)
    {
        var result = await _messageService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Tin nhắn với ID {id} không tìm thấy");
        }
        return Success(result, "Lấy tin nhắn thành công!");
    }

    /// <summary>
    /// Lấy mixed timeline (messages + call logs) cho conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMessagesByConversationId(
        string conversationId,
        [FromQuery] GetMessagesQueryParameters queryParams
    )
    {
        var request = new GetMixedTimelineRequest
        {
            ConversationId = conversationId,
            Before = queryParams.Before,
            After = queryParams.After,
            Limit = queryParams.Limit,
            MessagesOnly = queryParams.MessagesOnly,
            CallLogsOnly = queryParams.CallLogsOnly,
            CallTypeFilter = queryParams.CallTypeFilter,
            MessageTypeFilter = queryParams.MessageTypeFilter,
        };

        // 🎯 Check if user info enrichment is requested
        if (queryParams.IncludeSenderInfo || queryParams.IncludeReceiverInfo)
        {
            var messageOptions = new MessageLoadOptions
            {
                IncludeSenderInfo = queryParams.IncludeSenderInfo,
                IncludeReceiverInfo = queryParams.IncludeReceiverInfo,
                IncludeOnlineStatus = queryParams.IncludeOnlineStatus,
            };

            var result = await _messageService.GetMixedTimelineWithUserInfoAsync(
                request,
                messageOptions
            );
            return Success(result, "Lấy mixed timeline với user info thành công!");
        }
        else
        {
            var result = await _messageService.GetMixedTimelineAsync(request);
            return Success(result, "Lấy mixed timeline thành công!");
        }
    }

    /// <summary>
    /// Legacy endpoint với page-based pagination (kept for backward compatibility)
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages/paginated")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMessagesByConversationIdPaginated(
        string conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool includeSenderInfo = false, // 🎯 NEW: Include sender user info
        [FromQuery] bool includeReceiverInfo = false, // 🎯 NEW: Include receiver user info
        [FromQuery] bool includeOnlineStatus = false
    ) // 🎯 NEW: Include online status
    {
        // 🎯 Check if user info enrichment is requested
        if (includeSenderInfo || includeReceiverInfo)
        {
            var messageOptions = new MessageLoadOptions
            {
                IncludeSenderInfo = includeSenderInfo,
                IncludeReceiverInfo = includeReceiverInfo,
                IncludeOnlineStatus = includeOnlineStatus,
            };

            var result = await _messageService.GetByConversationIdWithUserInfoAsync(
                conversationId,
                page,
                pageSize,
                messageOptions
            );
            return Success(
                new
                {
                    Messages = result,
                    EnrichmentInfo = new
                    {
                        SenderInfoLoaded = includeSenderInfo,
                        ReceiverInfoLoaded = includeReceiverInfo,
                        OnlineStatusLoaded = includeOnlineStatus,
                        TotalMessages = result.Count(),
                        MessagesWithSenderInfo = result.Count(m => m.SenderInfo != null),
                        MessagesWithReceiverInfo = result.Count(m => m.ReceiverInfo != null),
                    },
                },
                "Lấy tin nhắn với user info thành công!"
            );
        }
        else
        {
            var result = await _messageService.GetByConversationIdAsync(
                conversationId,
                page,
                pageSize
            );
            return Success(result, "Lấy tin nhắn thành công!");
        }
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    [HttpDelete("messages/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        var result = await _messageService.DeleteAsync(id);
        if (!result)
        {
            return NotFound($"Tin nhắn với ID {id} không tìm thấy");
        }
        return Success(new { Deleted = true }, "Xóa tin nhắn thành công!");
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    [HttpPost("messages/mark-as-read")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> MarkMessageAsRead([FromBody] MarkMessageAsReadRequest request)
    {
        var result = await _messageService.MarkAsReadAsync(request);
        if (!result)
        {
            return BadRequest("Không thể đánh dấu tin nhắn đã đọc");
        }
        return Success(new { MarkedAsRead = true }, "Đánh dấu tin nhắn đã đọc thành công!");
    }

    /// <summary>
    /// Đánh dấu tất cả tin nhắn chưa đọc trong conversation là đã đọc
    /// </summary>
    [HttpPost("messages/mark-all-as-read")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> MarkAllMessagesAsRead(
        [FromBody] MarkAllMessagesAsReadRequest request
    )
    {
        var result = await _messageService.MarkAllAsReadAsync(request);
        if (!result)
        {
            return BadRequest(
                "Không thể đánh dấu tất cả tin nhắn là đã đọc hoặc không có tin nhắn chưa đọc"
            );
        }
        return Success(
            new { MarkedAllAsRead = true },
            "Đánh dấu tất cả tin nhắn đã đọc thành công!"
        );
    }

    /// <summary>
    /// Thu hồi tin nhắn (chỉ cho phép trong 1 giờ sau khi gửi)
    /// </summary>
    [HttpPost("messages/recall")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RecallMessage([FromBody] RecallMessageRequest request)
    {
        try
        {
            var result = await _messageService.RecallMessageAsync(request);
            if (result == null)
            {
                return BadRequest("Không thể thu hồi tin nhắn");
            }
            return Success(result, "Thu hồi tin nhắn thành công!");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi thu hồi tin nhắn: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy số tin nhắn chưa đọc trong một conversation cụ thể
    /// </summary>
    [HttpGet("conversations/{conversationId}/unread-count")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUnreadCount(
        string conversationId,
        [FromQuery] string userId
    )
    {
        var count = await _messageService.GetUnreadCountAsync(conversationId, userId);

        return Success(new { UnreadCount = count }, "Lấy số tin nhắn chưa đọc thành công!");
    }

    /// <summary>
    /// Lấy tổng số tin nhắn chưa đọc của user (across all conversations)
    /// Dùng để hiển thị badge notification trên icon chat
    /// </summary>
    [HttpGet("users/{userId}/total-unread-count")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetTotalUnreadCount(string userId)
    {
        try
        {
            var totalUnreadCount = await _messageService.GetTotalUnreadCountAsync(userId);

            return Success(
                new
                {
                    UserId = userId,
                    TotalUnreadCount = totalUnreadCount,
                    Timestamp = DateTime.UtcNow,
                },
                "Lấy tổng số tin nhắn chưa đọc thành công!"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi lấy tổng số tin nhắn chưa đọc: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy unread count chi tiết cho từng conversation của user
    /// Dùng để hiển thị badge trên danh sách conversations
    /// </summary>
    [HttpGet("users/{userId}/unread-counts-by-conversation")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetUnreadCountsByConversation(string userId)
    {
        try
        {
            var unreadCounts = await _messageService.GetUnreadCountByConversationsAsync(userId);

            var totalUnreadCount = unreadCounts.Values.Sum();

            return Success(
                new
                {
                    UserId = userId,
                    TotalUnreadCount = totalUnreadCount,
                    ConversationsWithUnreadMessages = unreadCounts.Count,
                    UnreadCountsByConversation = unreadCounts,
                    Timestamp = DateTime.UtcNow,
                },
                "Lấy chi tiết số tin nhắn chưa đọc theo conversation thành công!"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi lấy chi tiết tin nhắn chưa đọc: {ex.Message}");
        }
    }

    /// <summary>
    /// Tìm kiếm tin nhắn
    /// </summary>
    [HttpPost("messages/search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SearchMessages([FromBody] SearchMessageRequest request)
    {
        var result = await _messageService.SearchAsync(request);
        return Success(result, "Tìm kiếm tin nhắn thành công!");
    }

    /// <summary>
    /// Tạo tin nhắn text với real-time notification
    /// </summary>
    [HttpPost("messages/text")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateTextMessage([FromBody] CreateMessageRequest request)
    {
        try
        {
            if (request.Type != MessageType.Text)
            {
                return BadRequest("Endpoint này chỉ dành cho tin nhắn text");
            }

            var result = await _messageService.CreateAsync(request);

            // SignalR notification được gửi tự động trong MessageService
            return Created(result, "Tin nhắn text đã được gửi thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi tạo tin nhắn text: {ex.Message}");
        }
    }

    /// <summary>
    /// API endpoint để test SignalR connection
    /// </summary>
    [HttpPost("test-signalr")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> TestSignalR([FromBody] TestSignalRRequest request)
    {
        try
        {
            var signalRService =
                HttpContext.RequestServices.GetRequiredService<ISignalRNotificationService>();

            await signalRService.SendMessageToConversationAsync(
                request.ConversationId,
                new MessageResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = request.ConversationId,
                    SenderId = "system",
                    Content = request.Message,
                    Type = MessageType.System,
                    CreatedAt = DateTime.UtcNow,
                    Status = MessageStatus.SENT,
                }
            );

            return Success(new { Sent = true }, "Test SignalR message sent successfully!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error testing SignalR: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy tin nhắn theo loại
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages/by-type/{messageType}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMessagesByType(
        string conversationId,
        MessageType messageType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _messageService.GetMessagesByTypeAsync(
            conversationId,
            messageType,
            page,
            pageSize
        );
        return Success(result, "Lấy tin nhắn theo loại thành công!");
    }

    /// <summary>
    /// Lấy tất cả attachments trong conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/attachments")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationAttachments(
        string conversationId,
        [FromQuery] MessageType? messageType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var result = await _messageService.GetConversationAttachmentsAsync(
            conversationId,
            messageType,
            page,
            pageSize
        );
        return Success(result, "Lấy attachments thành công!");
    }

    #endregion

    #region Conversations

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    [HttpPost("conversations")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request
    )
    {
        var result = await _conversationService.CreateAsync(request);
        return Created(result, "Cuộc hội thoại đã được tạo thành công!");
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    [HttpGet("conversations/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversation(string id)
    {
        var result = await _conversationService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Cuộc hội thoại với ID {id} không tìm thấy");
        }
        return Success(result, "Lấy cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo user ID - phiên bản performance cao cho mobile
    /// </summary>
    [HttpGet("users/{userId}/conversations/mobile")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationsForMobile(
        string userId,
        [FromQuery] string? before = null, // Cursor cho mobile cũng dùng cursor-based
        [FromQuery] int limit = 10
    ) // Mobile dùng limit nhỏ hơn
    {
        // Mobile version chỉ load những thông tin cần thiết nhất
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = false,
            IncludeUnreadCount = true,
            IncludeMetadata = false,
            IncludeOnlineStatus = false,
        };

        var result = await _conversationService.GetByUserIdWithCursorAsync(
            userId,
            before,
            null,
            limit,
            options
        );

        return Success(
            new
            {
                Conversations = result.Data.Select(c => new
                {
                    c.Id,
                    c.Participants,
                    c.LastMessage,
                    c.UpdatedAt,
                    c.UnreadCount,
                    IsBlocked = c.Blocked != null,
                }),
                NextCursor = result.NextCursor,
                HasNext = result.HasNext,
                Limit = limit,
                OptimizedForMobile = true,
            },
            "Lấy cuộc hội thoại mobile thành công!"
        );
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo user ID - phiên bản đầy đủ cho web với participant enrichment
    /// </summary>
    [HttpGet("users/{userId}/conversations")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationsByUserId(
        string userId,
        [FromQuery] GetConversationsQueryParameters queryParams
    )
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = queryParams.IncludeParticipantDetails, // Enable gRPC + caching
            IncludeUnreadCount = queryParams.IncludeUnreadCount,
            IncludeMetadata = queryParams.IncludeMetadata,
            IncludeOnlineStatus = queryParams.IncludeOnlineStatus,
        };

        var result = await _conversationService.GetByUserIdWithCursorAsync(
            userId,
            queryParams.Before,
            queryParams.After,
            queryParams.Limit,
            options
        );

        // 🎯 Enhanced response với optimization info
        var totalOtherParticipantsEnriched = queryParams.IncludeParticipantDetails
            ? result.Data.Sum(c => c.ParticipantDetails?.Count ?? 0)
            : 0;

        var totalOriginalParticipants = result.Data.Sum(c => c.Participants.Count);
        var totalConversationsWithOtherParticipants = queryParams.IncludeParticipantDetails
            ? result.Data.Count(c => c.ParticipantDetails?.Any() == true)
            : 0;

        var response = new
        {
            result.Data,
            result.NextCursor,
            result.PreviousCursor,
            result.HasNext,
            result.HasPrevious,
            result.Limit,
            EnrichmentInfo = new
            {
                ParticipantDetailsLoaded = queryParams.IncludeParticipantDetails,
                UnreadCountLoaded = queryParams.IncludeUnreadCount,
                MetadataLoaded = queryParams.IncludeMetadata,
                OnlineStatusLoaded = queryParams.IncludeOnlineStatus,
                TotalConversations = result.Data.Count,

                // 🎯 OPTIMIZATION METRICS
                OptimizationMode = queryParams.IncludeParticipantDetails
                    ? "OtherParticipantsOnly"
                    : "Disabled",
                TotalOriginalParticipants = totalOriginalParticipants,
                TotalOtherParticipantsEnriched = totalOtherParticipantsEnriched,
                ConversationsWithOtherParticipants = totalConversationsWithOtherParticipants,
                PerformanceBenefit = new
                {
                    ParticipantsSkipped = queryParams.IncludeParticipantDetails
                        ? result.Data.Count
                        : 0, // Current user skipped per conversation
                    CacheCallsOptimized = queryParams.IncludeParticipantDetails,
                    NetworkCallsReduced = queryParams.IncludeParticipantDetails,
                },

                // Backward compatibility metric
                TotalParticipantsEnriched = totalOtherParticipantsEnriched // Legacy field name
                ,
            },
        };

        return Success(response, "Lấy cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Legacy endpoint với page-based pagination (kept for backward compatibility)
    /// </summary>
    [HttpGet("users/{userId}/conversations/paginated")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationsByUserIdPaginated(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeParticipantDetails = false,
        [FromQuery] bool includeUnreadCount = true,
        [FromQuery] bool includeMetadata = false,
        [FromQuery] bool includeOnlineStatus = false
    )
    {
        var options = new ConversationLoadOptions
        {
            IncludeParticipantDetails = includeParticipantDetails,
            IncludeUnreadCount = includeUnreadCount,
            IncludeMetadata = includeMetadata,
            IncludeOnlineStatus = includeOnlineStatus,
        };

        var result = await _conversationService.GetByUserIdAsync(userId, page, pageSize, options);
        return Success(result, "Lấy cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Lấy chi tiết conversation với các thông tin lazy loading
    /// </summary>
    [HttpGet("conversations/{id}/details")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationDetails(
        string id,
        [FromQuery] bool includeParticipantDetails = true,
        [FromQuery] bool includeUnreadCount = true,
        [FromQuery] bool includeMetadata = false
    )
    {
        var result = await _conversationService.GetConversationDetailsAsync(
            id,
            new ConversationLoadOptions
            {
                IncludeParticipantDetails = includeParticipantDetails,
                IncludeUnreadCount = includeUnreadCount,
                IncludeMetadata = includeMetadata,
            }
        );

        if (result == null)
        {
            return NotFound($"Cuộc hội thoại với ID {id} không tìm thấy");
        }
        return Success(result, "Lấy chi tiết cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 users
    /// </summary>
    [HttpGet("conversations/between")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetConversationBetweenUsers(
        [FromQuery] string userId1,
        [FromQuery] string userId2
    )
    {
        var result = await _conversationService.GetConversationBetweenUsersAsync(userId1, userId2);
        if (result == null)
        {
            return NotFound("Không tìm thấy cuộc hội thoại giữa 2 users này");
        }
        return Success(result, "Tìm cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Chặn cuộc hội thoại
    /// </summary>
    [HttpPost("conversations/block")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> BlockConversation([FromBody] BlockConversationRequest request)
    {
        var result = await _conversationService.BlockConversationAsync(request);
        if (!result)
        {
            return BadRequest("Không thể chặn cuộc hội thoại");
        }
        return Success(new { Blocked = true }, "Chặn cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Bỏ chặn cuộc hội thoại
    /// </summary>
    [HttpPost("conversations/unblock")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UnblockConversation(
        [FromBody] UnblockConversationRequest request
    )
    {
        var result = await _conversationService.UnblockConversationAsync(request);
        if (!result)
        {
            return BadRequest("Không thể bỏ chặn cuộc hội thoại");
        }
        return Success(new { Unblocked = true }, "Bỏ chặn cuộc hội thoại thành công!");
    }

    #endregion

    #region Call Logs

    /// <summary>
    /// Tạo call log mới
    /// </summary>
    [HttpPost("call-logs")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateCallLog([FromBody] CreateCallLogRequest request)
    {
        var result = await _callLogService.CreateAsync(request);
        return Created(result, "Call log đã được tạo thành công!");
    }

    /// <summary>
    /// Cập nhật call log
    /// </summary>
    [HttpPut("call-logs/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateCallLog(
        string id,
        [FromBody] UpdateCallLogRequest request
    )
    {
        if (id != request.Id)
        {
            return BadRequest("ID trong URL và request body không khớp");
        }

        var result = await _callLogService.UpdateAsync(request);
        return Success(result, "Call log đã được cập nhật thành công!");
    }

    /// <summary>
    /// Lấy call log theo ID
    /// </summary>
    [HttpGet("call-logs/{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetCallLog(string id)
    {
        var result = await _callLogService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Call log với ID {id} không tìm thấy");
        }
        return Success(result, "Lấy call log thành công!");
    }

    /// <summary>
    /// Lấy call logs theo user ID
    /// </summary>
    [HttpGet("users/{userId}/call-logs")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetCallLogsByUserId(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _callLogService.GetByUserIdAsync(userId, page, pageSize);
        return Success(result, "Lấy call logs thành công!");
    }

    /// <summary>
    /// Lấy thống kê cuộc gọi
    /// </summary>
    [HttpPost("call-logs/statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetCallStatistics([FromBody] GetCallStatisticsRequest request)
    {
        var result = await _callLogService.GetCallStatisticsAsync(request);
        return Success(result, "Lấy thống kê cuộc gọi thành công!");
    }

    #endregion

    /// <summary>
    /// Test kết nối database với version info
    /// </summary>
    [HttpGet("test-connection")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var conversationCount = await _dbContext.Conversations.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<Models.Entities.ConversationEntity>.Empty
            );
            var messageCount = await _dbContext.Messages.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<Models.Entities.MessageEntity>.Empty
            );
            var callLogCount = await _dbContext.CallLogs.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<Models.Entities.CallLogEntity>.Empty
            );

            var data = new
            {
                ConversationCount = conversationCount,
                MessageCount = messageCount,
                CallLogCount = callLogCount,
                DatabaseName = "BookingCare_Communication",
                ConnectionStatus = "Connected",
                Version = ApiVersions.V1_0,
                Timestamp = DateTime.UtcNow,
            };

            return Success(data, "Kết nối database thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi kết nối database: {ex.Message}");
        }
    }

    #region Cache Management

    /// <summary>
    /// Clear cache cho một account ID cụ thể
    /// </summary>
    [HttpDelete("cache/accounts/{accountId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ClearAccountCache(string accountId)
    {
        try
        {
            var participantEnrichmentService =
                HttpContext.RequestServices.GetRequiredService<IParticipantEnrichmentService>();
            await participantEnrichmentService.ClearAccountCacheAsync(accountId);

            return Success(
                new { AccountId = accountId, ClearedAt = DateTime.UtcNow },
                $"Cache đã được clear cho account {accountId}"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi clear cache cho account {accountId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear cache cho multiple account IDs
    /// </summary>
    [HttpPost("cache/accounts/clear-multiple")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ClearMultipleAccountsCache(
        [FromBody] ClearMultipleAccountsCacheRequest request
    )
    {
        try
        {
            if (request.AccountIds == null || !request.AccountIds.Any())
            {
                return BadRequest("AccountIds không được để trống");
            }

            var participantEnrichmentService =
                HttpContext.RequestServices.GetRequiredService<IParticipantEnrichmentService>();
            await participantEnrichmentService.ClearAccountCacheAsync(request.AccountIds);

            return Success(
                new
                {
                    AccountIds = request.AccountIds,
                    Count = request.AccountIds.Count(),
                    ClearedAt = DateTime.UtcNow,
                },
                $"Cache đã được clear cho {request.AccountIds.Count()} accounts"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi clear cache cho multiple accounts: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy thông tin account detail từ cache hoặc Auth Service (for testing)
    /// </summary>
    [HttpGet("cache/accounts/{accountId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAccountDetail(string accountId)
    {
        try
        {
            var participantEnrichmentService =
                HttpContext.RequestServices.GetRequiredService<IParticipantEnrichmentService>();
            var accountDetail = await participantEnrichmentService.GetAccountDetailAsync(accountId);

            if (accountDetail == null)
            {
                return NotFound($"Account với ID {accountId} không tìm thấy");
            }

            return Success(accountDetail, $"Lấy thông tin account {accountId} thành công");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi lấy thông tin account {accountId}: {ex.Message}");
        }
    }

    #endregion
}
