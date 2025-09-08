using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Services;

public interface IOtpService
{
    Task<bool> SendAsync(SendOtpRequest request);
    Task<object> VerifyAsync(VerifyOtpRequest request);
}

