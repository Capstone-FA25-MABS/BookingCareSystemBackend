using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Data.Seeding;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Communication.Controllers;

/// <summary>
/// Controller cho Communication service s? d?ng BaseApiController
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
            "Communication service ?ang ho?t ??ng bình th??ng");
    }

    /// <summary>
    /// Seed d? li?u m?u vào database (ch? dùng trong development)
    /// </summary>
    [HttpPost("seed-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await CommunicationDataSeeder.SeedAsync(_dbContext);
            return Success(new { Message = "D? li?u m?u ?ã ???c t?o thành công!" }, "D? li?u m?u ?ã ???c t?o thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"L?i khi t?o d? li?u m?u: {ex.Message}");
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
    /// C?p nh?t tin nh?n
    /// </summary>
    [HttpPut("messages/{id}")]
    public async Task<IActionResult> UpdateMessage(string id, [FromBody] UpdateMessageRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest("ID trong URL và request body không kh?p");
        }

        var result = await _messageService.UpdateAsync(request);
        return Success(result, "Tin nh?n ?ã ???c c?p nh?t thành công!");
    }

    /// <summary>
    /// L?y tin nh?n theo ID
    /// </summary>
    [HttpGet("messages/{id}")]
    public async Task<IActionResult> GetMessage(string id)
    {
        var result = await _messageService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Tin nh?n v?i ID {id} không tìm th?y");
        }
        return Success(result, "L?y tin nh?n thành công!");
    }

    /// <summary>
    /// L?y tin nh?n theo conversation ID
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessagesByConversationId(
        string conversationId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var result = await _messageService.GetByConversationIdAsync(conversationId, page, pageSize);
        return Success(result, "L?y tin nh?n thành công!");
    }

    /// <summary>
    /// Xóa tin nh?n
    /// </summary>
    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        var result = await _messageService.DeleteAsync(id);
        if (!result)
        {
            return NotFound($"Tin nh?n v?i ID {id} không tìm th?y");
        }
        return Success(new { Deleted = true }, "Xóa tin nh?n thành công!");
    }

    /// <summary>
    /// ?ánh d?u tin nh?n ?ã ??c
    /// </summary>
    [HttpPost("messages/mark-as-read")]
    public async Task<IActionResult> MarkMessageAsRead([FromBody] MarkMessageAsReadRequest request)
    {
        var result = await _messageService.MarkAsReadAsync(request);
        if (!result)
        {
            return BadRequest("Không th? ?ánh d?u tin nh?n ?ã ??c");
        }
        return Success(new { MarkedAsRead = true }, "?ánh d?u tin nh?n ?ã ??c thành công!");
    }

    /// <summary>
    /// L?y s? tin nh?n ch?a ??c
    /// </summary>
    [HttpGet("conversations/{conversationId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(string conversationId, [FromQuery] string userId)
    {
        var count = await _messageService.GetUnreadCountAsync(conversationId, userId);
        return Success(new { UnreadCount = count }, "L?y s? tin nh?n ch?a ??c thành công!");
    }

    /// <summary>
    /// Tìm ki?m tin nh?n
    /// </summary>
    [HttpPost("messages/search")]
    public async Task<IActionResult> SearchMessages([FromBody] SearchMessageRequest request)
    {
        var result = await _messageService.SearchAsync(request);
        return Success(result, "Tìm ki?m tin nh?n thành công!");
    }

    /// <summary>
    /// T?o tin nh?n text v?i real-time notification (basic implementation)
    /// </summary>
    [HttpPost("messages/text")]
    public async Task<IActionResult> CreateTextMessage([FromBody] CreateMessageRequest request)
    {
        if (request.Type != MessageType.Text)
        {
            return BadRequest("Endpoint này ch? dành cho tin nh?n text");
        }

        var result = await _messageService.CreateAsync(request);
        return Created(result, "Tin nh?n text ?ã ???c g?i thành công!");
    }

    /// <summary>
    /// L?y tin nh?n theo lo?i
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages/by-type/{messageType}")]
    public async Task<IActionResult> GetMessagesByType(
        string conversationId, 
        MessageType messageType,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _messageService.GetMessagesByTypeAsync(conversationId, messageType, page, pageSize);
        return Success(result, "L?y tin nh?n theo lo?i thành công!");
    }

    /// <summary>
    /// L?y t?t c? attachments trong conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/attachments")]
    public async Task<IActionResult> GetConversationAttachments(
        string conversationId,
        [FromQuery] MessageType? messageType = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var result = await _messageService.GetConversationAttachmentsAsync(conversationId, messageType, page, pageSize);
        return Success(result, "L?y attachments thành công!");
    }

    #endregion

    #region Conversations

    /// <summary>
    /// T?o cu?c h?i tho?i m?i
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var result = await _conversationService.CreateAsync(request);
        return Created(result, "Cu?c h?i tho?i ?ã ???c t?o thành công!");
    }

    /// <summary>
    /// L?y cu?c h?i tho?i theo ID
    /// </summary>
    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(string id)
    {
        var result = await _conversationService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Cu?c h?i tho?i v?i ID {id} không tìm th?y");
        }
        return Success(result, "L?y cu?c h?i tho?i thành công!");
    }

    /// <summary>
    /// L?y cu?c h?i tho?i theo user ID
    /// </summary>
    [HttpGet("users/{userId}/conversations")]
    public async Task<IActionResult> GetConversationsByUserId(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _conversationService.GetByUserIdAsync(userId, page, pageSize);
        return Success(result, "L?y cu?c h?i tho?i thành công!");
    }

    /// <summary>
    /// Tìm cu?c h?i tho?i gi?a 2 users
    /// </summary>
    [HttpGet("conversations/between")]
    public async Task<IActionResult> GetConversationBetweenUsers([FromQuery] string userId1, [FromQuery] string userId2)
    {
        var result = await _conversationService.GetConversationBetweenUsersAsync(userId1, userId2);
        if (result == null)
        {
            return NotFound("Không tìm th?y cu?c h?i tho?i gi?a 2 users này");
        }
        return Success(result, "Tìm cu?c h?i tho?i thành công!");
    }

    /// <summary>
    /// Ch?n cu?c h?i tho?i
    /// </summary>
    [HttpPost("conversations/block")]
    public async Task<IActionResult> BlockConversation([FromBody] BlockConversationRequest request)
    {
        var result = await _conversationService.BlockConversationAsync(request);
        if (!result)
        {
            return BadRequest("Không th? ch?n cu?c h?i tho?i");
        }
        return Success(new { Blocked = true }, "Ch?n cu?c h?i tho?i thành công!");
    }

    /// <summary>
    /// B? ch?n cu?c h?i tho?i
    /// </summary>
    [HttpPost("conversations/unblock")]
    public async Task<IActionResult> UnblockConversation([FromBody] UnblockConversationRequest request)
    {
        var result = await _conversationService.UnblockConversationAsync(request);
        if (!result)
        {
            return BadRequest("Không th? b? ch?n cu?c h?i tho?i");
        }
        return Success(new { Unblocked = true }, "B? ch?n cu?c h?i tho?i thành công!");
    }

    #endregion

    #region Call Logs

    /// <summary>
    /// T?o call log m?i
    /// </summary>
    [HttpPost("call-logs")]
    public async Task<IActionResult> CreateCallLog([FromBody] CreateCallLogRequest request)
    {
        var result = await _callLogService.CreateAsync(request);
        return Created(result, "Call log ?ã ???c t?o thành công!");
    }

    /// <summary>
    /// C?p nh?t call log
    /// </summary>
    [HttpPut("call-logs/{id}")]
    public async Task<IActionResult> UpdateCallLog(string id, [FromBody] UpdateCallLogRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest("ID trong URL và request body không kh?p");
        }

        var result = await _callLogService.UpdateAsync(request);
        return Success(result, "Call log ?ã ???c c?p nh?t thành công!");
    }

    /// <summary>
    /// L?y call log theo ID
    /// </summary>
    [HttpGet("call-logs/{id}")]
    public async Task<IActionResult> GetCallLog(string id)
    {
        var result = await _callLogService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Call log v?i ID {id} không tìm th?y");
        }
        return Success(result, "L?y call log thành công!");
    }

    /// <summary>
    /// L?y call logs theo user ID
    /// </summary>
    [HttpGet("users/{userId}/call-logs")]
    public async Task<IActionResult> GetCallLogsByUserId(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _callLogService.GetByUserIdAsync(userId, page, pageSize);
        return Success(result, "L?y call logs thành công!");
    }

    /// <summary>
    /// L?y th?ng kê cu?c g?i
    /// </summary>
    [HttpPost("call-logs/statistics")]
    public async Task<IActionResult> GetCallStatistics([FromBody] GetCallStatisticsRequest request)
    {
        var result = await _callLogService.GetCallStatisticsAsync(request);
        return Success(result, "L?y th?ng kê cu?c g?i thành công!");
    }

    #endregion

    /// <summary>
    /// Test k?t n?i database
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

            return Success(data, "K?t n?i database thành công!");
        }
        catch (Exception ex)
        {
            return BadRequest($"L?i k?t n?i database: {ex.Message}");
        }
    }
}