using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Shared.Common.Helpers;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing specialty schedules (hospital assigns doctor mode)
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class SpecialtySchedulesController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public SpecialtySchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get aggregated available slots for a specialty
    /// This aggregates availability across all doctors in the specialty for "hospital assigns doctor" mode
    /// </summary>
    /// <param name="hospitalId">Hospital ID</param>
    /// <param name="specialtyId">Specialty ID</param>
    /// <param name="date">Date to check availability</param>
    /// <param name="appointmentType">Appointment type (IN_PERSON or TELEHEALTH)</param>
    /// <returns>Aggregated available slots with capacity information</returns>
    [HttpGet("{hospitalId}/specialties/{specialtyId}/available-slots")]
    public async Task<IActionResult> GetSpecialtyAvailableSlots(
        Guid hospitalId,
        Guid specialtyId,
        [FromQuery] DateOnly date,
        [FromQuery] AppointmentType appointmentType = AppointmentType.IN_PERSON)
    {
        var request = new GetSpecialtyAvailableSlotsRequest
        {
            HospitalId = hospitalId,
            SpecialtyId = specialtyId,
            Date = date,
            AppointmentType = appointmentType
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

        var response = await _scheduleService.GetSpecialtyAvailableSlotsAsync(request, currentUserId);
        return Success(response, "Specialty available slots retrieved successfully");
    }
}
