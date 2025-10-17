using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.PayOS;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Services.Payment.Controllers.Base;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using System.Text.Json;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for PayOS integration
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class PayOSController : BasePaymentGatewayController
{
    private const string GatewayName = "PayOS";
    private readonly IPayOSService _payOSService;
    private readonly IValidator<PayOSPaymentRequest> _validator;

    public PayOSController(
        IPayOSService payOSService,
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        IValidator<PayOSPaymentRequest> validator,
        ILogger<PayOSController> logger,
        AppointmentService.AppointmentServiceClient appointmentClient)
        : base(paymentService, eventBus, frontendOptions, logger, appointmentClient)
    {
        _payOSService = payOSService;
        _validator = validator;
    }

    /// <summary>
    /// Health check for PayOS service
    /// </summary>
    /// <returns>Status of PayOS service</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return CreateHealthCheckResponse(GatewayName);
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

            // Use shared payment validation helper (PayOS requires PENDING status validation)
            var (validationError, _) = await PaymentValidationHelper.ValidatePaymentForGatewayAsync(
                PaymentService, request.PaymentId, request.Amount, validateStatus: true);

            if (validationError != null)
            {
                return validationError;
            }

            // Create PayOS payment link
            var payOSResponse = await _payOSService.CreatePaymentLinkAsync(request);

            Logger.LogInformation("PayOS payment link created successfully for PaymentId: {PaymentId}, OrderCode: {OrderCode}",
                request.PaymentId, payOSResponse.OrderCode);

            return Success(payOSResponse, "Create PayOS payment link successful");
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "PayOS payment creation failed - Invalid argument");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PayOS payment creation failed for PaymentId: {PaymentId}", request.PaymentId);
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
        var requestId = GenerateRequestId();

        try
        {
            Logger.LogInformation("PayOS Callback #{RequestId} - Code: {Code}, Id: {Id}, Cancel: {Cancel}, OrderCode: {OrderCode}",
                requestId, code, id, cancel, orderCode);

            // Validate input parameters
            var validationResult = ValidateCallbackParameters(orderCode, requestId);
            if (validationResult != null) return validationResult;

            // Process callback via PayOSService
            var orderCodeLong = long.Parse(orderCode);
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, code ?? string.Empty, cancel);

            Logger.LogInformation("PayOS Callback #{RequestId} - Processed successfully - PaymentId: {PaymentId}, Success: {Success}, IsEmptyGuid: {IsEmptyGuid}",
                requestId, result.PaymentId, result.Success, result.PaymentId == Guid.Empty);

            // Handle different payment outcomes
            if (result.PaymentId == Guid.Empty)
            {
                return CreateAlreadyProcessedResponse(result, requestId);
            }

            var payment = await GetPaymentWithValidation(result.PaymentId, requestId, GatewayName);
            if (payment == null)
            {
                return BadRequest("Payment not found");
            }

            if (result.Success)
            {
                return HandleSuccessfulPayment(payment, result, requestId, GatewayName,
                    (p, r, reqId) => CreateStandardResponse(r, reqId));
            }

            return await HandleFailedPaymentAsync(payment, result, requestId, GatewayName,
                payment.AppointmentId!.Value, GetPayOSResponseMessage,
                (p, r, reqId) => CreateStandardResponse(r, reqId));
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "PayOS Callback #{RequestId} - Processing failed - Invalid argument", requestId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateProcessingErrorResponse(ex, requestId, GatewayName, code, orderCode);
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
            Logger.LogError(ex, "Failed to get PayOS payment info for OrderCode: {OrderCode}", orderCode);
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
            Logger.LogInformation("Manual PayOS mapping cleanup requested");

            var deletedCount = await _payOSService.CleanupExpiredMappingsAsync();

            Logger.LogInformation("Manual PayOS mapping cleanup completed - Deleted {DeletedCount} mappings", deletedCount);

            return Success(new
            {
                DeletedCount = deletedCount,
                CleanupTime = DateTime.UtcNow,
                Message = $"Deleted {deletedCount} expired mappings"
            }, "Cleanup PayOS mappings successful");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup PayOS mappings");
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
        var requestId = GenerateRequestId();
        try
        {
            Logger.LogInformation("PayOS Cancel Callback #{RequestId} - OrderCode: {OrderCode}", requestId, orderCode);

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                return CreateParameterValidationError("OrderCode", requestId, GatewayName);
            }

            // Process cancel callback
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, "CANCELLED", true);

            Logger.LogInformation("PayOS Cancel Callback #{RequestId} - Processed - PaymentId: {PaymentId}", requestId, result.PaymentId);

            // Check if payment is for appointment and try to redirect to doctor booking page
            if (result.PaymentId != Guid.Empty)
            {
                var payment = await GetPaymentWithValidation(result.PaymentId, requestId, GatewayName);
                if (payment == null)
                {
                    return BadRequest("Payment not found");
                }

                var appointmentId = payment.AppointmentId;
                if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
                {
                    return await ProcessFailedAppointmentPaymentAsync(payment, result, requestId, GatewayName,
                        appointmentId!.Value, GetPayOSResponseMessage);
                }
            }

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
            Logger.LogWarning(ex, "PayOS Cancel Callback #{RequestId} - Invalid argument: {Error}", requestId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateProcessingErrorResponse(ex, requestId, GatewayName, orderCode);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate callback parameters
    /// </summary>
    private IActionResult? ValidateCallbackParameters(string orderCode, string requestId)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            return CreateParameterValidationError("OrderCode", requestId, GatewayName);
        }

        if (!long.TryParse(orderCode, out _))
        {
            Logger.LogWarning("PayOS Callback #{RequestId} - Invalid OrderCode format: {OrderCode}", requestId, orderCode);
            return BadRequest("Invalid OrderCode format");
        }

        return null;
    }

    /// <summary>
    /// Create response for already processed payments
    /// </summary>
    private IActionResult CreateAlreadyProcessedResponse(PayOSCallbackResponse result, string requestId)
    {
        var response = new
        {
            Success = result.Success,
            PaymentId = result.PaymentId,
            OrderCode = result.OrderCode,
            Code = result.ResponseCode,
            Amount = result.Amount,
            Message = result.Message,
            PaymentDate = result.PaymentDate,
            RequestId = requestId,
            ProcessedAt = DateTime.UtcNow,
            IsAlreadyProcessed = true
        };

        return Success(response, result.Success ? "PayOS payment successful" : "PayOS payment failed");
    }

    /// <summary>
    /// Create standard response for non-redirect scenarios
    /// </summary>
    private IActionResult CreateStandardResponse(PayOSCallbackResponse result, string requestId)
    {
        var response = new
        {
            Success = result.Success,
            PaymentId = result.PaymentId,
            OrderCode = result.OrderCode,
            Code = result.ResponseCode,
            Amount = result.Amount,
            Message = result.Message,
            PaymentDate = result.PaymentDate,
            RequestId = requestId,
            ProcessedAt = DateTime.UtcNow,
            IsAlreadyProcessed = false
        };

        return Success(response, result.Success ? "PayOS payment successful" : "PayOS payment failed");
    }

    /// <summary>
    /// Convert PayOS response code to human readable message
    /// </summary>
    private static string GetPayOSResponseMessage(string responseCode) => responseCode switch
    {
        "00" => "Transaction successful",
        "CANCELLED" => "Transaction cancelled by user",
        "FAILED" => "Transaction failed",
        "EXPIRED" => "Transaction expired",
        "PENDING" => "Transaction pending",
        _ => $"Unknown response code: {responseCode}"
    };

    #endregion
}