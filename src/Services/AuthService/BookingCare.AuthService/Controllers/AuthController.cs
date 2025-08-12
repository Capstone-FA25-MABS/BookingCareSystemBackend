using Microsoft.AspNetCore.Mvc;
using BookingCare.Shared.Common.Models;
using BookingCare.AuthService.Models.DTOs;
using BookingCare.AuthService.Services;

namespace BookingCare.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(ApiResponse<RegisterResponseDto>.SuccessResult(result, "Registration successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return BadRequest(ApiResponse<RegisterResponseDto>.ErrorResult("Registration failed", new List<string> { ex.Message }));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(result, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return BadRequest(ApiResponse<LoginResponseDto>.ErrorResult("Login failed", new List<string> { ex.Message }));
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<RefreshTokenResponseDto>.SuccessResult(result, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return BadRequest(ApiResponse<RefreshTokenResponseDto>.ErrorResult("Token refresh failed", new List<string> { ex.Message }));
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request.Email);
            return Ok(ApiResponse<string>.SuccessResult("Password reset email sent", "Password reset instructions sent to your email"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password failed for email: {Email}", request.Email);
            return BadRequest(ApiResponse<string>.ErrorResult("Failed to send password reset email", new List<string> { ex.Message }));
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return Ok(ApiResponse<string>.SuccessResult("Password reset successful", "Your password has been reset successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed");
            return BadRequest(ApiResponse<string>.ErrorResult("Password reset failed", new List<string> { ex.Message }));
        }
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<string>>> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        try
        {
            await _authService.VerifyEmailAsync(request.Token);
            return Ok(ApiResponse<string>.SuccessResult("Email verified", "Your email has been verified successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed");
            return BadRequest(ApiResponse<string>.ErrorResult("Email verification failed", new List<string> { ex.Message }));
        }
    }
}
