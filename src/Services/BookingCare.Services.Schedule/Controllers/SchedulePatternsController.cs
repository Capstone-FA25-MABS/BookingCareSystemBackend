using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing schedule patterns
/// </summary>
[Route("api/v{version:apiVersion}/schedule-patterns")]
public class SchedulePatternsController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public SchedulePatternsController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get all schedule patterns
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSchedulePatterns()
    {
        var patterns = await _scheduleService.GetAllSchedulePatternsAsync();
        return Success(patterns, "Schedule patterns retrieved successfully");
    }

    /// <summary>
    /// Get schedule pattern by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSchedulePatternById(long id)
    {
        var pattern = await _scheduleService.GetSchedulePatternByIdAsync(id);
        if (pattern == null)
        {
            return NotFound("Schedule pattern not found");
        }
        return Success(pattern, "Schedule pattern retrieved successfully");
    }

    /// <summary>
    /// Create a new schedule pattern
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSchedulePattern([FromBody] CreateSchedulePatternRequest request)
    {
        var pattern = await _scheduleService.CreateSchedulePatternAsync(request);
        return Success(pattern, "Schedule pattern created successfully");
    }
}