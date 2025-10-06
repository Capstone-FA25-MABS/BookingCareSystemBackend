using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Appointment.Services;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;

namespace BookingCare.Services.Appointment.Controllers;

/// <summary>
/// Controller for appointment operations
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class AppointmentsController : BaseApiController
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Appointment",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a new appointment
    /// </summary>
    /// <param name="request">Appointment creation request</param>
    /// <returns>Success status</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var success = await _appointmentService.CreateAppointmentAsync(request);

        if (!success)
            return BadRequest("Failed to create appointment");

        return Success("Appointment created successfully");
    }

    /// <summary>
    /// Get appointment by ID
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <returns>Appointment details</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAppointment(Guid id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);

        if (appointment == null)
            return NotFound($"Appointment with ID {id} not found");

        return Success(appointment, "Appointment retrieved successfully");
    }

    /// <summary>
    /// Get appointments by patient with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters (must include PatientId)</param>
    /// <returns>Paginated list of patient appointments with enriched data</returns>
    [HttpPost("patient")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAppointmentsByPatient([FromBody] AppointmentQueryRequest query)
    {
        var appointments = await _appointmentService.GetAppointmentsByPatientAsync(query);
        return Success(appointments, "Patient appointments retrieved successfully");
    }

    /// <summary>
    /// Get appointments for management roles (Doctor, Staff, Admin) with role-based filtering
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of appointments with enriched data based on user role</returns>
    [HttpPost("management")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAppointmentsForManagement([FromBody] AppointmentQueryRequest query)
    {
        var appointments = await _appointmentService.GetAppointmentsForManagementAsync(query);
        return Success(appointments, "Management appointments retrieved successfully");
    }

    /// <summary>
    /// Update appointment status
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Success status</returns>
    [HttpPut("status/{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateAppointmentStatus(
        Guid id,
        [FromBody] UpdateAppointmentStatusRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID in URL does not match ID in request body");

        var success = await _appointmentService.UpdateAppointmentStatusAsync(request);

        if (!success)
            return BadRequest("Failed to update appointment status");

        return Success("Appointment status updated successfully");
    }

}