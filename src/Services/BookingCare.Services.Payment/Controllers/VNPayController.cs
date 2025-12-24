using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Payment.Controllers.Base;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.DTOs.VNPay;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for VNPay integration
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class VNPayController : BasePaymentGatewayController
{
    private const string GatewayName = "VNPay";
    private readonly IVNPayService _vnpayService;
    private readonly IValidator<VNPayPaymentRequest> _validator;
    private readonly BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient _hospitalSubscriptionClient;

    public VNPayController(
        IVNPayService vnpayService,
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        IValidator<VNPayPaymentRequest> validator,
        ILogger<VNPayController> logger,
        AppointmentService.AppointmentServiceClient appointmentClient,
        BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient hospitalSubscriptionClient,
        BookingCare.Services.Discount.Protos.DiscountService.DiscountServiceClient discountClient
    )
        : base(paymentService, eventBus, frontendOptions, logger, appointmentClient, discountClient)
    {
        _vnpayService = vnpayService;
        _validator = validator;
        _hospitalSubscriptionClient = hospitalSubscriptionClient;
    }

    /// <summary>
    /// Health check for VNPay service
    /// </summary>
    /// <returns>Status of VNPay service</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return CreateHealthCheckResponse(GatewayName);
    }

    /// <summary>
    /// Create VNPay payment URL
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <param name="forwardedFor">X-Forwarded-For header for client IP detection</param>
    /// <returns>URL to redirect to VNPay</returns>
    [HttpPost("create-payment-url")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreatePaymentUrl(
        [FromBody] VNPayPaymentRequest request,
        [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor
    )
    {
        try
        {
            // Set client IP if not provided (before validation)
            if (string.IsNullOrEmpty(request.ClientIP))
            {
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    var forwardedIps = forwardedFor.Split(',');
                    request.ClientIP =
                        forwardedIps.Length > 0 ? forwardedIps[0].Trim() : string.Empty;
                }
                else
                {
                    request.ClientIP =
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
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
                PaymentService,
                request.PaymentId,
                request.Amount,
                validateStatus: false
            );

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
            Logger.LogWarning(ex, "Invalid argument when creating VNPay payment URL");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error creating VNPay payment URL for PaymentId: {PaymentId}",
                request.PaymentId
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while creating VNPay payment URL" }
            );
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
        var requestId = GenerateRequestId();
        try
        {
            // Get and validate query parameters
            var rawQueryParams = Request.Query.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToString()
            );
            Logger.LogInformation(
                "VNPay Callback #{RequestId} received with {ParamCount} parameters",
                requestId,
                rawQueryParams.Count
            );

            // Process callback and extract payment info
            var callbackResult = await _vnpayService.ProcessCallbackAsync(rawQueryParams);
            var paymentId = ExtractPaymentIdFromCallback(callbackResult, requestId);

            if (paymentId == null)
            {
                return BadRequest("Invalid transaction reference format");
            }

            // Update payment status
            await UpdatePaymentStatusAsync(paymentId.Value, callbackResult);

            // Get payment details
            var payment = await GetPaymentWithValidation(paymentId.Value, requestId, GatewayName);
            if (payment == null)
            {
                return BadRequest("Payment not found");
            }

            // Check if this is a subscription payment by parsing OrderInfo
            var subscriptionMetadata = ParseSubscriptionMetadata(callbackResult.vnp_OrderInfo);

            // Handle success or failure scenarios
            if (callbackResult.IsSuccess)
            {
                if (subscriptionMetadata.HasValue)
                {
                    // Handle subscription payment
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleSubscriptionPaymentSuccessAsync(
                                _hospitalSubscriptionClient,
                                subscriptionMetadata.Value.SubscriptionPlanId,
                                subscriptionMetadata.Value.HospitalId,
                                subscriptionMetadata.Value.IsUpgrade,
                                subscriptionMetadata.Value.CurrentHospitalSubscriptionId,
                                requestId,
                                GatewayName
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(
                                ex,
                                "VNPay Callback #{RequestId} - Failed to process subscription payment for HospitalId: {HospitalId}",
                                requestId,
                                subscriptionMetadata.Value.HospitalId
                            );
                        }
                    });

                    // Redirect to subscription confirmation page with plan type
                    var planType =
                        subscriptionMetadata.Value.PlanType?.ToLowerInvariant() ?? "monthly";
                    var frontendUrl =
                        $"{FrontendOptions.Admin.BaseUrl}hospitals/subscription-plan?plan-type={planType}";
                    Logger.LogInformation(
                        "VNPay Callback #{RequestId} - Subscription payment successful, redirecting to confirmation for HospitalId: {HospitalId}, PlanType: {PlanType}",
                        requestId,
                        subscriptionMetadata.Value.HospitalId,
                        planType
                    );
                    return Redirect(frontendUrl);
                }

                // Regular payment - use base handler
                return await HandleSuccessfulPayment(
                    payment,
                    callbackResult,
                    requestId,
                    GatewayName,
                    (p, r, reqId) => CreateVNPayResponse(p.Id, r, reqId, true)
                );
            }

            // Handle failed/cancelled payment
            if (subscriptionMetadata.HasValue)
            {
                // Subscription payment failed - redirect to subscription plan page
                return HandleSubscriptionPaymentFailed(
                    subscriptionMetadata.Value.PlanType,
                    subscriptionMetadata.Value.HospitalId,
                    requestId,
                    GatewayName,
                    "failed/cancelled",
                    callbackResult.vnp_ResponseCode
                );
            }

            // Regular payment failed - use base handler
            return await HandleFailedPaymentAsync(
                payment,
                callbackResult,
                requestId,
                GatewayName,
                payment.AppointmentId!.Value,
                GetVNPayResponseMessage,
                (p, r, reqId) => CreateVNPayResponse(p.Id, r, reqId, false)
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(
                ex,
                "VNPay Callback #{RequestId} - Signature validation failed",
                requestId
            );
            return BadRequest("Invalid VNPay signature");
        }
        catch (Exception ex)
        {
            return CreateProcessingErrorResponse(ex, requestId, GatewayName);
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
    public async Task<IActionResult> QueryTransaction(
        string transactionRef,
        [FromQuery] string transactionDate
    )
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
            Logger.LogError(ex, "Error querying VNPay transaction: {TxnRef}", transactionRef);
            return StatusCode(
                500,
                new { Message = "An error occurred while querying VNPay transaction" }
            );
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Extract and validate PaymentId from VNPay callback
    /// </summary>
    private Guid? ExtractPaymentIdFromCallback(
        VNPayCallbackResponse callbackResult,
        string requestId
    )
    {
        var paymentIdStr = callbackResult.vnp_TxnRef.Split('_')[0];
        if (!Guid.TryParse(paymentIdStr, out var paymentId))
        {
            Logger.LogError(
                "VNPay Callback #{RequestId} - Invalid PaymentId format in TxnRef: {TxnRef}",
                requestId,
                callbackResult.vnp_TxnRef
            );
            return null;
        }
        return paymentId;
    }

    /// <summary>
    /// Update payment status based on VNPay callback result
    /// </summary>
    private async Task UpdatePaymentStatusAsync(
        Guid paymentId,
        VNPayCallbackResponse callbackResult
    )
    {
        var newStatus = callbackResult.IsSuccess ? "COMPLETED" : "FAILED";
        await PaymentService.UpdateStatusAsync(
            new Models.DTOs.Requests.UpdatePaymentStatusRequest
            {
                Id = paymentId,
                Status = Enum.Parse<BookingCare.Services.Payment.Enums.PaymentStatus>(newStatus),
            }
        );
    }

    /// <summary>
    /// Create standard VNPay response for non-redirect scenarios
    /// </summary>
    private IActionResult CreateVNPayResponse(
        Guid paymentId,
        VNPayCallbackResponse callbackResult,
        string requestId,
        bool isSuccess
    )
    {
        var message = GetVNPayResponseMessage(callbackResult.vnp_ResponseCode);

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
            IsAlreadyProcessed = false,
        };

        if (isSuccess)
        {
            Logger.LogInformation(
                "VNPay Callback #{RequestId} - Payment completed successfully for PaymentId: {PaymentId}",
                requestId,
                paymentId
            );
        }
        else
        {
            Logger.LogWarning(
                "VNPay Callback #{RequestId} - Payment failed for PaymentId: {PaymentId}, ResponseCode: {ResponseCode}",
                requestId,
                paymentId,
                callbackResult.vnp_ResponseCode
            );
        }

        return Success(
            unified,
            callbackResult.IsSuccess ? "VNPay payment successful" : "VNPay payment failed"
        );
    }

    /// <summary>
    /// Convert VNPay response code to human readable message
    /// </summary>
    private static string GetVNPayResponseMessage(string responseCode) =>
        responseCode switch
        {
            "00" => "Transaction successful",
            "07" =>
                "Debit successful. Transaction is suspicious (possible fraud or unusual activity).",
            "09" =>
                "Transaction failed: Card/account is not registered for InternetBanking at the bank.",
            "10" => "Transaction failed: Card/account authentication failed more than 3 times.",
            "11" =>
                "Transaction failed: Payment waiting time expired. Please retry the transaction.",
            "12" => "Transaction failed: Card/account is blocked.",
            "13" => "Transaction failed: Incorrect OTP entered. Please retry.",
            "24" => "Transaction failed: Customer canceled the transaction.",
            "51" => "Transaction failed: Insufficient funds.",
            "65" => "Transaction failed: Daily transaction limit exceeded.",
            "75" => "The paying bank is under maintenance.",
            "79" =>
                "Transaction failed: Payment password entered incorrectly too many times. Please retry.",
            "99" => "Other errors (not listed in known response codes)",
            _ => "Unknown error",
        };

    /// <summary>
    /// Parse subscription metadata from VNPay OrderInfo
    /// Format: SUBSCRIPTION:{subscriptionId}:HOSPITAL:{hospitalId}:UPGRADE:{isUpgrade}:CURRENT:{currentSubId}:PLAN_TYPE:{planType}
    /// </summary>
    private (
        Guid SubscriptionPlanId,
        Guid HospitalId,
        bool IsUpgrade,
        Guid? CurrentHospitalSubscriptionId,
        string? PlanType
    )? ParseSubscriptionMetadata(string orderInfo)
    {
        if (string.IsNullOrEmpty(orderInfo) || !orderInfo.StartsWith("SUBSCRIPTION:"))
        {
            return null;
        }

        try
        {
            var parts = orderInfo.Split(':');

            if (parts.Length < 6) // Minimum: SUBSCRIPTION, id, HOSPITAL, id, UPGRADE, bool
            {
                return null;
            }

            var subscriptionPlanId = Guid.Parse(parts[1]);
            var hospitalId = Guid.Parse(parts[3]);
            var isUpgrade = bool.Parse(parts[5]);

            Guid? currentHospitalSubscriptionId = null;
            string? planType = null;

            // Parse optional fields
            for (int i = 6; i < parts.Length - 1; i++)
            {
                if (parts[i] == "CURRENT" && i + 1 < parts.Length)
                {
                    currentHospitalSubscriptionId = Guid.Parse(parts[i + 1]);
                }
                else if (parts[i] == "PLAN_TYPE" && i + 1 < parts.Length)
                {
                    planType = parts[i + 1];
                }
            }

            return (
                subscriptionPlanId,
                hospitalId,
                isUpgrade,
                currentHospitalSubscriptionId,
                planType
            );
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to parse subscription metadata from OrderInfo: {OrderInfo}",
                orderInfo
            );
            return null;
        }
    }

    #endregion
}
