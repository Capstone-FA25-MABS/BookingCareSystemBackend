using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Services.Interfaces;

public interface IOtpService
{
    Task<bool> SendAsync(SendOtpRequest request);
    Task<object> VerifyAsync(VerifyOtpRequest request);
}

