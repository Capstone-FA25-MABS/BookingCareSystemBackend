using BookingCare.Services.Auth.Models.DTOs;

namespace BookingCare.Services.Auth.Services.Interfaces;

public interface IAuthService
{
    // Authentication methods
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}