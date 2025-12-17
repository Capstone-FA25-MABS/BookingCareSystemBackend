using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Exceptions;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/hospital-subscriptions")]
[Route("api/hospital-subscriptions")]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "HospitalSubscriptions", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Role:Admin,Staff")]
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
    [Authorize(Policy = "Role:Admin,Staff")]
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
    [Authorize(Policy = "Role:Admin,Staff")]
    public async Task<IActionResult> GetActiveSubscriptionByHospitalId(Guid hospitalId)
    {
        try
        {
            _logger.LogInformation("Attempting to get active subscription for hospital {HospitalId}", hospitalId);
            var subscription = await _subscriptionService.GetActiveByHospitalIdAsync(hospitalId);
            if (subscription == null)
            {
                _logger.LogInformation("No active subscription found for hospital {HospitalId}", hospitalId);
                // Return 200 OK with null data instead of 404 NotFound
                // This allows frontend to handle "no subscription yet" scenario gracefully
                return Ok(new
                {
                    success = true,
                    message = $"No active subscription found for hospital {hospitalId}",
                    data = (object?)null
                });
            }
            _logger.LogInformation("Successfully retrieved active subscription for hospital {HospitalId}", hospitalId);
            return Ok(new
            {
                success = true,
                message = "Active subscription retrieved successfully",
                data = subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subscription for hospital {HospitalId}. Exception: {ExceptionMessage}. StackTrace: {StackTrace}",
                hospitalId, ex.Message, ex.StackTrace);
            return StatusCode(500, new
            {
                success = false,
                message = "An unexpected error occurred. Please try again later.",
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("expiring")]
    [Authorize(Policy = "Role:Admin,Staff")]
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
    [Authorize(Policy = "Role:Admin")]
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
    [Authorize(Policy = "Role:Admin")]
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
    [Authorize(Policy = "Role:Admin")]
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

    [HttpPost("{id}/upgrade")]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> UpgradeSubscription(Guid id, [FromBody] UpgradeHospitalSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpgradeSubscriptionAsync(id, request.NewSubscriptionPlanId);
            return Ok(new
            {
                success = true,
                message = "Subscription upgraded successfully",
                data = subscription
            });
        }
        catch (HospitalOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (SubscriptionPlanNotFoundException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading hospital subscription with ID {SubscriptionId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }
}

public class CancelSubscriptionRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}
