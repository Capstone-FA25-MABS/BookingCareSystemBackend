using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Payment.Controllers.Base;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Services.Payment.Models.DTOs.Stripe;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Payment.Controllers;

/// <summary>
/// Controller for Stripe integration - handles webhooks and callbacks
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class StripeController : BasePaymentGatewayController
{
    private const string GatewayName = "Stripe";
    private const string SubscriptionPlanIdKey = "SubscriptionPlanId";
    private const string CancelledStatus = "cancelled";
    private readonly IStripeService _stripeService;
    private readonly BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient _hospitalSubscriptionClient;

    public StripeController(
        IStripeService stripeService,
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<StripeController> logger,
        AppointmentService.AppointmentServiceClient appointmentClient,
        BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient hospitalSubscriptionClient
    )
        : base(paymentService, eventBus, frontendOptions, logger, appointmentClient)
    {
        _stripeService = stripeService;
        _hospitalSubscriptionClient = hospitalSubscriptionClient;
    }

    /// <summary>
    /// Health check for Stripe service
    /// </summary>
    /// <returns>Status of Stripe service</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult HealthCheck()
    {
        return CreateHealthCheckResponse(GatewayName);
    }

    /// <summary>
    /// Stripe webhook endpoint - receives payment events from Stripe
    /// </summary>
    /// <returns>Webhook processing result</returns>
    [HttpPost("webhook")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> StripeWebhook()
    {
        var requestId = GenerateRequestId();

        try
        {
            // Read raw body
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();

            // Get Stripe signature from header
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(stripeSignature))
            {
                Logger.LogWarning(
                    "Stripe Webhook #{RequestId} - Missing Stripe-Signature header",
                    requestId
                );
                return BadRequest("Missing Stripe-Signature header");
            }

            Logger.LogInformation("Stripe Webhook #{RequestId} - Received event", requestId);

            // Process webhook
            var callbackResult = await _stripeService.ProcessWebhookAsync(json, stripeSignature);

            Logger.LogInformation(
                "Stripe Webhook #{RequestId} - Processed event, Status: {Status}, PaymentId: {PaymentId}",
                requestId,
                callbackResult.Status,
                callbackResult.PaymentId
            );

            // Handle successful payment
            if (callbackResult.IsSuccess && callbackResult.PaymentId != Guid.Empty)
            {
                // Check if this is a subscription payment
                var isSubscription =
                    callbackResult.Metadata != null
                    && callbackResult.Metadata.ContainsKey(SubscriptionPlanIdKey);

                if (isSubscription)
                {
                    await HandleSubscriptionPaymentAsync(callbackResult, requestId);
                }
                else
                {
                    // Check if this is a supplementary payment
                    var isSupplementary =
                        callbackResult.Metadata != null
                        && callbackResult.Metadata.ContainsKey("SupplementaryPaymentId");

                    if (isSupplementary)
                    {
                        await HandleSupplementaryPaymentAsync(callbackResult, requestId);
                    }
                    else
                    {
                        // Regular appointment payment
                        await HandleAppointmentPaymentAsync(callbackResult, requestId);
                    }
                }
            }

            // Return 200 OK to acknowledge receipt
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(
                "Stripe Webhook #{RequestId} - Signature validation failed: {Message}",
                requestId,
                ex.Message
            );
            return Unauthorized("Invalid signature");
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Webhook #{RequestId} - Error processing webhook",
                requestId
            );
            return StatusCode(500, "Error processing webhook");
        }
    }

    /// <summary>
    /// Success callback - user is redirected here after successful payment
    /// </summary>
    [HttpGet("success")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> StripeSuccess([FromQuery] string session_id)
    {
        var requestId = GenerateRequestId();

        try
        {
            Logger.LogInformation(
                "Stripe Success #{RequestId} - SessionId: {SessionId}",
                requestId,
                session_id
            );

            if (string.IsNullOrEmpty(session_id))
            {
                Logger.LogWarning("Stripe Success #{RequestId} - Missing session ID", requestId);
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=failed&gateway={GatewayName}&error=missing_session"
                );
            }

            // Get session details from Stripe
            var sessionData = await _stripeService.GetSessionAsync(session_id);
            var session = sessionData as Stripe.Checkout.Session;

            if (session == null || session.Metadata == null)
            {
                Logger.LogWarning(
                    "Stripe Success #{RequestId} - Invalid session data or missing metadata",
                    requestId
                );
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=failed&gateway={GatewayName}&error=invalid_session"
                );
            }

            // Extract PaymentId from metadata
            if (
                !session.Metadata.TryGetValue("PaymentId", out var paymentIdStr)
                || !Guid.TryParse(paymentIdStr, out var paymentId)
            )
            {
                Logger.LogWarning(
                    "Stripe Success #{RequestId} - Invalid PaymentId in metadata",
                    requestId
                );
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=failed&gateway={GatewayName}&error=invalid_payment"
                );
            }

            // Update payment status to COMPLETED and save PaymentIntentId
            await PaymentService.UpdatePaymentIntentAsync(
                paymentId,
                session.PaymentIntentId ?? string.Empty
            );

            await PaymentService.UpdateStatusAsync(
                new Models.DTOs.Requests.UpdatePaymentStatusRequest
                {
                    Id = paymentId,
                    Status = Enums.PaymentStatus.COMPLETED,
                }
            );

            // Get payment details
            var payment = await GetPaymentWithValidation(paymentId, requestId, GatewayName);
            if (payment == null)
            {
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=failed&gateway={GatewayName}&error=payment_not_found"
                );
            }

            // Check if this is a subscription payment and handle subscription payment success
            if (
                session.Metadata.ContainsKey(SubscriptionPlanIdKey)
                && session.Metadata.TryGetValue(SubscriptionPlanIdKey, out var subscriptionIdStr)
                && Guid.TryParse(subscriptionIdStr, out var subscriptionId)
                && session.Metadata.TryGetValue("HospitalId", out var hospitalIdStr)
                && Guid.TryParse(hospitalIdStr, out var hospitalId)
            )
            {
                {
                    var isUpgrade =
                        session.Metadata.TryGetValue("IsUpgrade", out var isUpgradeStr)
                        && bool.Parse(isUpgradeStr);

                    Guid? currentSubscriptionId = null;
                    if (
                        isUpgrade
                        && session.Metadata.TryGetValue(
                            "CurrentSubscriptionId",
                            out var currentSubIdStr
                        )
                        && Guid.TryParse(currentSubIdStr, out var currentSubId)
                    )
                    {
                        currentSubscriptionId = currentSubId;
                    }

                    // Process subscription via gRPC in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleSubscriptionPaymentSuccessAsync(
                                _hospitalSubscriptionClient,
                                subscriptionId,
                                hospitalId,
                                isUpgrade,
                                currentSubscriptionId,
                                requestId,
                                GatewayName
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(
                                ex,
                                "Stripe Success #{RequestId} - Failed to process subscription for HospitalId: {HospitalId}",
                                requestId,
                                hospitalId
                            );
                        }
                    });

                    // Redirect to subscription confirmation page
                    var planType = session.Metadata.TryGetValue("PlanType", out var planTypeStr)
                        ? planTypeStr.ToLowerInvariant()
                        : "monthly";
                    var frontendUrl =
                        $"{FrontendOptions.Admin.BaseUrl}hospitals/subscription-plan?plan-type={planType}";

                    Logger.LogInformation(
                        "Stripe Success #{RequestId} - Subscription payment successful, redirecting for HospitalId: {HospitalId}",
                        requestId,
                        hospitalId
                    );

                    return Redirect(frontendUrl);
                }
            }

            // Regular appointment payment - use base handler
            var callbackResponse = new StripeCallbackResponse
            {
                SessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId ?? string.Empty,
                Amount = session.AmountTotal ?? 0,
                Currency = session.Currency ?? "vnd",
                Status = session.Status ?? "unknown",
                CustomerEmail = session.CustomerEmail,
                PaymentMethodType = session.PaymentMethodTypes?.FirstOrDefault(),
                Metadata =
                    session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    ?? new Dictionary<string, string>(),
            };

            return await HandleSuccessfulPayment(
                payment,
                callbackResponse,
                requestId,
                GatewayName,
                (p, r, reqId) => CreateStripeResponse(p.Id, r, reqId, true)
            );
        }
        catch (Exception ex)
        {
            return CreateProcessingErrorResponse(ex, requestId, GatewayName);
        }
    }

    /// <summary>
    /// Cancel callback - user is redirected here when canceling payment
    /// </summary>
    [HttpGet("cancel")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> StripeCancel([FromQuery] string session_id)
    {
        var requestId = GenerateRequestId();

        try
        {
            Logger.LogInformation(
                "Stripe Cancel #{RequestId} - SessionId: {SessionId}",
                requestId,
                session_id ?? "null"
            );

            if (string.IsNullOrEmpty(session_id))
            {
                Logger.LogWarning("Stripe Cancel #{RequestId} - Missing session ID", requestId);
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=cancelled&gateway={GatewayName}&error=missing_session"
                );
            }

            // Get session details from Stripe
            var sessionData = await _stripeService.GetSessionAsync(session_id);
            var session = sessionData as Stripe.Checkout.Session;

            if (session == null || session.Metadata == null)
            {
                Logger.LogWarning("Stripe Cancel #{RequestId} - Invalid session data", requestId);
                var baseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
                return Redirect(
                    $"{baseUrl}?payment=cancelled&gateway={GatewayName}&error=invalid_session"
                );
            }

            // Extract PaymentId from metadata
            if (
                session.Metadata.TryGetValue("PaymentId", out var paymentIdStr)
                && Guid.TryParse(paymentIdStr, out var paymentId)
            )
            {
                // Update payment status to FAILED
                await PaymentService.UpdateStatusAsync(
                    new Models.DTOs.Requests.UpdatePaymentStatusRequest
                    {
                        Id = paymentId,
                        Status = Enums.PaymentStatus.FAILED,
                    }
                );

                // Get payment details
                var payment = await GetPaymentWithValidation(paymentId, requestId, GatewayName);

                if (payment != null)
                {
                    // Check if this is a subscription payment
                    var isSubscription = session.Metadata.ContainsKey(SubscriptionPlanIdKey);

                    if (isSubscription)
                    {
                        // Subscription payment cancelled - redirect to subscription plan page
                        var planType = session.Metadata.TryGetValue("PlanType", out var planTypeStr)
                            ? planTypeStr.ToLowerInvariant()
                            : "monthly";

                        session.Metadata.TryGetValue("HospitalId", out var hospitalIdStr);
                        var hospitalId = Guid.TryParse(hospitalIdStr, out var hId)
                            ? hId
                            : Guid.Empty;

                        return HandleSubscriptionPaymentFailed(
                            planType,
                            hospitalId,
                            requestId,
                            GatewayName,
                            CancelledStatus,
                            session.Status ?? CancelledStatus
                        );
                    }

                    // Regular appointment payment cancelled
                    var callbackResponse = new StripeCallbackResponse
                    {
                        SessionId = session.Id,
                        PaymentIntentId = session.PaymentIntentId ?? string.Empty,
                        Amount = session.AmountTotal ?? 0,
                        Currency = session.Currency ?? "vnd",
                        Status = CancelledStatus,
                        CustomerEmail = session.CustomerEmail,
                        PaymentMethodType = session.PaymentMethodTypes?.FirstOrDefault(),
                        Metadata =
                            session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                            ?? new Dictionary<string, string>(),
                    };

                    if (payment.AppointmentId.HasValue)
                    {
                        return await HandleFailedPaymentAsync(
                            payment,
                            callbackResponse,
                            requestId,
                            GatewayName,
                            payment.AppointmentId.Value,
                            GetStripeResponseMessage,
                            (p, r, reqId) => CreateStripeResponse(p.Id, r, reqId, false)
                        );
                    }
                }
            }

            // Fallback redirect
            Logger.LogWarning(
                "Stripe Cancel #{RequestId} - Payment cancelled without valid payment data",
                requestId
            );
            var cancelBaseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
            return Redirect(
                $"{cancelBaseUrl}?payment=cancelled&gateway={GatewayName}&sessionId={session_id}"
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Cancel #{RequestId} - Error processing cancel callback",
                requestId
            );

            var errorBaseUrl = FrontendOptions.Client.BaseUrl.TrimEnd('/');
            return Redirect(
                $"{errorBaseUrl}?payment=cancelled&gateway={GatewayName}&error=processing_error"
            );
        }
    }

    /// <summary>
    /// Handle appointment payment from Stripe webhook
    /// </summary>
    private async Task HandleAppointmentPaymentAsync(
        StripeCallbackResponse callbackResult,
        string requestId
    )
    {
        try
        {
            var paymentId = callbackResult.PaymentId;
            var payment = await GetPaymentWithValidation(paymentId, requestId, GatewayName);

            if (payment == null)
            {
                return;
            }

            // Update payment status
            var isSuccess =
                callbackResult.Status == "succeeded" || callbackResult.Status == "complete";
            await PaymentService.UpdateStatusAsync(
                new Models.DTOs.Requests.UpdatePaymentStatusRequest
                {
                    Id = paymentId,
                    Status = isSuccess ? Enums.PaymentStatus.COMPLETED : Enums.PaymentStatus.FAILED,
                }
            );

            Logger.LogInformation(
                "Stripe Webhook #{RequestId} - Appointment payment processed for PaymentId: {PaymentId}, Status: {Status}",
                requestId,
                paymentId,
                isSuccess ? "Success" : "Failed"
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Webhook #{RequestId} - Error processing appointment payment",
                requestId
            );
        }
    }

    /// <summary>
    /// Handle supplementary payment from Stripe webhook
    /// </summary>
    private async Task HandleSupplementaryPaymentAsync(
        StripeCallbackResponse callbackResult,
        string requestId
    )
    {
        try
        {
            if (
                callbackResult.Metadata == null
                || !callbackResult.Metadata.TryGetValue("AppointmentId", out var appointmentIdStr)
                || !Guid.TryParse(appointmentIdStr, out var appointmentId)
            )
            {
                Logger.LogWarning(
                    "Stripe Webhook #{RequestId} - Invalid AppointmentId in supplementary payment metadata",
                    requestId
                );
                return;
            }

            var isStaffAssigned =
                callbackResult.Metadata.TryGetValue("IsStaffAssigned", out var isStaffAssignedStr)
                && bool.Parse(isStaffAssignedStr);

            if (callbackResult.IsSuccess)
            {
                // Get current payment for this appointment
                var currentPayment = await PaymentService.GetByAppointmentIdAsync(appointmentId);
                if (currentPayment != null)
                {
                    await HandleSupplementaryPaymentSuccessAsync(
                        appointmentId,
                        currentPayment,
                        callbackResult,
                        requestId,
                        GatewayName,
                        isStaffAssigned
                    );
                }

                Logger.LogInformation(
                    "Stripe Webhook #{RequestId} - Supplementary payment successful for AppointmentId: {AppointmentId}",
                    requestId,
                    appointmentId
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Webhook #{RequestId} - Error processing supplementary payment",
                requestId
            );
        }
    }

    /// <summary>
    /// Handle subscription payment from Stripe webhook
    /// </summary>
    private async Task HandleSubscriptionPaymentAsync(
        StripeCallbackResponse callbackResult,
        string requestId
    )
    {
        try
        {
            if (callbackResult.Metadata == null)
            {
                Logger.LogWarning(
                    "Stripe Webhook #{RequestId} - Missing metadata in subscription payment",
                    requestId
                );
                return;
            }

            var metadata = callbackResult.Metadata;

            if (
                !metadata.TryGetValue(SubscriptionPlanIdKey, out var subscriptionIdStr)
                || !Guid.TryParse(subscriptionIdStr, out var subscriptionId)
            )
            {
                Logger.LogError(
                    "Stripe Webhook #{RequestId} - Invalid SubscriptionPlanId in metadata",
                    requestId
                );
                return;
            }

            if (
                !metadata.TryGetValue("HospitalId", out var hospitalIdStr)
                || !Guid.TryParse(hospitalIdStr, out var hospitalId)
            )
            {
                Logger.LogWarning(
                    "Stripe Webhook #{RequestId} - Invalid HospitalId in metadata",
                    requestId
                );
                return;
            }

            var isUpgrade =
                metadata.TryGetValue("IsUpgrade", out var isUpgradeStr) && bool.Parse(isUpgradeStr);

            Guid? currentSubscriptionId = null;
            if (
                isUpgrade
                && metadata.TryGetValue("CurrentSubscriptionId", out var currentSubIdStr)
                && Guid.TryParse(currentSubIdStr, out var currentSubId)
            )
            {
                currentSubscriptionId = currentSubId;
            }

            if (callbackResult.IsSuccess)
            {
                // Update payment status
                await PaymentService.UpdateStatusAsync(
                    new Models.DTOs.Requests.UpdatePaymentStatusRequest
                    {
                        Id = callbackResult.PaymentId,
                        Status = Enums.PaymentStatus.COMPLETED,
                    }
                );

                // Use base class helper to handle subscription
                await HandleSubscriptionPaymentSuccessAsync(
                    _hospitalSubscriptionClient,
                    subscriptionId,
                    hospitalId,
                    isUpgrade,
                    currentSubscriptionId,
                    requestId,
                    GatewayName
                );

                Logger.LogInformation(
                    "Stripe Webhook #{RequestId} - Subscription payment successful for HospitalId: {HospitalId}",
                    requestId,
                    hospitalId
                );
            }
            else
            {
                // Update payment status as failed
                await PaymentService.UpdateStatusAsync(
                    new Models.DTOs.Requests.UpdatePaymentStatusRequest
                    {
                        Id = callbackResult.PaymentId,
                        Status = Enums.PaymentStatus.FAILED,
                    }
                );

                Logger.LogWarning(
                    "Stripe Webhook #{RequestId} - Subscription payment failed for HospitalId: {HospitalId}",
                    requestId,
                    hospitalId
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Webhook #{RequestId} - Error processing subscription payment",
                requestId
            );
        }
    }

    /// <summary>
    /// Override to get amount from Stripe callback
    /// </summary>
    protected override decimal GetAmountFromCallback<TResponse>(TResponse callbackResult)
    {
        if (callbackResult is StripeCallbackResponse stripeCallback)
        {
            // Stripe amounts are in smallest currency unit (cents for USD, 1 for VND)
            // VND doesn't have decimal places, so we just convert directly
            return stripeCallback.Amount;
        }
        return base.GetAmountFromCallback(callbackResult);
    }

    #region Private Helper Methods

    /// <summary>
    /// Create standard Stripe response for non-redirect scenarios
    /// </summary>
    private IActionResult CreateStripeResponse(
        Guid paymentId,
        StripeCallbackResponse callbackResult,
        string requestId,
        bool isSuccess
    )
    {
        var message = GetStripeResponseMessage(callbackResult.Status);

        var unified = new
        {
            Success = callbackResult.IsSuccess,
            PaymentId = paymentId,
            SessionId = callbackResult.SessionId,
            PaymentIntentId = callbackResult.PaymentIntentId,
            Status = callbackResult.Status,
            Amount = callbackResult.Amount,
            Message = message,
            RequestId = requestId,
            ProcessedAt = DateTime.UtcNow,
        };

        if (isSuccess)
        {
            Logger.LogInformation(
                "Stripe Callback #{RequestId} - Payment completed successfully for PaymentId: {PaymentId}",
                requestId,
                paymentId
            );
        }
        else
        {
            Logger.LogWarning(
                "Stripe Callback #{RequestId} - Payment failed for PaymentId: {PaymentId}, Status: {Status}",
                requestId,
                paymentId,
                callbackResult.Status
            );
        }

        return Success(
            unified,
            callbackResult.IsSuccess ? "Stripe payment successful" : "Stripe payment failed"
        );
    }

    /// <summary>
    /// Convert Stripe status to human readable message
    /// </summary>
    private static string GetStripeResponseMessage(string status) =>
        status switch
        {
            "complete" or "succeeded" => "Payment completed successfully",
            "processing" => "Payment is being processed",
            "requires_payment_method" => "Payment requires a payment method",
            "requires_confirmation" => "Payment requires confirmation",
            "requires_action" => "Payment requires additional action",
            "canceled" or "cancelled" => "Payment was cancelled by the customer",
            "failed" => "Payment failed",
            "expired" => "Payment session has expired",
            _ => "Unknown payment status",
        };

    #endregion

    #region Refund Operations

    /// <summary>
    /// Create a refund for a Stripe payment
    /// </summary>
    /// <param name="request">Refund request information</param>
    /// <returns>Refund result</returns>
    [HttpPost("refund")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateRefund([FromBody] StripeRefundRequest request)
    {
        var requestId = GenerateRequestId();

        try
        {
            Logger.LogInformation(
                "Stripe Refund #{RequestId} - Creating refund for PaymentId: {PaymentId}",
                requestId,
                request.PaymentId
            );

            // Validate request
            if (request.PaymentId == Guid.Empty)
            {
                return BadRequest("PaymentId is required");
            }

            // Get payment to validate
            var payment = await PaymentService.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                Logger.LogWarning(
                    "Stripe Refund #{RequestId} - Payment not found: {PaymentId}",
                    requestId,
                    request.PaymentId
                );
                return NotFound("Payment not found");
            }

            // Check if PaymentIntentId exists
            if (string.IsNullOrEmpty(payment.PaymentIntentId))
            {
                Logger.LogWarning(
                    "Stripe Refund #{RequestId} - PaymentIntentId not found for PaymentId: {PaymentId}",
                    requestId,
                    request.PaymentId
                );
                return BadRequest(
                    "PaymentIntentId not found for this payment. Cannot process refund."
                );
            }

            // Set PaymentIntentId from stored payment data
            request.PaymentIntentId = payment.PaymentIntentId;

            // Check if payment is already refunded
            if (payment.Status == Enums.PaymentStatus.REFUNDED)
            {
                Logger.LogWarning(
                    "Stripe Refund #{RequestId} - Payment already refunded: {PaymentId}",
                    requestId,
                    request.PaymentId
                );
                return BadRequest("Payment has already been refunded");
            }

            // Check if payment is completed
            if (payment.Status != Enums.PaymentStatus.COMPLETED)
            {
                Logger.LogWarning(
                    "Stripe Refund #{RequestId} - Payment not completed: {PaymentId}, Status: {Status}",
                    requestId,
                    request.PaymentId,
                    payment.Status
                );
                return BadRequest("Only completed payments can be refunded");
            }

            // Create refund via Stripe
            var refundResponse = await _stripeService.CreateRefundAsync(request);

            if (!refundResponse.IsSuccess)
            {
                Logger.LogWarning(
                    "Stripe Refund #{RequestId} - Refund failed: {PaymentId}, Status: {Status}",
                    requestId,
                    request.PaymentId,
                    refundResponse.Status
                );
                return BadRequest($"Refund failed with status: {refundResponse.Status}");
            }

            // Update payment status to REFUNDED
            await PaymentService.UpdateStatusAsync(
                new Models.DTOs.Requests.UpdatePaymentStatusRequest
                {
                    Id = request.PaymentId,
                    Status = Enums.PaymentStatus.REFUNDED,
                }
            );

            Logger.LogInformation(
                "Stripe Refund #{RequestId} - Refund successful: PaymentId: {PaymentId}, RefundId: {RefundId}",
                requestId,
                request.PaymentId,
                refundResponse.RefundId
            );

            return Success(
                new
                {
                    PaymentId = request.PaymentId,
                    RefundId = refundResponse.RefundId,
                    Status = refundResponse.Status,
                    Amount = refundResponse.Amount,
                    Currency = refundResponse.Currency,
                    Reason = refundResponse.Reason,
                    CreatedAt = refundResponse.CreatedAt,
                    Message = "Refund processed successfully",
                },
                "Stripe refund created successfully"
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Refund #{RequestId} - Error creating refund for PaymentId: {PaymentId}",
                requestId,
                request.PaymentId
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while processing the refund" }
            );
        }
    }

    /// <summary>
    /// Get refund information from Stripe
    /// </summary>
    /// <param name="refundId">Stripe refund ID</param>
    /// <returns>Refund information</returns>
    [HttpGet("refund/{refundId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRefund(string refundId)
    {
        var requestId = GenerateRequestId();

        try
        {
            Logger.LogInformation(
                "Stripe Refund Query #{RequestId} - Getting refund: {RefundId}",
                requestId,
                refundId
            );

            if (string.IsNullOrEmpty(refundId))
            {
                return BadRequest("RefundId is required");
            }

            var refund = await _stripeService.GetRefundAsync(refundId);

            return Success(refund, "Refund information retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Stripe Refund Query #{RequestId} - Error getting refund: {RefundId}",
                requestId,
                refundId
            );
            return StatusCode(
                500,
                new { Message = "An error occurred while retrieving refund information" }
            );
        }
    }

    #endregion
}
