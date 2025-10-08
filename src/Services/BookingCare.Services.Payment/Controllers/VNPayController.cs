using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for VNPay integration
/// </summary>
[ApiVersion(ApiVersions.V1_0)]
public class VNPayController : BaseApiController
{
    private readonly IVNPayService _vnpayService;
    private readonly IPaymentService _paymentService;
    private readonly IValidator<VNPayPaymentRequest> _validator;
    private readonly ILogger<VNPayController> _logger;

    public VNPayController(
        IVNPayService vnpayService,
        IPaymentService paymentService,
        IValidator<VNPayPaymentRequest> validator,
        ILogger<VNPayController> logger)
    {
        _vnpayService = vnpayService;
        _paymentService = paymentService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Health check for VNPay service
    /// </summary>
    /// <returns>Status of VNPay service</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return Success(new
        {
            Service = "VNPay Integration",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = ApiVersions.V1_0
        }, "VNPay service is healthy");
    }

    /// <summary>
    /// Create VNPay payment URL
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <param name="forwardedFor">X-Forwarded-For header for client IP detection</param>
    /// <returns>URL to redirect to VNPay</returns>
    [HttpPost("create-payment-url")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] VNPayPaymentRequest request, [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor)
    {
        try
        {
            // Set client IP if not provided (before validation)
            if (string.IsNullOrEmpty(request.ClientIP))
            {
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    var forwardedIps = forwardedFor.Split(',');
                    request.ClientIP = forwardedIps.Length > 0 ? forwardedIps[0].Trim() : null;
                }
                else
                {
                    request.ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }
            }

            // Validate request data (now ClientIP is populated)
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest("Invalid request data", errors);
            }

            // Use shared payment validation helper (VNPay doesn't require PENDING status validation)
            var (validationError, _) = await PaymentValidationHelper.ValidatePaymentForGatewayAsync(
                _paymentService, request.PaymentId, request.Amount, validateStatus: false);

            if (validationError != null)
            {
                return validationError;
            }

            // Create VNPay payment URL
            var response = await _vnpayService.CreatePaymentUrlAsync(request);

            return Success(response, "Create VNPay payment URL successful");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating VNPay payment URL");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay payment URL for PaymentId: {PaymentId}", request.PaymentId);
            return StatusCode(500, new { Message = "An error occurred while creating VNPay payment URL" });
        }
    }

    /// <summary>
    /// Callback from VNPay after payment
    /// </summary>
    /// <returns>Callback processing result</returns>
    [HttpGet("callback")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> VNPayCallback()
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        try
        {
            // Get all query parameters
            var rawQueryParams = Request.Query.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToString()
            );

            _logger.LogInformation("VNPay Callback #{RequestId} received with {ParamCount} parameters", requestId, rawQueryParams.Count);

            // Process callback
            var callbackResult = await _vnpayService.ProcessCallbackAsync(rawQueryParams);

            // Extract PaymentId from TxnRef (format: PaymentId_Timestamp)
            var paymentIdStr = callbackResult.vnp_TxnRef.Split('_')[0];
            if (!Guid.TryParse(paymentIdStr, out var paymentId))
            {
                _logger.LogError("VNPay Callback #{RequestId} - Invalid PaymentId format in TxnRef: {TxnRef}", requestId, callbackResult.vnp_TxnRef);
                return BadRequest("Invalid transaction reference format");
            }

            // Update payment status based on VNPay result
            var newStatus = callbackResult.IsSuccess ? "COMPLETED" : "FAILED";
            await _paymentService.UpdateStatusAsync(new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId,
                Status = Enum.Parse<Shared.Common.Enums.PaymentStatus>(newStatus)
            });

            var message = GetVNPayResponseMessage(callbackResult.vnp_ResponseCode);

            // Unified response object similar to PayOS callback
            var unified = new
            {
                Success = callbackResult.IsSuccess,
                PaymentId = paymentId,
                OrderCode = callbackResult.vnp_TxnRef,
                Code = callbackResult.vnp_ResponseCode,
                Amount = callbackResult.GetActualAmount,
                Message = message,
                PaymentDate = callbackResult.GetPaymentDateTime(),
                RequestId = requestId,
                ProcessedAt = DateTime.UtcNow,
                IsAlreadyProcessed = false
            };

            if (callbackResult.IsSuccess)
            {
                _logger.LogInformation("VNPay Callback #{RequestId} - Payment completed successfully for PaymentId: {PaymentId}", requestId, paymentId);
            }
            else
            {
                _logger.LogWarning("VNPay Callback #{RequestId} - Payment failed for PaymentId: {PaymentId}, ResponseCode: {ResponseCode}",
                    requestId, paymentId, callbackResult.vnp_ResponseCode);
            }

            return Success(unified, callbackResult.IsSuccess ? "VNPay payment successful" : "VNPay payment failed");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "VNPay Callback #{RequestId} - Signature validation failed", requestId);
            return BadRequest("Invalid VNPay signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay Callback #{RequestId} - Error processing callback", requestId);
            return StatusCode(500, new { Message = "An error occurred while processing VNPay callback", RequestId = requestId });
        }
    }

    /// <summary>
    /// Query VNPay transaction status
    /// </summary>
    /// <param name="transactionRef">Transaction reference</param>
    /// <param name="transactionDate">Transaction date (yyyyMMdd)</param>
    /// <returns>Transaction information</returns>
    [HttpGet("query/{transactionRef}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> QueryTransaction(string transactionRef, [FromQuery] string transactionDate)
    {
        try
        {
            if (string.IsNullOrEmpty(transactionRef))
            {
                return BadRequest("Transaction reference must not be empty");
            }
            if (string.IsNullOrEmpty(transactionDate))
            {
                return BadRequest("Transaction date must not be empty");
            }
            var result = await _vnpayService.QueryTransactionAsync(transactionRef, transactionDate);
            return Success(result, "VNPay transaction query successful");
        }
        catch (NotImplementedException)
        {
            return BadRequest("VNPay transaction query feature is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying VNPay transaction: {TxnRef}", transactionRef);
            return StatusCode(500, new { Message = "An error occurred while querying VNPay transaction" });
        }
    }

    /// <summary>
    /// Convert VNPay response code to human readable message
    /// </summary>
    /// <param name="responseCode">VNPay response code</param>
    /// <returns>Human readable message</returns>
    private string GetVNPayResponseMessage(string responseCode) => responseCode switch
    {
        "00" => "Transaction successful",
        "07" => "Debit successful. Transaction is suspicious (possible fraud or unusual activity).",
        "09" => "Transaction failed: Card/account is not registered for InternetBanking at the bank.",
        "10" => "Transaction failed: Card/account authentication failed more than 3 times.",
        "11" => "Transaction failed: Payment waiting time expired. Please retry the transaction.",
        "12" => "Transaction failed: Card/account is blocked.",
        "13" => "Transaction failed: Incorrect OTP entered. Please retry.",
        "24" => "Transaction failed: Customer canceled the transaction.",
        "51" => "Transaction failed: Insufficient funds.",
        "65" => "Transaction failed: Daily transaction limit exceeded.",
        "75" => "The paying bank is under maintenance.",
        "79" => "Transaction failed: Payment password entered incorrectly too many times. Please retry.",
        "99" => "Other errors (not listed in known response codes)",
        _ => "Unknown error"
    };
}