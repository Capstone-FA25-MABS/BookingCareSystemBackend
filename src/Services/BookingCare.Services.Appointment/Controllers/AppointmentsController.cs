using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BookingCare.Services.Appointment.Services;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Enums;

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
    private const string IdMismatchErrorMessage = "ID in URL does not match ID in request body";

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
    /// <returns>Created appointment ID</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var appointmentId = await _appointmentService.CreateAppointmentAsync(request);

        if (appointmentId == Guid.Empty)
            return BadRequest("Failed to create appointment");

        return Success(new { appointmentId }, "Appointment created successfully");
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
        var appointment = await _appointmentService.GetAppointmentByIdForPatientAsync(id);

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
    [Authorize(Roles = "Admin,Staff,Doctor")]
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
            return BadRequest(IdMismatchErrorMessage);

        var success = await _appointmentService.UpdateAppointmentStatusAsync(request);

        if (!success)
            return BadRequest("Failed to update appointment status");

        return Success("Appointment status updated successfully");
    }

    /// <summary>
    /// Cancel an appointment (any time before appointment)
    /// Triggers refund process and sends notifications with reschedule options
    /// Refund percentage depends on cancellation time:
    /// - >= 24 hours before: 100% refund
    /// - 12-24 hours before: 50% refund
    /// - < 12 hours before: 0% refund
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Cancellation request with reason</param>
    /// <returns>Success status with reschedule options</returns>
    [HttpPost("cancel/{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Roles = "Staff, Patient")]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody] CancelAppointmentRequest request)
    {
        if (id != request.AppointmentId)
            return BadRequest(IdMismatchErrorMessage);

        var result = await _appointmentService.CancelAppointmentAsync(request);

        if (result == null)
            return BadRequest("Failed to cancel appointment");

        return Success(result, "Appointment cancelled successfully");
    }

    /// <summary>
    /// Reschedule appointment with same doctor (Option 1)
    /// Patient selects new date/time for same doctor
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Reschedule request with token and new date/time</param>
    /// <returns>Updated appointment</returns>
    [HttpPost("{id:guid}/reschedule-same-doctor")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RescheduleSameDoctor(
        Guid id,
        [FromBody] RescheduleSameDoctorRequest request)
    {
        if (id != request.AppointmentId)
            return BadRequest(IdMismatchErrorMessage);

        var result = await _appointmentService.RescheduleSameDoctorAsync(request);

        if (!result)
            return BadRequest("Failed to reschedule appointment");

        return Success("Appointment rescheduled successfully with same doctor");
    }

    /// <summary>
    /// Staff assigns new doctor (Option 2 - Step 1: Create soft reservation)
    /// Creates a soft lock on doctor's schedule for 48 hours
    /// Returns confirmation URL for patient
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Assignment request with new doctor ID</param>
    /// <returns>Confirmation URL for patient</returns>
    [HttpPost("{id:guid}/assign-new-doctor")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Roles = "Staff, Admin")]
    public async Task<IActionResult> AssignNewDoctor(
        Guid id,
        [FromBody] AssignNewDoctorRequest request)
    {
        if (id != request.AppointmentId)
            return BadRequest(IdMismatchErrorMessage);

        var confirmationUrl = await _appointmentService.AssignNewDoctorAsync(request);

        return Success(new { confirmationUrl }, "Doctor assigned successfully. Patient will be notified.");
    }

    /// <summary>
    /// Request refund for cancelled appointment (Option 4)
    /// Patient chooses refund instead of rescheduling
    /// Triggers Payment Service to create refund record
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Refund request with token and bank info</param>
    /// <returns>Success status</returns>
    [HttpPost("{id:guid}/request-refund")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RequestRefund(
        Guid id,
        [FromBody] RequestRefundRequest request)
    {
        if (id != request.AppointmentId)
            return BadRequest(IdMismatchErrorMessage);

        var success = await _appointmentService.RequestRefundAsync(request);

        if (!success)
            return BadRequest("Failed to create refund request");

        return Success("Refund request submitted successfully. Payment Service will process your request.");
    }

    /// <summary>
    /// Choose new doctor (Option 3)
    /// Patient selects a different doctor from same hospital + specialty
    /// Handles 3 scenarios: same price (direct update), higher price (payment required), lower price (refund created)
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Choose new doctor request with token and new doctor info</param>
    /// <returns>Response with action to take and any payment/refund info</returns>
    [HttpPost("{id:guid}/choose-new-doctor")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ChooseNewDoctor(
        Guid id,
        [FromBody] ChooseNewDoctorRequest request)
    {
        if (id != request.AppointmentId)
            return BadRequest(IdMismatchErrorMessage);

        var result = await _appointmentService.ChooseNewDoctorAsync(request);

        return Success(result, result.Message);
    }

    /// <summary>
    /// Get available doctors for assignment (Option 2)
    /// Returns doctors from same hospital + specialty
    /// Used by staff when assigning new doctor to cancelled appointment
    /// </summary>
    /// <param name="hospitalId">Hospital ID</param>
    /// <param name="specialtyId">Specialty ID</param>
    /// <param name="appointmentDate">Appointment date (required when checkAvailability = true)</param>
    /// <param name="appointmentTimeId">Appointment time slot (required when checkAvailability = true)</param>
    /// <param name="checkAvailability">If true, only return doctors available at specified date/time. If false, return all doctors.</param>
    /// <returns>List of doctors</returns>
    [HttpGet("available-doctors")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Roles = "Staff, Admin")]
    public async Task<IActionResult> GetAvailableDoctors(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid specialtyId,
        [FromQuery] DateTime? appointmentDate = null,
        [FromQuery] AppointmentTime? appointmentTimeId = null,
        [FromQuery] bool checkAvailability = true)
    {
        var result = await _appointmentService.GetAvailableDoctorsAsync(
            hospitalId,
            specialtyId,
            appointmentDate,
            appointmentTimeId,
            checkAvailability);

        return Success(result, $"Found {result.TotalCount} doctors");
    }

}