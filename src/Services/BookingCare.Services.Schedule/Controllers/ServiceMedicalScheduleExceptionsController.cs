using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing service medical schedule exceptions
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class ServiceMedicalScheduleExceptionsController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public ServiceMedicalScheduleExceptionsController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get service medical's schedule exceptions for a specific date
    /// </summary>
    [HttpGet("{serviceMedicalId}/{date}")]
    public async Task<IActionResult> GetServiceMedicalExceptions(Guid serviceMedicalId, DateOnly date)
    {
        var exceptions = await _scheduleService.GetServiceMedicalExceptionsAsync(serviceMedicalId, date);
        return Success(exceptions, "Service medical schedule exceptions retrieved successfully");
    }

    /// <summary>
    /// Create service medical schedule exceptions for multiple appointment times
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateServiceMedicalScheduleException([FromBody] CreateServiceMedicalScheduleExceptionRequest request)
    {
        var exceptions = await _scheduleService.CreateServiceMedicalScheduleExceptionAsync(request);
        var message = exceptions.Count == 1
            ? "Service medical schedule exception created successfully"
            : $"{exceptions.Count} service medical schedule exceptions created successfully";
        return Success(exceptions, message);
    }

    /// <summary>
    /// Delete a service medical schedule exception
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServiceMedicalScheduleException(Guid id)
    {
        await _scheduleService.DeleteServiceMedicalScheduleExceptionAsync(id);
        return Success<string>("Service medical schedule exception deleted successfully");
    }
}
