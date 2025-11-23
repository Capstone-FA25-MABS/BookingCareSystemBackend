using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing service medical schedules
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class ServiceMedicalSchedulesController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public ServiceMedicalSchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get service medical's daily schedule
    /// </summary>
    [HttpGet("{serviceMedicalId}/daily/{date}")]
    public async Task<IActionResult> GetServiceMedicalDailySchedule(Guid serviceMedicalId, DateOnly date)
    {
        var schedule = await _scheduleService.GetServiceMedicalDailyScheduleAsync(serviceMedicalId, date);
        if (schedule == null)
        {
            return NotFound("No schedule found for the specified service medical and date");
        }
        return Success(schedule, "Service medical schedule retrieved successfully");
    }

    /// <summary>
    /// Get service medical's schedule for a date range
    /// </summary>
    [HttpGet("{serviceMedicalId}/range")]
    public async Task<IActionResult> GetServiceMedicalScheduleRange(
        Guid serviceMedicalId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var request = new GetServiceMedicalScheduleRequest
        {
            ServiceMedicalId = serviceMedicalId,
            StartDate = startDate,
            EndDate = endDate
        };

        var schedules = await _scheduleService.GetServiceMedicalScheduleRangeAsync(request);
        return Success(schedules, "Service medical schedule range retrieved successfully");
    }

    /// <summary>
    /// Create or update service medical's daily schedule
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateServiceMedicalDailySchedule([FromBody] CreateServiceMedicalDailyScheduleRequest request)
    {
        var schedule = await _scheduleService.CreateOrUpdateServiceMedicalDailyScheduleAsync(request);
        return Success(schedule, "Service medical schedule created/updated successfully");
    }

    /// <summary>
    /// Delete service medical's daily schedule
    /// </summary>
    [HttpDelete("{serviceMedicalId}/daily/{date}")]
    public async Task<IActionResult> DeleteServiceMedicalDailySchedule(Guid serviceMedicalId, DateOnly date)
    {
        await _scheduleService.DeleteServiceMedicalDailyScheduleAsync(serviceMedicalId, date);
        return Success<string>("Service medical daily schedule deleted successfully");
    }

    /// <summary>
    /// Get available slots for a service medical
    /// </summary>
    [HttpGet("{serviceMedicalId}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        Guid serviceMedicalId,
        [FromQuery] DateOnly date)
    {
        var request = new GetServiceMedicalAvailableSlotsRequest
        {
            ServiceMedicalId = serviceMedicalId,
            Date = date
        };

        var slots = await _scheduleService.GetServiceMedicalAvailableSlotsAsync(request);
        return Success(slots, "Available slots retrieved successfully");
    }
}
