using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing doctor schedule exceptions
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
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
    public async Task<IActionResult> GetDoctorExceptions(Guid doctorId, DateOnly date)
    {
        var exceptions = await _scheduleService.GetDoctorExceptionsAsync(doctorId, date);
        return Success(exceptions, "Doctor schedule exceptions retrieved successfully");
    }

    /// <summary>
    /// Create doctor schedule exceptions for multiple appointment times
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDoctorScheduleException([FromBody] CreateDoctorScheduleExceptionRequest request)
    {
        var exceptions = await _scheduleService.CreateDoctorScheduleExceptionAsync(request);
        var message = exceptions.Count == 1
            ? "Doctor schedule exception created successfully"
            : $"{exceptions.Count} doctor schedule exceptions created successfully";
        return Success(exceptions, message);
    }

    /// <summary>
    /// Delete a doctor schedule exception
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctorScheduleException(Guid id)
    {
        await _scheduleService.DeleteDoctorScheduleExceptionAsync(id);
        return Success<string>("Doctor schedule exception deleted successfully");
    }
}