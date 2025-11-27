using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing slot hold operations
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize]
public class HoldSlotController : BaseApiController
{
    private readonly IHoldSlotService _holdSlotService;

    public HoldSlotController(IHoldSlotService holdSlotService)
    {
        _holdSlotService = holdSlotService;
    }

    /// <summary>
    /// Hold a slot for 5 minutes
    /// </summary>
    /// <param name="request">Hold slot request</param>
    /// <returns>Hold slot response with remaining time</returns>
    [HttpPost("hold")]
    public async Task<IActionResult> HoldSlot([FromBody] HoldSlotRequest request)
    {
        // Get user ID from JWT token
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

        var response = await _holdSlotService.HoldSlotAsync(request, userId);

        if (response.Success)
        {
            return Success(response, response.Message);
        }

        return BadRequest(response.Message);
    }

    /// <summary>
    /// Release a held slot
    /// </summary>
    /// <param name="request">Release slot request</param>
    /// <returns>Success status</returns>
    [HttpPost("release")]
    public async Task<IActionResult> ReleaseSlot([FromBody] ReleaseSlotRequest request)
    {
        // Get user ID from JWT token
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

        var success = await _holdSlotService.ReleaseSlotAsync(request, userId);

        if (success)
        {
            return Success("Đã hủy giữ chỗ thành công");
        }

        return BadRequest("Không thể hủy giữ chỗ");
    }

    /// <summary>
    /// Get remaining time for a held slot
    /// </summary>
    /// <param name="targetId">Target ID (Doctor or ServiceMedical)</param>
    /// <param name="targetType">Target type (0 = Doctor, 1 = ServiceMedical)</param>
    /// <param name="date">Date</param>
    /// <param name="appointmentTimeId">Appointment time ID</param>
    /// <returns>Remaining seconds</returns>
    [HttpGet("remaining-time")]
    public async Task<IActionResult> GetRemainingTime(
        [FromQuery] Guid targetId,
        [FromQuery] HoldSlotTargetType targetType,
        [FromQuery] DateOnly date,
        [FromQuery] AppointmentTime appointmentTimeId)
    {
        // Get user ID from JWT token
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

        var remainingSeconds = await _holdSlotService.GetRemainingTimeAsync(
            targetId, targetType, date, appointmentTimeId, userId);

        return Success(new { RemainingSeconds = remainingSeconds }, "Thời gian còn lại");
    }

    /// <summary>
    /// Release all held slots for current user (cleanup)
    /// </summary>
    /// <returns>Number of slots released</returns>
    [HttpPost("release-all")]
    public async Task<IActionResult> ReleaseAllSlots()
    {
        // Get user ID from JWT token
        var userId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

        var releasedCount = await _holdSlotService.ReleaseAllUserSlotsAsync(userId);

        return Success(new { ReleasedCount = releasedCount }, $"Đã hủy {releasedCount} slot đang giữ");
    }
}
