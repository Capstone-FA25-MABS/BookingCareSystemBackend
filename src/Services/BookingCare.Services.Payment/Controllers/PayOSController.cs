using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.PayOS;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using System.Text.Json;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for PayOS integration
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
[Route("api/[controller]")]
public class PayOSController : BaseApiController
{
    private readonly IPayOSService _payOSService;
    private readonly IPaymentService _paymentService;
    private readonly IValidator<PayOSPaymentRequest> _validator;
    private readonly ILogger<PayOSController> _logger;

    public PayOSController(
        IPayOSService payOSService,
        IPaymentService paymentService,
        IValidator<PayOSPaymentRequest> validator,
        ILogger<PayOSController> logger)
    {
        _payOSService = payOSService;
        _paymentService = paymentService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Health check for PayOS service
    /// </summary>
    /// <returns>Status of PayOS service</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return Success(new
        {
            Service = "PayOS Integration",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = ApiVersions.V1_0
        }, "PayOS service is healthy");
    }

    /// <summary>
    /// Create PayOS payment link
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <returns>PayOS payment link</returns>
    [HttpPost("create-payment-link")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreatePaymentLink([FromBody] PayOSPaymentRequest request)
    {
        try
        {
            // Validate request data
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            // Validate payment exists
            var payment = await _paymentService.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                return NotFound($"Payment with ID {request.PaymentId} was not found");
            }

            // Validate amount matches
            if (Math.Abs(payment.Amount - request.Amount) > 0.01m)
            {
                return BadRequest("Amount does not match the payment in the system");
            }

            // Validate payment status
            if (payment.Status != PaymentStatus.PENDING)
            {
                return BadRequest($"Payment has been processed with status: {payment.Status}");
            }

            // Create PayOS payment link
            var payOSResponse = await _payOSService.CreatePaymentLinkAsync(request);

            _logger.LogInformation("PayOS payment link created successfully for PaymentId: {PaymentId}, OrderCode: {OrderCode}",
                request.PaymentId, payOSResponse.OrderCode);

            return Success(payOSResponse, "Create PayOS payment link successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS payment creation failed - Invalid argument: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS payment creation failed for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { Message = "An error occurred while creating PayOS payment link" });
        }
    }

    /// <summary>
    /// Callback endpoint to receive user returning from PayOS (success/cancel)
    /// </summary>
    /// <returns>Payment result</returns>
    [HttpGet("payos-return")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> PayOSCallback([FromQuery] string code, [FromQuery] string id, [FromQuery] bool cancel, [FromQuery] string orderCode)
    {
        // Generate request ID for tracking duplicate calls
        var requestId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.LogInformation("PayOS Callback #{RequestId} - Code: {Code}, Id: {Id}, Cancel: {Cancel}, OrderCode: {OrderCode}",
                requestId, code, id, cancel, orderCode);

            // Validate orderCode parameter
            if (string.IsNullOrEmpty(orderCode))
            {
                _logger.LogWarning("PayOS Callback #{RequestId} - OrderCode is missing", requestId);
                return BadRequest("OrderCode parameter is required");
            }

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                _logger.LogWarning("PayOS Callback #{RequestId} - Invalid OrderCode format: {OrderCode}", requestId, orderCode);
                return BadRequest("Invalid OrderCode format");
            }

            // Process callback via PayOSService with tracking
            _logger.LogInformation("PayOS Callback #{RequestId} - Processing callback for OrderCode: {OrderCode}", requestId, orderCodeLong);

            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, code ?? string.Empty, cancel);

            // Enhanced logging with request tracking
            _logger.LogInformation("PayOS Callback #{RequestId} - Processed successfully - PaymentId: {PaymentId}, Success: {Success}, IsEmptyGuid: {IsEmptyGuid}",
                requestId, result.PaymentId, result.Success, result.PaymentId == Guid.Empty);

            // Create response with details
            var response = new
            {
                Success = result.Success,
                PaymentId = result.PaymentId,
                OrderCode = result.OrderCode,
                Code = result.ResponseCode,
                Amount = result.Amount,
                Message = result.Message,
                PaymentDate = result.PaymentDate,
                RequestId = requestId, // For tracking
                ProcessedAt = DateTime.UtcNow,
                IsAlreadyProcessed = result.PaymentId == Guid.Empty // enough, no need for IsDuplicateCall and OriginalPaymentId
            };

            return Success(response, result.Success ? "PayOS payment successful" : "PayOS payment failed");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS Callback #{RequestId} - Processing failed - Invalid argument: {Error}", requestId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Callback #{RequestId} - Processing failed - Code: {Code}, OrderCode: {OrderCode}",
                requestId, code, orderCode);
            return StatusCode(500, new
            {
                Message = "An error occurred while processing PayOS callback",
                RequestId = requestId
            });
        }
    }

    /// <summary>
    /// Get payment info from PayOS
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Payment details</returns>
    [HttpGet("payment-info/{orderCode}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPaymentInfo(long orderCode)
    {
        try
        {
            var paymentInfo = await _payOSService.GetPaymentInfoAsync(orderCode);
            return Success(paymentInfo, "Get PayOS payment info successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment info for OrderCode: {OrderCode}", orderCode);
            return StatusCode(500, new { Message = "An error occurred while retrieving PayOS payment info" });
        }
    }

    /// <summary>
    /// Manual cleanup of expired PayOS mappings
    /// </summary>
    /// <returns>Number of mappings deleted</returns>
    [HttpPost("cleanup-mappings")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CleanupMappings()
    {
        try
        {
            _logger.LogInformation("Manual PayOS mapping cleanup requested");

            var deletedCount = await _payOSService.CleanupExpiredMappingsAsync();

            _logger.LogInformation("Manual PayOS mapping cleanup completed - Deleted {DeletedCount} mappings", deletedCount);

            return Success(new
            {
                DeletedCount = deletedCount,
                CleanupTime = DateTime.UtcNow,
                Message = $"Deleted {deletedCount} expired mappings"
            }, "Cleanup PayOS mappings successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup PayOS mappings");
            return StatusCode(500, new { Message = "An error occurred while cleaning up PayOS mappings" });
        }
    }

    /// <summary>
    /// Cancel callback endpoint to receive user canceling payment from PayOS
    /// </summary>
    /// <returns>Cancellation result</returns>
    [HttpGet("cancel-callback")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> PayOSCancelCallback([FromQuery] string orderCode)
    {
        try
        {
            _logger.LogInformation("Received PayOS cancel callback - OrderCode: {OrderCode}", orderCode);

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                return BadRequest("Invalid order code format");
            }

            // Process cancel callback
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, "CANCELLED", true);

            _logger.LogInformation("PayOS cancel callback processed successfully - PaymentId: {PaymentId}",
                result.PaymentId);

            return Success(new
            {
                Success = false,
                PaymentId = result.PaymentId,
                OrderCode = result.OrderCode,
                Message = "Payment has been cancelled",
                CancelledAt = DateTime.UtcNow
            }, "PayOS payment has been cancelled");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("PayOS cancel callback processing failed - Invalid argument: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS cancel callback processing failed - OrderCode: {OrderCode}", orderCode);
            return StatusCode(500, new { Message = "An error occurred while processing PayOS cancel callback" });
        }
    }
}