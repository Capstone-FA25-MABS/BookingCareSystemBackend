using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Data.Seeding;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller cho Communication service sử dụng BaseApiController
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
        CommunicationDbContext dbContext)
    {
        _messageService = messageService;
        _conversationService = conversationService;
        _callLogService = callLogService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Success(new { Status = "Healthy", Service = "Communication", Timestamp = DateTime.UtcNow },
            "Communication service đang hoạt động bình thường");
    }

    /// <summary>
    /// Seed dữ liệu mẫu vào database (chỉ dùng trong development)
    /// </summary>
    [HttpPost("seed-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await CommunicationDataSeeder.SeedAsync(_dbContext);
            return Success(new { Message = "Dữ liệu mẫu đã được tạo thành công!" }, "Dữ liệu mẫu đã được tạo thành công!");
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
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
    {
        var result = await _messageService.CreateAsync(request);
        return Created(result, "Tin nhắn đã được tạo thành công!");
    }

    /// <summary>
    /// Tạo tin nhắn với file upload (Complete Flow) - One-step upload & send
    /// </summary>
    [HttpPost("messages/with-files")]
    public async Task<IActionResult> CreateMessageWithFiles([FromForm] CreateMessageWithFilesRequest request)
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
    public async Task<IActionResult> CreateMessageWithAttachments([FromBody] CreateMessageWithAttachmentsRequest request)
    {
        try
        {
            // Validate attachments cho non-text messages
            if (request.Type != MessageType.Text && request.Type != MessageType.System && !request.Attachments.Any())
            {
                return BadRequest($"Tin nhắn loại {request.Type} yêu cầu phải có attachments");
            }

            if ((request.Type == MessageType.Text || request.Type == MessageType.System) && request.Attachments.Any())
            {
                return BadRequest($"Tin nhắn loại {request.Type} không được có attachments");
            }

            // Create message với attachments
            var result = await _messageService.CreateAsync(new CreateMessageRequest
            {
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                Type = request.Type,
                Attachments = request.Attachments
            });

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
    public async Task<IActionResult> UpdateMessage(string id, [FromBody] UpdateMessageRequest request)
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
    /// Lấy tin nhắn theo conversation ID
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessagesByConversationId(
        string conversationId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var result = await _messageService.GetByConversationIdAsync(conversationId, page, pageSize);
        return Success(result, "Lấy tin nhắn thành công!");
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    [HttpDelete("messages/{id}")]
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
    public async Task<IActionResult> MarkAllMessagesAsRead([FromBody] MarkAllMessagesAsReadRequest request)
    {
        var result = await _messageService.MarkAllAsReadAsync(request);
        if (!result)
        {
            return BadRequest("Không thể đánh dấu tất cả tin nhắn là đã đọc hoặc không có tin nhắn chưa đọc");
        }
        return Success(new { MarkedAllAsRead = true }, "Đánh dấu tất cả tin nhắn đã đọc thành công!");
    }

    /// <summary>
    /// Lấy số tin nhắn chưa đọc
    /// </summary>
    [HttpGet("conversations/{conversationId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(string conversationId, [FromQuery] string userId)
    {
        var count = await _messageService.GetUnreadCountAsync(conversationId, userId);

        return Success(new { UnreadCount = count }, "Lấy số tin nhắn chưa đọc thành công!");
    }

    /// <summary>
    /// Tìm kiếm tin nhắn
    /// </summary>
    [HttpPost("messages/search")]
    public async Task<IActionResult> SearchMessages([FromBody] SearchMessageRequest request)
    {
        var result = await _messageService.SearchAsync(request);
        return Success(result, "Tìm kiếm tin nhắn thành công!");
    }

    /// <summary>
    /// Tạo tin nhắn text với real-time notification
    /// </summary>
    [HttpPost("messages/text")]
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
    public async Task<IActionResult> TestSignalR([FromBody] TestSignalRRequest request)
    {
        try
        {
            var signalRService = HttpContext.RequestServices.GetRequiredService<ISignalRNotificationService>();
            
            await signalRService.SendMessageToConversationAsync(request.ConversationId, new MessageResponse
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                SenderId = "system",
                Content = request.Message,
                Type = MessageType.System,
                CreatedAt = DateTime.UtcNow,
                Status = MessageStatus.SENT
            });

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
    public async Task<IActionResult> GetMessagesByType(
        string conversationId, 
        MessageType messageType,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _messageService.GetMessagesByTypeAsync(conversationId, messageType, page, pageSize);
        return Success(result, "Lấy tin nhắn theo loại thành công!");
    }

    /// <summary>
    /// Lấy tất cả attachments trong conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/attachments")]
    public async Task<IActionResult> GetConversationAttachments(
        string conversationId,
        [FromQuery] MessageType? messageType = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var result = await _messageService.GetConversationAttachmentsAsync(conversationId, messageType, page, pageSize);
        return Success(result, "Lấy attachments thành công!");
    }

    #endregion

    #region Conversations

    /// <summary>
    /// Tạo cuộc hội thoại mới
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var result = await _conversationService.CreateAsync(request);
        return Created(result, "Cuộc hội thoại đã được tạo thành công!");
    }

    /// <summary>
    /// Lấy cuộc hội thoại theo ID
    /// </summary>
    [HttpGet("conversations/{id}")]
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
    /// Lấy cuộc hội thoại theo user ID
    /// </summary>
    [HttpGet("users/{userId}/conversations")]
    public async Task<IActionResult> GetConversationsByUserId(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _conversationService.GetByUserIdAsync(userId, page, pageSize);
        return Success(result, "Lấy cuộc hội thoại thành công!");
    }

    /// <summary>
    /// Tìm cuộc hội thoại giữa 2 users
    /// </summary>
    [HttpGet("conversations/between")]
    public async Task<IActionResult> GetConversationBetweenUsers([FromQuery] string userId1, [FromQuery] string userId2)
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
    public async Task<IActionResult> UnblockConversation([FromBody] UnblockConversationRequest request)
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
    public async Task<IActionResult> CreateCallLog([FromBody] CreateCallLogRequest request)
    {
        var result = await _callLogService.CreateAsync(request);
        return Created(result, "Call log đã được tạo thành công!");
    }

    /// <summary>
    /// Cập nhật call log
    /// </summary>
    [HttpPut("call-logs/{id}")]
    public async Task<IActionResult> UpdateCallLog(string id, [FromBody] UpdateCallLogRequest request)
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
    public async Task<IActionResult> GetCallLogsByUserId(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _callLogService.GetByUserIdAsync(userId, page, pageSize);
        return Success(result, "Lấy call logs thành công!");
    }

    /// <summary>
    /// Lấy thống kê cuộc gọi
    /// </summary>
    [HttpPost("call-logs/statistics")]
    public async Task<IActionResult> GetCallStatistics([FromBody] GetCallStatisticsRequest request)
    {
        var result = await _callLogService.GetCallStatisticsAsync(request);
        return Success(result, "Lấy thống kê cuộc gọi thành công!");
    }

    #endregion

    /// <summary>
    /// Test kết nối database
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var conversationCount = await _dbContext.Conversations.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<Models.Entities.ConversationEntity>.Empty);
            var messageCount = await _dbContext.Messages.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<Models.Entities.MessageEntity>.Empty);
            var callLogCount = await _dbContext.CallLogs.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<Models.Entities.CallLogEntity>.Empty);
            
            var data = new {
                ConversationCount = conversationCount,
                MessageCount = messageCount,
                CallLogCount = callLogCount,
                DatabaseName = "BookingCare_Communication",
                ConnectionStatus = "Connected"
            };

            return Success(data, "Kết nối database thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi kết nối database: {ex.Message}");
        }
    }
}