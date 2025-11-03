using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Exceptions;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/subscription-plans")]
[Route("api/subscription-plans")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(
        ISubscriptionPlanService subscriptionPlanService,
        ILogger<SubscriptionPlansController> logger)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "SubscriptionPlans", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid id)
    {
        try
        {
            var plan = await _subscriptionPlanService.GetByIdAsync(id);
            if (plan == null)
            {
                return NotFound(new { Message = $"Subscription plan with ID {id} not found" });
            }
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan with ID {PlanId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionPlanByName(string name)
    {
        try
        {
            var plan = await _subscriptionPlanService.GetByNameAsync(name);
            if (plan == null)
            {
                return NotFound(new { Message = $"Subscription plan with name {name} not found" });
            }
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan with name {PlanName}", name);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionPlanListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllSubscriptionPlans()
    {
        try
        {
            var plans = await _subscriptionPlanService.GetAllAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all subscription plans");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("filtered")]
    [ProducesResponseType(typeof(SubscriptionPlanListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilteredSubscriptionPlans([FromQuery] SubscriptionPlanFilterRequest filter)
    {
        try
        {
            _logger.LogInformation("GetFilteredSubscriptionPlans called with Page={Page}, PageSize={PageSize}, SortBy={SortBy}, SortOrder={SortOrder}",
                filter.Page, filter.PageSize, filter.SortBy, filter.SortOrder);

            var plans = await _subscriptionPlanService.GetFilteredAsync(filter);

            _logger.LogInformation("Successfully retrieved {Count} subscription plans", plans.SubscriptionPlans.Count);
            return Ok(plans);
        }
        catch (SubscriptionPlanOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error retrieving filtered subscription plans: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving filtered subscription plans. Filter: {@Filter}, ErrorType: {ErrorType}, StackTrace: {StackTrace}",
                filter, ex.GetType().Name, ex.StackTrace);
            return StatusCode(500, new { Message = $"An error occurred while retrieving subscription plans: {ex.Message}" });
        }
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(List<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveSubscriptionPlans()
    {
        try
        {
            var plans = await _subscriptionPlanService.GetActiveAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subscription plans");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        try
        {
            _logger.LogInformation("CreateSubscriptionPlan called with Name={Name}, Price={Price}, BillingCycle={BillingCycle}",
                request.Name, request.Price, request.BillingCycle);

            var plan = await _subscriptionPlanService.CreateAsync(request);

            _logger.LogInformation("Successfully created subscription plan with ID={Id}", plan.Id);
            return CreatedAtAction(nameof(GetSubscriptionPlanById),
                new { id = plan.Id }, plan);
        }
        catch (SubscriptionPlanAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Subscription plan already exists: {PlanName}", request.Name);
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidSubscriptionPlanDataException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription plan data: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan {PlanName}. ErrorType: {ErrorType}, StackTrace: {StackTrace}",
                request.Name, ex.GetType().Name, ex.StackTrace);
            return StatusCode(500, new { Message = $"An error occurred while creating subscription plan: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSubscriptionPlan(Guid id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        try
        {
            var plan = await _subscriptionPlanService.UpdateAsync(id, request);
            return Ok(plan);
        }
        catch (SubscriptionPlanNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (SubscriptionPlanAlreadyExistsException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidSubscriptionPlanDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan with ID {PlanId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSubscriptionPlan(Guid id)
    {
        try
        {
            var result = await _subscriptionPlanService.DeleteAsync(id);
            if (result)
            {
                return Ok(new { Message = "Subscription plan deleted successfully", PlanId = id });
            }
            return NotFound(new { Message = $"Subscription plan with ID {id} not found" });
        }
        catch (SubscriptionPlanOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan with ID {PlanId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}
