using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/subscription-usage")]
[Route("api/subscription-usage")]
public class SubscriptionUsageController : ControllerBase
{
    private readonly ISubscriptionUsageService _usageService;
    private readonly ILogger<SubscriptionUsageController> _logger;

    public SubscriptionUsageController(
        ISubscriptionUsageService usageService,
        ILogger<SubscriptionUsageController> logger)
    {
        _usageService = usageService;
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "SubscriptionUsage", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("hospital/{hospitalId}")]
    [ProducesResponseType(typeof(SubscriptionUsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageByHospitalId(Guid hospitalId)
    {
        try
        {
            var usage = await _usageService.GetUsageByHospitalIdAsync(hospitalId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}/doctor-limit")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckDoctorLimit(Guid hospitalId)
    {
        try
        {
            var canAddDoctor = await _usageService.CheckDoctorLimitAsync(hospitalId);
            return Ok(new { CanAddDoctor = canAddDoctor, HospitalId = hospitalId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor limit for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}/specialty-limit")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckSpecialtyLimit(Guid hospitalId)
    {
        try
        {
            var canAddSpecialty = await _usageService.CheckSpecialtyLimitAsync(hospitalId);
            return Ok(new { CanAddSpecialty = canAddSpecialty, HospitalId = hospitalId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking specialty limit for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}/appointment-limit")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckAppointmentLimit(Guid hospitalId, [FromQuery] int additionalAppointments = 1)
    {
        try
        {
            var canAddAppointments = await _usageService.CheckAppointmentLimitAsync(hospitalId, additionalAppointments);
            return Ok(new
            {
                CanAddAppointments = canAddAppointments,
                HospitalId = hospitalId,
                AdditionalAppointments = additionalAppointments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking appointment limit for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}/alerts")]
    [ProducesResponseType(typeof(SubscriptionUsageAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckUsageAlerts(Guid hospitalId)
    {
        try
        {
            var alerts = await _usageService.CheckUsageAlertsAsync(hospitalId);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking usage alerts for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(List<SubscriptionUsageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageReport([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var report = await _usageService.GetUsageReportAsync(fromDate, toDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating usage report");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}
