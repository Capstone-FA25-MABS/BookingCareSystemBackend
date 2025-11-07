using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Notification.Models.DTOs
{
    public class SendOtpRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public OtpPurpose Purpose { get; set; } = OtpPurpose.REGISTER;
        public string? DeviceId { get; set; } // Cho SMS
    }

    public class VerifyOtpRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Otp { get; set; } = "";
        public OtpPurpose Purpose { get; set; } = OtpPurpose.REGISTER;
    }

    public class DeviceRegistrationRequest
    {
        [Required]
        public string DeviceName { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";
    }

    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string TitleVi { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string ContentVi { get; set; } = string.Empty;
        public string ContentEn { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public string? ActionUrl { get; set; }
        public string? Icon { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    /// <summary>
    /// DTO for creating a new notification
    /// Inherits from NotificationContentBase to eliminate code duplication
    /// </summary>
    public class CreateNotificationDto : NotificationContentBase
    {
        public string UserId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
    }

    public class MarkNotificationReadDto
    {
        public string NotificationId { get; set; } = string.Empty;
    }

    public class NotificationFilterDto
    {
        public string? UserId { get; set; }
        public NotificationType? Type { get; set; }
        public bool? IsRead { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class NotificationSummaryDto
    {
        public long TotalCount { get; set; }
        public long UnreadCount { get; set; }
    }
}