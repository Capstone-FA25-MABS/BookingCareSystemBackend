using BookingCare.Services.Notification.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Notification.Models.DTOs;

namespace BookingCare.Services.Notification.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("send")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> Send([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data");
        }

        var response = await _otpService.SendAsync(request);
        return response ? Success("OTP sent successfully") : BadRequest("Failed to send OTP");
    }

    [HttpPost("verify")]
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

    // Controller intentionally thin: all logic is in OtpService
}

