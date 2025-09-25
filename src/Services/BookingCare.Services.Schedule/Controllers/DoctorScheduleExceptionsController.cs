using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing doctor schedule exceptions
/// </summary>
[Route("api/v{version:apiVersion}/doctor-schedule-exceptions")]
public class DoctorScheduleExceptionsController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public DoctorScheduleExceptionsController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get doctor's schedule exceptions for a specific date
    /// </summary>
    [HttpGet("{doctorId}/{date}")]
    public async Task<IActionResult> GetDoctorExceptions(long doctorId, DateOnly date)
    {
        var exceptions = await _scheduleService.GetDoctorExceptionsAsync(doctorId, date);
        return Success(exceptions, "Doctor schedule exceptions retrieved successfully");
    }

    /// <summary>
    /// Create a doctor schedule exception
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDoctorScheduleException([FromBody] CreateDoctorScheduleExceptionRequest request)
    {
        var exception = await _scheduleService.CreateDoctorScheduleExceptionAsync(request);
        return Success(exception, "Doctor schedule exception created successfully");
    }

    /// <summary>
    /// Delete a doctor schedule exception
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctorScheduleException(long id)
    {
        await _scheduleService.DeleteDoctorScheduleExceptionAsync(id);
        return Success<string>("Doctor schedule exception deleted successfully");
    }
}