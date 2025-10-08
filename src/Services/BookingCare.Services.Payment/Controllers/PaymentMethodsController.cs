using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for managing payment methods
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
public class PaymentMethodsController : BaseApiController
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly ILogger<PaymentMethodsController> _logger;

    public PaymentMethodsController(
        IPaymentMethodService paymentMethodService,
        ILogger<PaymentMethodsController> logger
    )
    {
        _paymentMethodService = paymentMethodService;
        _logger = logger;
    }

    /// <summary>
    /// Get all payment methods
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllPaymentMethods()
    {
        try
        {
            var paymentMethods = await _paymentMethodService.GetAllAsync();
            return Success(paymentMethods, "Get payment methods list successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all payment methods");
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving the payment methods list" }
            );
        }
    }

    /// <summary>
    /// Get only active payment methods
    /// </summary>
    [HttpGet("active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActivePaymentMethods()
    {
        try
        {
            var paymentMethods = await _paymentMethodService.GetActiveAsync();
            return Success(paymentMethods, "Get active payment methods successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active payment methods");
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving active payment methods" }
            );
        }
    }

    /// <summary>
    /// Get payment method by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentMethod(Guid id)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.GetByIdAsync(id);
            if (paymentMethod == null)
            {
                return NotFound($"Payment method with ID {id} was not found");
            }

            return Success(paymentMethod, "Get payment method successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method with ID: {PaymentMethodId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the payment method" });
        }
    }

    /// <summary>
    /// Get payment method by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentMethodByName(string name)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.GetByNameAsync(name);
            if (paymentMethod == null)
            {
                return NotFound($"Payment method with name '{name}' was not found");
            }

            return Success(paymentMethod, "Get payment method successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting payment method with name: {PaymentMethodName}",
                name
            );
            return StatusCode(500, new { Message = "An error occurred while retrieving the payment method" });
        }
    }

    /// <summary>
    /// Toggle payment method status (ACTIVE <-> INACTIVE)
    /// </summary>
    [HttpPut("{id}/toggle")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> TogglePaymentMethodStatus(Guid id)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.ToggleStatusAsync(id);
            return Success(paymentMethod, "Toggle payment method status successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when toggling payment method status");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error toggling payment method status for ID: {PaymentMethodId}",
                id
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while toggling payment method status" }
            );
        }
    }
}
