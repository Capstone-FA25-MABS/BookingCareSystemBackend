using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Helpers;
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
    /// Get pending exception requests for approval (filtered by hospital or doctor)
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingExceptionRequests(
        [FromQuery] Guid? hospitalId = null,
        [FromQuery] Guid? doctorId = null)
    {
        var pendingRequests = await _scheduleService.GetPendingDoctorExceptionRequestsAsync(hospitalId, doctorId);
        return Success(pendingRequests, "Pending doctor exception requests retrieved successfully");
    }

    /// <summary>
    /// Get all exception requests for a specific doctor (all statuses - for doctor's own view)
    /// </summary>
    [HttpGet("my-requests/{doctorId:guid}", Order = 1)]
    public async Task<IActionResult> GetMyExceptionRequests(Guid doctorId)
    {
        var requests = await _scheduleService.GetDoctorExceptionsByDoctorIdAsync(doctorId);
        return Success(requests, "Doctor exception requests retrieved successfully");
    }

    /// <summary>
    /// Get doctor's schedule exceptions for a specific date
    /// </summary>
    [HttpGet("by-date/{doctorId:guid}/{date}", Order = 2)]
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
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDoctorScheduleException(Guid id)
    {
        await _scheduleService.DeleteDoctorScheduleExceptionAsync(id);
        return Success<string>("Doctor schedule exception deleted successfully");
    }

    /// <summary>
    /// Approve or reject an exception request (Staff only)
    /// </summary>
    [HttpPut("review")]
    public async Task<IActionResult> ReviewExceptionRequest([FromBody] ReviewExceptionRequest request)
    {
        // Get reviewer ID from JWT token
        var reviewerId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);

        var reviewed = await _scheduleService.ReviewDoctorScheduleExceptionAsync(request, reviewerId);
        var statusText = reviewed.Status == "APPROVED" ? "approved" : "rejected";
        return Success(reviewed, $"Exception request {statusText} successfully");
    }
}