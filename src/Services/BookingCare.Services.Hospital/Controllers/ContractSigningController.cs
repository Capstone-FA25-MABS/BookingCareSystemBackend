using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Hospital.Controllers;

/// <summary>
/// Controller for contract signing operations (public endpoints for hospital representatives)
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class ContractSigningController : BaseApiController
{
    private readonly IContractSigningService _contractSigningService;

    public ContractSigningController(IContractSigningService contractSigningService)
    {
        _contractSigningService = contractSigningService;
    }

    /// <summary>
    /// Validate a contract signing token (public endpoint)
    /// </summary>
    [HttpPost("validate-token")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _contractSigningService.ValidateTokenAsync(request.Token);

        if (!result.IsValid)
        {
            return BadRequest(result);
        }

        return Success(result);
    }

    /// <summary>
    /// Send OTP for contract signing verification (public endpoint)
    /// </summary>
    [HttpPost("send-otp")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendSigningOtpRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _contractSigningService.SendSigningOtpAsync(request.Token);

        if (!result)
        {
            return BadRequest("Failed to send OTP");
        }

        return Success<object?>(null, "OTP đã được gửi đến email của bạn");
    }

    /// <summary>
    /// Sign the contract (public endpoint)
    /// </summary>
    [HttpPost("sign")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [AllowAnonymous]
    public async Task<IActionResult> SignContract(
        [FromBody] SignContractRequestDto request,
        [FromHeader(Name = "User-Agent")] string? userAgent = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Set IP address and user agent from request context
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.UserAgent = userAgent;

        var result = await _contractSigningService.SignContractAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Get signing link for a registration (Admin only)
    /// </summary>
    [HttpGet("signing-link/{registrationId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> GetSigningLink(Guid registrationId)
    {
        var link = await _contractSigningService.GetSigningLinkAsync(registrationId);
        return Success(new { signingLink = link });
    }
}
