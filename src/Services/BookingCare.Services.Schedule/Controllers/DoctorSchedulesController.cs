using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Shared.Common.Helpers;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing doctor schedules
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class DoctorSchedulesController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public DoctorSchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get doctor's daily schedule
    /// </summary>
    [HttpGet("{doctorId}/daily/{date}")]
    public async Task<IActionResult> GetDoctorDailySchedule(Guid doctorId, DateOnly date)
    {
        var schedule = await _scheduleService.GetDoctorDailyScheduleAsync(doctorId, date);
        if (schedule == null)
        {
            return NotFound("No schedule found for the specified doctor and date");
        }
        return Success(schedule, "Doctor schedule retrieved successfully");
    }

    /// <summary>
    /// Get doctor's schedule for a date range
    /// </summary>
    [HttpGet("{doctorId}/range")]
    public async Task<IActionResult> GetDoctorScheduleRange(
        Guid doctorId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var request = new GetDoctorScheduleRequest
        {
            DoctorId = doctorId,
            StartDate = startDate,
            EndDate = endDate
        };

        var schedules = await _scheduleService.GetDoctorScheduleRangeAsync(request);
        return Success(schedules, "Doctor schedule range retrieved successfully");
    }

    /// <summary>
    /// Create or update doctor's daily schedule
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateDoctorDailySchedule([FromBody] CreateDoctorDailyScheduleRequest request)
    {
        var schedule = await _scheduleService.CreateOrUpdateDoctorDailyScheduleAsync(request);
        return Success(schedule, "Doctor schedule created/updated successfully");
    }

    /// <summary>
    /// Delete doctor's daily schedule
    /// </summary>
    [HttpDelete("{doctorId}/daily/{date}")]
    public async Task<IActionResult> DeleteDoctorDailySchedule(Guid doctorId, DateOnly date)
    {
        await _scheduleService.DeleteDoctorDailyScheduleAsync(doctorId, date);
        return Success<string>("Doctor daily schedule deleted successfully");
    }

    /// <summary>
    /// Get available slots for a doctor
    /// </summary>
    [HttpGet("{doctorId}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        Guid doctorId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? serviceId = null)
    {
        var request = new GetAvailableSlotsRequest
        {
            DoctorId = doctorId,
            Date = date,
            ServiceId = serviceId
        };

        // Get current user ID from JWT token if authenticated
        Guid? currentUserId = null;
        try
        {
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                currentUserId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            }
        }
        catch
        {
            // If JWT parsing fails, continue without user ID (anonymous request)
        }

        var slots = await _scheduleService.GetAvailableSlotsAsync(request, currentUserId);
        return Success(slots, "Available slots retrieved successfully");
    }

    /// <summary>
    /// List all doctor schedules with filtering (for Staff management)
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListDoctorSchedules(
        [FromQuery] ListDoctorSchedulesRequest request)
    {
        // Implementation would be in service layer
        // This endpoint allows filtering by doctorId, hospitalId, date range
        // Returns paginated results
        
        // For now, if doctorId is provided, use existing range endpoint
        if (request.DoctorId.HasValue)
        {
            var schedules = await _scheduleService.GetDoctorScheduleRangeAsync(new GetDoctorScheduleRequest
            {
                DoctorId = request.DoctorId.Value,
                StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
            });

            return Success(new
            {
                items = schedules,
                totalCount = schedules.Count(),
                pageNumber = request.PageNumber,
                pageSize = request.PageSize
            }, "Doctor schedules retrieved successfully");
        }

        return Success(new
        {
            items = new List<DoctorDailyScheduleDto>(),
            totalCount = 0,
            pageNumber = request.PageNumber,
            pageSize = request.PageSize
        }, "No doctor ID provided");
    }
}