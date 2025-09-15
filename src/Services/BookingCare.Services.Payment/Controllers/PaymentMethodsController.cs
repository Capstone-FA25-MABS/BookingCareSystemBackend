using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller quản lý các phương thức thanh toán
/// </summary>
[Route("api/[controller]")]
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
    /// Lấy tất cả payment methods
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPaymentMethods()
    {
        try
        {
            var paymentMethods = await _paymentMethodService.GetAllAsync();
            return Success(paymentMethods, "Lấy danh sách payment methods thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all payment methods");
            return StatusCode(
                500,
                new { Message = "Có lỗi xảy ra khi lấy danh sách payment methods" }
            );
        }
    }

    /// <summary>
    /// Lấy chỉ payment methods đang active
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActivePaymentMethods()
    {
        try
        {
            var paymentMethods = await _paymentMethodService.GetActiveAsync();
            return Success(paymentMethods, "Lấy danh sách payment methods active thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active payment methods");
            return StatusCode(
                500,
                new { Message = "Có lỗi xảy ra khi lấy danh sách payment methods active" }
            );
        }
    }

    /// <summary>
    /// Lấy payment method theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentMethod(Guid id)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.GetByIdAsync(id);
            if (paymentMethod == null)
            {
                return NotFound($"Payment method với ID {id} không tìm thấy");
            }

            return Success(paymentMethod, "Lấy payment method thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method with ID: {PaymentMethodId}", id);
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy payment method" });
        }
    }

    /// <summary>
    /// Lấy payment method theo tên
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetPaymentMethodByName(string name)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.GetByNameAsync(name);
            if (paymentMethod == null)
            {
                return NotFound($"Payment method với tên '{name}' không tìm thấy");
            }

            return Success(paymentMethod, "Lấy payment method thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting payment method with name: {PaymentMethodName}",
                name
            );
            return StatusCode(500, new { Message = "Có lỗi xảy ra khi lấy payment method" });
        }
    }

    /// <summary>
    /// Toggle trạng thái payment method (ACTIVE <-> INACTIVE)
    /// </summary>
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> TogglePaymentMethodStatus(Guid id)
    {
        try
        {
            var paymentMethod = await _paymentMethodService.ToggleStatusAsync(id);
            return Success(paymentMethod, "Toggle trạng thái payment method thành công");
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
                new { Message = "Có lỗi xảy ra khi toggle trạng thái payment method" }
            );
        }
    }
}
