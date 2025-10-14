using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.PayOS;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using System.Text.Json;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for PayOS integration
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class PayOSController : BaseApiController
{
    private readonly IPayOSService _payOSService;
    private readonly IPaymentService _paymentService;
    private readonly IEventBus _eventBus;
    private readonly FrontendOptions _frontendOptions;
    private readonly IValidator<PayOSPaymentRequest> _validator;
    private readonly ILogger<PayOSController> _logger;
    private readonly AppointmentService.AppointmentServiceClient _appointmentClient;

    public PayOSController(
        IPayOSService payOSService,
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        IValidator<PayOSPaymentRequest> validator,
        ILogger<PayOSController> logger,
        AppointmentService.AppointmentServiceClient appointmentClient)
    {
        _payOSService = payOSService;
        _paymentService = paymentService;
        _eventBus = eventBus;
        _frontendOptions = frontendOptions.Value;
        _validator = validator;
        _logger = logger;
        _appointmentClient = appointmentClient;
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

            // Use shared payment validation helper (PayOS requires PENDING status validation)
            var (validationError, _) = await PaymentValidationHelper.ValidatePaymentForGatewayAsync(
                _paymentService, request.PaymentId, request.Amount, validateStatus: true);

            if (validationError != null)
            {
                return validationError;
            }

            // Create PayOS payment link
            var payOSResponse = await _payOSService.CreatePaymentLinkAsync(request);

            _logger.LogInformation("PayOS payment link created successfully for PaymentId: {PaymentId}, OrderCode: {OrderCode}",
                request.PaymentId, payOSResponse.OrderCode);

            return Success(payOSResponse, "Create PayOS payment link successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "PayOS payment creation failed - Invalid argument");
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
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, code ?? string.Empty, cancel);

            // Enhanced logging with request tracking
            _logger.LogInformation("PayOS Callback #{RequestId} - Processed successfully - PaymentId: {PaymentId}, Success: {Success}, IsEmptyGuid: {IsEmptyGuid}",
                requestId, result.PaymentId, result.Success, result.PaymentId == Guid.Empty);

            // If payment was successful and has a valid PaymentId, check for appointment redirect
            if (result.Success && result.PaymentId != Guid.Empty)
            {
                // Get payment details to check if it's for an appointment
                var payment = await _paymentService.GetByIdAsync(result.PaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("PayOS Callback #{RequestId} - Payment not found for PaymentId: {PaymentId}", requestId, result.PaymentId);
                    return BadRequest("Payment not found");
                }

                var appointmentId = payment.AppointmentId;
                if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
                {
                    var apptId = appointmentId!.Value;
                    var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(_frontendOptions, apptId, true);
                    _logger.LogInformation("PayOS Callback #{RequestId} - Redirecting to frontend for appointment: {AppointmentId}, URL: {RedirectUrl}",
                        requestId, apptId, frontendUrl);

                    return Redirect(frontendUrl);
                }
            }
            else if (!result.Success && result.PaymentId != Guid.Empty)
            {
                // Payment failed - try to get doctorId and redirect to doctor booking page
                var payment = await _paymentService.GetByIdAsync(result.PaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("PayOS Callback #{RequestId} - Payment not found for PaymentId: {PaymentId}", requestId, result.PaymentId);
                    return BadRequest("Payment not found");
                }

                var appointmentId = payment.AppointmentId;
                if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
                {
                    var apptId = appointmentId!.Value;
                    // Try to get doctorId using gRPC
                    var doctorId = await PaymentFrontendHelper.GetDoctorIdFromAppointmentAsync(
                        _appointmentClient, apptId);

                    // Publish appointment deletion event for failed payment
                    await PaymentEventHelper.PublishAppointmentDeleteEventAsync(
                        payment,
                        result.ResponseCode,
                        "PayOS",
                        requestId,
                        _appointmentClient,
                        _eventBus,
                        _logger,
                        GetPayOSResponseMessage);

                    if (doctorId.HasValue)
                    {
                        // Redirect to doctor's booking page
                        var doctorBookingUrl = PaymentFrontendHelper.BuildDoctorBookingRedirectUrl(
                            _frontendOptions, doctorId.Value);

                        _logger.LogInformation("PayOS Callback #{RequestId} - Payment failed, redirecting to doctor booking page: {DoctorId}, URL: {RedirectUrl}",
                            requestId, doctorId.Value, doctorBookingUrl);

                        return Redirect(doctorBookingUrl);
                    }
                    else
                    {
                        // Fallback to original error page if can't get doctorId
                        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
                            _frontendOptions, apptId, false);

                        _logger.LogWarning("PayOS Callback #{RequestId} - Payment failed, could not get doctorId, redirecting to original error page for appointment: {AppointmentId}",
                            requestId, apptId);

                        return Redirect(frontendUrl);
                    }
                }
            }

            // Create response with details for non-appointment payments or API calls
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
            _logger.LogWarning(ex, "PayOS Callback #{RequestId} - Processing failed - Invalid argument", requestId);
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
        var requestId = Guid.NewGuid().ToString("N")[..8];
        try
        {
            _logger.LogInformation("PayOS Cancel Callback #{RequestId} - OrderCode: {OrderCode}", requestId, orderCode);

            if (!long.TryParse(orderCode, out var orderCodeLong))
            {
                _logger.LogWarning("PayOS Cancel Callback #{RequestId} - Invalid OrderCode format: {OrderCode}", requestId, orderCode);
                return BadRequest("Invalid order code format");
            }

            // Process cancel callback
            var result = await _payOSService.ProcessCallbackAsync(orderCodeLong, "CANCELLED", true);

            _logger.LogInformation("PayOS Cancel Callback #{RequestId} - Processed - PaymentId: {PaymentId}", requestId, result.PaymentId);

            // Check if payment is for appointment and try to redirect to doctor booking page
            if (result.PaymentId != Guid.Empty)
            {
                var payment = await _paymentService.GetByIdAsync(result.PaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("PayOS Cancel Callback #{RequestId} - Payment not found for PaymentId: {PaymentId}", requestId, result.PaymentId);
                    return BadRequest("Payment not found");
                }

                var appointmentId = payment.AppointmentId;
                if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
                {
                    var apptId = appointmentId!.Value;
                    // Try to get doctorId using gRPC
                    var doctorId = await PaymentFrontendHelper.GetDoctorIdFromAppointmentAsync(
                        _appointmentClient, apptId);

                    // Publish appointment deletion event for cancelled payment
                    await PaymentEventHelper.PublishAppointmentDeleteEventAsync(
                        payment,
                        "CANCELLED",
                        "PayOS",
                        $"CANCEL-{Guid.NewGuid().ToString("N")[..8]}",
                        _appointmentClient,
                        _eventBus,
                        _logger,
                        GetPayOSResponseMessage);

                    if (doctorId.HasValue)
                    {
                        // Redirect to doctor's booking page
                        var doctorBookingUrl = PaymentFrontendHelper.BuildDoctorBookingRedirectUrl(
                            _frontendOptions, doctorId.Value);

                        _logger.LogInformation("PayOS Cancel Callback #{RequestId} - Redirecting to doctor booking page: {DoctorId}, URL: {RedirectUrl}",
                            requestId, doctorId.Value, doctorBookingUrl);

                        return Redirect(doctorBookingUrl);
                    }
                    else
                    {
                        // Fallback to original error page if can't get doctorId
                        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
                            _frontendOptions, apptId, false);

                        _logger.LogWarning("PayOS Cancel Callback #{RequestId} - Could not get doctorId, redirecting to original error page for appointment: {AppointmentId}",
                            requestId, apptId);

                        return Redirect(frontendUrl);
                    }
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
            _logger.LogWarning(ex, "PayOS Cancel Callback #{RequestId} - Invalid argument: {Error}", requestId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Cancel Callback #{RequestId} - Error processing cancel callback for OrderCode: {OrderCode}", requestId, orderCode);
            return StatusCode(500, new { Message = "An error occurred while processing PayOS cancel callback" });
        }
    }

    /// <summary>
    /// Convert PayOS response code to human readable message
    /// </summary>
    /// <param name="responseCode">PayOS response code</param>
    /// <returns>Human readable message</returns>
    private static string GetPayOSResponseMessage(string responseCode) => responseCode switch
    {
        "00" => "Transaction successful",
        "CANCELLED" => "Transaction cancelled by user",
        "FAILED" => "Transaction failed",
        "EXPIRED" => "Transaction expired",
        "PENDING" => "Transaction pending",
        _ => $"Unknown response code: {responseCode}"
    };
}