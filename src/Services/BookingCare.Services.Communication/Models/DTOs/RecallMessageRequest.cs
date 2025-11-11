namespace BookingCare.Services.Communication.Models.DTOs;

/// <summary>
/// Request DTO để thu hồi tin nhắn
/// Người dùng chỉ có thể thu hồi tin nhắn của mình trong vòng 1 giờ
/// </summary>
public class RecallMessageRequest
{
    /// <summary>
    /// ID của tin nhắn cần thu hồi
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// ID của người dùng yêu cầu thu hồi (phải là người gửi tin nhắn)
    /// </summary>
    public required string UserId { get; set; }
}
