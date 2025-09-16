using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Services.Interfaces;

namespace BookingCare.Services.Notification.Controllers;

/// <summary>
/// Controller for OTP (One-Time Password) operations
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class OtpController : BaseApiController
{
    private readonly ILogger<OtpController> _logger;
    private readonly IOtpService _otpService;

    public OtpController(
        ILogger<OtpController> logger,
        IOtpService otpService)
    {
        _logger = logger;
        _otpService = otpService;
    }

    /// <summary>
    /// Health check endpoint for OTP service
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Success(new
        {
            Status = "Healthy",
            Service = "Notification-OTP",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send OTP to email or phone number
    /// </summary>
    /// <param name="request">OTP send request</param>
    /// <returns>Success message if OTP sent successfully</returns>
    [HttpPost("send")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Send([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data");
        }

        var response = await _otpService.SendAsync(request);
        return response ? Success("OTP sent successfully") : BadRequest("Account already exists");
    }

    /// <summary>
    /// Verify OTP code
    /// </summary>
    /// <param name="request">OTP verification request</param>
    /// <returns>Verification result with proof token</returns>
    [HttpPost("verify")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data");
        }

        var response = await _otpService.VerifyAsync(request);
        return Success(response, "OTP verified successfully");
    }
}

