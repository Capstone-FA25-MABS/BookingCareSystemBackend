using BookingCare.Services.Auth.Models.DTOs;
using BookingCare.Services.Auth.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Exceptions.Domain;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Auth.Controllers;

/// <summary>
/// Authentication controller - handles user authentication and authorization
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Health check endpoint - Available in all versions
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        return Ok(new { 
            Status = "Healthy", 
            Service = "Auth", 
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// User login - V1.0 (Legacy)
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication result</returns>
    [HttpPost("login")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Success(result, "Login successful");
    }
    
    /// <summary>
    /// User registration - Available in all versions
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created(result, "User registered successfully");
    }

    /// <summary>
    /// Refresh token - Available in all versions
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Success(result, "Token refreshed successfully");
    }
}