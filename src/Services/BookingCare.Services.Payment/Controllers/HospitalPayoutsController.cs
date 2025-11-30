using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for hospital payout operations (Admin only)
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class HospitalPayoutsController : BaseApiController
{
    private readonly IHospitalPayoutService _payoutService;
    private readonly ILogger<HospitalPayoutsController> _logger;

    public HospitalPayoutsController(
        IHospitalPayoutService payoutService,
        ILogger<HospitalPayoutsController> logger
    )
    {
        _payoutService = payoutService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of hospital payouts with filters
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination</param>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPayouts([FromQuery] PayoutQueryRequest query)
    {
        try
        {
            var result = await _payoutService.GetPayoutsAsync(query);
            return Success(result, "Retrieved payouts successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payouts");
            return StatusCode(500, new { Message = "An error occurred while retrieving payouts" });
        }
    }

    /// <summary>
    /// Get payout details by ID including appointment breakdown
    /// </summary>
    /// <param name="id">Payout ID</param>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPayoutDetails(Guid id)
    {
        try
        {
            var result = await _payoutService.GetPayoutDetailsAsync(id);
            return Success(result, "Retrieved payout details successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payout details for ID: {PayoutId}", id);
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving payout details" }
            );
        }
    }

    /// <summary>
    /// Generate payouts for hospitals in a specific period
    /// </summary>
    /// <param name="request">Request containing period and optional hospital IDs</param>
    [HttpPost("generate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> GeneratePayouts([FromBody] GeneratePayoutsRequest request)
    {
        try
        {
            // Get admin ID from claims using JwtHelper
            var adminId = JwtHelper.GetAccountIdFromClaims(HttpContext);
            if (adminId == null)
            {
                return Unauthorized("Invalid or missing admin credentials");
            }

            var result = await _payoutService.GeneratePayoutsAsync(request, adminId.Value);
            return Success(result, $"Generated {result.Count} payout(s) successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payouts");
            return StatusCode(500, new { Message = "An error occurred while generating payouts" });
        }
    }

    /// <summary>
    /// Mark a payout as completed (admin has transferred money)
    /// </summary>
    /// <param name="id">Payout ID</param>
    /// <param name="request">Request containing optional notes</param>
    [HttpPut("{id}/complete")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> MarkPayoutCompleted(
        Guid id,
        [FromBody] MarkPayoutCompletedRequest request
    )
    {
        try
        {
            // Get admin ID from claims using JwtHelper
            var adminId = JwtHelper.GetAccountIdFromClaims(HttpContext);
            if (adminId == null)
            {
                return Unauthorized("Invalid or missing admin credentials");
            }

            var result = await _payoutService.MarkPayoutCompletedAsync(id, request, adminId.Value);
            return Success(result, "Payout marked as completed successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payout as completed for ID: {PayoutId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating payout" });
        }
    }

    /// <summary>
    /// Get payout statistics
    /// </summary>
    [HttpGet("statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var result = await _payoutService.GetStatisticsAsync();
            return Success(result, "Retrieved payout statistics successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payout statistics");
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving statistics" }
            );
        }
    }

    /// <summary>
    /// Get list of hospitals with pending payouts for a period
    /// </summary>
    /// <param name="periodStartDate">Period start date (format: YYYY-MM-DD)</param>
    /// <param name="periodEndDate">Period end date (format: YYYY-MM-DD)</param>
    [HttpGet("pending-hospitals")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetHospitalsWithPendingPayouts(
        [FromQuery(Name = "periodStartDate")] string? periodStartDate,
        [FromQuery(Name = "periodEndDate")] string? periodEndDate
    )
    {
        try
        {
            // Parse date strings to DateTime
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;

            if (
                !string.IsNullOrEmpty(periodStartDate)
                && DateTime.TryParse(periodStartDate, out var parsedStart)
            )
            {
                startDate = parsedStart.Date;
            }

            if (
                !string.IsNullOrEmpty(periodEndDate)
                && DateTime.TryParse(periodEndDate, out var parsedEnd)
            )
            {
                endDate = parsedEnd.Date.AddDays(1).AddTicks(-1); // End of day
            }

            var result = await _payoutService.GetHospitalsWithPendingPayoutsAsync(
                startDate,
                endDate
            );
            return Success(result, "Retrieved hospitals with pending payouts successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals with pending payouts");
            return StatusCode(500, new { Message = "An error occurred while retrieving data" });
        }
    }
}
