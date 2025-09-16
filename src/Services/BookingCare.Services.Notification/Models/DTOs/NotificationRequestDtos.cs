using BookingCare.Shared.Common.Enums;
using Microsoft.AspNetCore.Antiforgery;
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
}