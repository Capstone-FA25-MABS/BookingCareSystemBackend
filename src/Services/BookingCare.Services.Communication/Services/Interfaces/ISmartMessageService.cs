using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Smart message service that handles automatic file type detection and routing
/// </summary>
public interface ISmartMessageService
{
    /// <summary>
    /// Create message with automatic file type detection and appropriate routing
    /// </summary>
    Task<MessageResponse> CreateSmartMessageAsync(CreateSmartMessageRequest request);

    /// <summary>
    /// Create message with files - automatically detects file types and routes to appropriate storage
    /// </summary>
    Task<MessageResponse> CreateMessageWithSmartFilesAsync(CreateMessageWithSmartFilesRequest request);
}

/// <summary>
/// Smart message creation request - only requires Text or File type
/// </summary>
public class CreateSmartMessageRequest
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Sender ID
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Receiver ID (used for 1-1 chats)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Simple message type: Text or File
    /// </summary>
    public SimpleMessageType Type { get; set; } = SimpleMessageType.Text;

    /// <summary>
    /// File attachments (when Type = File)
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}

/// <summary>
/// Smart message with files request
/// </summary>
public class CreateMessageWithSmartFilesRequest
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Sender ID
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Receiver ID (used for 1-1 chats)
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Message content (optional for file messages)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Files to upload and attach
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}