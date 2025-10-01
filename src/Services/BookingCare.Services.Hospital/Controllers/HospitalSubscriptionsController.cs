using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Exceptions;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class HospitalSubscriptionsController : ControllerBase
{
    private readonly IHospitalSubscriptionService _subscriptionService;
    private readonly ILogger<HospitalSubscriptionsController> _logger;

    public HospitalSubscriptionsController(
        IHospitalSubscriptionService subscriptionService,
        ILogger<HospitalSubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "HospitalSubscriptions", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubscriptionById(Guid id)
    {
        try
        {
            var subscription = await _subscriptionService.GetByIdAsync(id);
            if (subscription == null)
            {
                return NotFound(new { Message = $"Hospital subscription with ID {id} not found" });
            }
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital subscription with ID {SubscriptionId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}")]
    public async Task<IActionResult> GetSubscriptionsByHospitalId(Guid hospitalId)
    {
        try
        {
            var subscriptions = await _subscriptionService.GetByHospitalIdAsync(hospitalId);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("hospital/{hospitalId}/active")]
    public async Task<IActionResult> GetActiveSubscriptionByHospitalId(Guid hospitalId)
    {
        try
        {
            var subscription = await _subscriptionService.GetActiveByHospitalIdAsync(hospitalId);
            if (subscription == null)
            {
                return NotFound(new { Message = $"No active subscription found for hospital {hospitalId}" });
            }
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subscription for hospital {HospitalId}", hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringSoon([FromQuery] int days = 30)
    {
        try
        {
            var subscriptions = await _subscriptionService.GetExpiringSoonAsync(days);
            return Ok(new
            {
                Message = $"Subscriptions expiring in the next {days} days",
                Count = subscriptions.Count,
                Subscriptions = subscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring subscriptions");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateHospitalSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.CreateAsync(request);
            return CreatedAtAction(nameof(GetSubscriptionById),
                new { id = subscription.HospitalSubscriptionId }, subscription);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (SubscriptionPlanNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (HospitalOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital subscription");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateHospitalSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpdateAsync(id, request);
            return Ok(subscription);
        }
        catch (HospitalOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital subscription with ID {SubscriptionId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] CancelSubscriptionRequest? request = null)
    {
        try
        {
            var reason = request?.CancellationReason ?? "";
            var result = await _subscriptionService.CancelSubscriptionAsync(id, reason);
            if (result)
            {
                return Ok(new { Message = "Subscription cancelled successfully", SubscriptionId = id });
            }
            return NotFound(new { Message = $"Hospital subscription with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling hospital subscription with ID {SubscriptionId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}

public class CancelSubscriptionRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}
