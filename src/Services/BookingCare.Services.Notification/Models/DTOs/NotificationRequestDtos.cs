using BookingCare.Shared.Common.Enums;

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
        public string DeviceName { get; set; } = "";
        public string Token { get; set; } = "";
    }
}