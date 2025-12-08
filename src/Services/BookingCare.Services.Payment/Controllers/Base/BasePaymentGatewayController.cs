using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DiscountProtos = BookingCare.Services.Discount.Protos;

namespace BookingCare.Services.Payment.Controllers.Base;

/// <summary>
/// Base controller for payment gateway integrations with common functionality
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public abstract class BasePaymentGatewayController : BaseApiController
{
    protected readonly IPaymentService PaymentService;
    protected readonly IEventBus EventBus;
    protected readonly FrontendOptions FrontendOptions;
    protected readonly ILogger Logger;
    protected readonly AppointmentService.AppointmentServiceClient AppointmentClient;
    protected readonly DiscountProtos.DiscountService.DiscountServiceClient? DiscountClient;

    protected BasePaymentGatewayController(
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        ILogger logger,
        AppointmentService.AppointmentServiceClient appointmentClient,
        DiscountProtos.DiscountService.DiscountServiceClient? discountClient = null
    )
    {
        PaymentService = paymentService;
        EventBus = eventBus;
        FrontendOptions = frontendOptions.Value;
        Logger = logger;
        AppointmentClient = appointmentClient;
        DiscountClient = discountClient;
    }

    /// <summary>
    /// Common health check implementation
    /// </summary>
    protected IActionResult CreateHealthCheckResponse(string serviceName)
    {
        return Success(
            new
            {
                Service = $"{serviceName} Integration",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = ApiVersions.V1_0,
            },
            $"{serviceName} service is healthy"
        );
    }

    /// <summary>
    /// Common payment retrieval with validation
    /// </summary>
    protected async Task<PaymentResponse?> GetPaymentWithValidation(
        Guid paymentId,
        string requestId,
        string gatewayName
    )
    {
        var payment = await PaymentService.GetByIdAsync(paymentId);
        if (payment == null)
        {
            Logger.LogWarning(
                "{Gateway} Callback #{RequestId} - Payment not found for PaymentId: {PaymentId}",
                gatewayName,
                requestId,
                paymentId
            );
        }
        return payment;
    }

    /// <summary>
    /// Handle successful payment scenario - common logic
    /// Supports both regular payments and supplementary payments (price difference)
    /// </summary>
    protected async Task<IActionResult> HandleSuccessfulPayment<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Func<PaymentResponse, TResponse, string, IActionResult> createResponseFunc
    )
        where TResponse : class
    {
        var appointmentId = payment.AppointmentId;
        var metadata = ExtractMetadataFromCallback(callbackResult);
        var isSupplementaryPayment = IsSupplementaryPayment(metadata, out var suppAppointmentId);

        if (isSupplementaryPayment && suppAppointmentId.HasValue)
        {
            return await HandleSupplementaryPaymentFlow(
                payment,
                callbackResult,
                requestId,
                gatewayName,
                suppAppointmentId.Value,
                metadata
            );
        }

        ProcessRegularPaymentAsync(payment, callbackResult, requestId, gatewayName);

        return HandleFrontendRedirectOrResponse(
            payment,
            callbackResult,
            requestId,
            gatewayName,
            appointmentId,
            createResponseFunc
        );
    }

    /// <summary>
    /// Handle supplementary payment flow
    /// </summary>
    private async Task<IActionResult> HandleSupplementaryPaymentFlow<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Guid suppAppointmentId,
        string? metadata
    )
        where TResponse : class
    {
        var isStaffAssigned = ExtractIsStaffAssigned(metadata);

        try
        {
            await HandleSupplementaryPaymentSuccessAsync(
                suppAppointmentId,
                payment,
                callbackResult,
                requestId,
                gatewayName,
                isStaffAssigned
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "{Gateway} Callback #{RequestId} - Failed to process supplementary payment for AppointmentId: {AppointmentId}",
                gatewayName,
                requestId,
                suppAppointmentId
            );
        }

        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
            FrontendOptions,
            suppAppointmentId,
            true
        );
        Logger.LogInformation(
            "{Gateway} Callback #{RequestId} - Supplementary payment successful, redirecting to confirmation for AppointmentId: {AppointmentId}",
            gatewayName,
            requestId,
            suppAppointmentId
        );
        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Process regular payment asynchronously
    /// </summary>
    private void ProcessRegularPaymentAsync<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName
    )
        where TResponse : class
    {
        if (!payment.AppointmentId.HasValue || !payment.PatientId.HasValue)
        {
            return;
        }

        var capturedData = CapturePaymentData(payment);

        _ = Task.Run(async () =>
        {
            await ProcessDiscountUsageAsync(capturedData, requestId, gatewayName);
            await PublishPaymentSuccessEventAsync(
                capturedData,
                callbackResult,
                requestId,
                gatewayName
            );
        });
    }

    /// <summary>
    /// Capture payment data to avoid DbContext disposed error
    /// </summary>
    private CapturedPaymentData CapturePaymentData(PaymentResponse payment)
    {
        return new CapturedPaymentData
        {
            DiscountId = payment.DiscountId,
            DiscountCode = payment.DiscountCode,
            HospitalId = payment.HospitalId,
            Amount = payment.Amount,
            PaymentId = payment.Id,
            PatientId = payment.PatientId!.Value,
            AppointmentId = payment.AppointmentId!.Value,
        };
    }

    /// <summary>
    /// Process discount usage via gRPC
    /// </summary>
    private async Task ProcessDiscountUsageAsync(
        CapturedPaymentData data,
        string requestId,
        string gatewayName
    )
    {
        if (
            !data.DiscountId.HasValue
            || string.IsNullOrWhiteSpace(data.DiscountCode)
            || DiscountClient == null
            || !data.HospitalId.HasValue
        )
        {
            return;
        }

        try
        {
            var useDiscountRequest = new DiscountProtos.UseDiscountRequest
            {
                Code = data.DiscountCode,
                HospitalId = data.HospitalId.ToString()!,
                TotalAmount = (double)data.Amount,
            };

            var useDiscountResponse = await DiscountClient.UseDiscountAsync(useDiscountRequest);

            if (useDiscountResponse.Success)
            {
                Logger.LogInformation(
                    "{Gateway} Callback #{RequestId} - Discount usage incremented successfully for DiscountId: {DiscountId}, Remaining: {Remaining}",
                    gatewayName,
                    requestId,
                    data.DiscountId,
                    useDiscountResponse.RemainingUses
                );
            }
            else
            {
                Logger.LogWarning(
                    "{Gateway} Callback #{RequestId} - Failed to increment discount usage: {Message}",
                    gatewayName,
                    requestId,
                    useDiscountResponse.Message
                );
            }
        }
        catch (Exception discountEx)
        {
            Logger.LogError(
                discountEx,
                "{Gateway} Callback #{RequestId} - Error incrementing discount usage for DiscountId: {DiscountId}",
                gatewayName,
                requestId,
                data.DiscountId
            );
        }
    }

    /// <summary>
    /// Publish payment success event
    /// </summary>
    private async Task PublishPaymentSuccessEventAsync<TResponse>(
        CapturedPaymentData data,
        TResponse callbackResult,
        string requestId,
        string gatewayName
    )
        where TResponse : class
    {
        try
        {
            var paymentSuccessEvent = new AppointmentPaymentSuccessIntegrationEvent
            {
                AppointmentId = data.AppointmentId,
                PatientId = data.PatientId,
                PaymentId = data.PaymentId,
                Amount = data.Amount,
                PaymentMethod = gatewayName,
                TransactionId = GetTransactionIdFromCallback(callbackResult),
                PaymentCompletedAt = DateTime.UtcNow,
                CorrelationId = requestId,
            };

            await EventBus.PublishAsync(paymentSuccessEvent);

            Logger.LogInformation(
                "{Gateway} Callback #{RequestId} - Published appointment payment success event for AppointmentId: {AppointmentId}",
                gatewayName,
                requestId,
                data.AppointmentId
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "{Gateway} Callback #{RequestId} - Failed to publish payment success event for AppointmentId: {AppointmentId}",
                gatewayName,
                requestId,
                data.AppointmentId
            );
        }
    }

    /// <summary>
    /// Handle frontend redirect or create response
    /// </summary>
    private IActionResult HandleFrontendRedirectOrResponse<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Guid? appointmentId,
        Func<PaymentResponse, TResponse, string, IActionResult> createResponseFunc
    )
        where TResponse : class
    {
        if (!PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
        {
            return createResponseFunc(payment, callbackResult, requestId);
        }

        if (!appointmentId.HasValue)
        {
            Logger.LogError(
                "{Gateway} Callback #{RequestId} - Cannot redirect to frontend: appointmentId is null",
                gatewayName,
                requestId
            );
            return createResponseFunc(payment, callbackResult, requestId);
        }

        var apptId = appointmentId.Value;
        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
            FrontendOptions,
            apptId,
            true
        );
        Logger.LogInformation(
            "{Gateway} Callback #{RequestId} - Redirecting to frontend for appointment: {AppointmentId}, URL: {RedirectUrl}",
            gatewayName,
            requestId,
            apptId,
            frontendUrl
        );

        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Data structure to capture payment data
    /// </summary>
    private sealed class CapturedPaymentData
    {
        public Guid? DiscountId { get; init; }
        public string? DiscountCode { get; init; }
        public Guid? HospitalId { get; init; }
        public decimal Amount { get; init; }
        public Guid PaymentId { get; init; }
        public Guid PatientId { get; init; }
        public Guid AppointmentId { get; init; }
    }

    /// <summary>
    /// Handle failed payment scenario - common logic
    /// </summary>
    protected async Task<IActionResult> HandleFailedPaymentAsync<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Guid appointmentId,
        Func<string, string> responseMessageFunc,
        Func<PaymentResponse, TResponse, string, IActionResult> createResponseFunc
    )
        where TResponse : class
    {
        var appointmentIdValue = payment.AppointmentId;

        if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentIdValue))
        {
            return await ProcessFailedAppointmentPaymentAsync(
                payment,
                callbackResult,
                requestId,
                gatewayName,
                appointmentId,
                responseMessageFunc
            );
        }

        return createResponseFunc(payment, callbackResult, requestId);
    }

    /// <summary>
    /// Process failed appointment payment with cleanup and redirection - common logic
    /// </summary>
    protected async Task<IActionResult> ProcessFailedAppointmentPaymentAsync<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Guid appointmentId,
        Func<string, string> responseMessageFunc
    )
        where TResponse : class
    {
        // Check if this is a supplementary payment (price difference payment)
        var metadata = ExtractMetadataFromCallback(callbackResult);
        var isSupplementaryPayment = IsSupplementaryPayment(metadata, out var _);

        // Try to get doctorId using gRPC
        var (
            doctorId,
            pendingDoctorId,
            assignedDoctorId,
            rescheduleToken,
            hospitalId,
            specialtyId
        ) = await PaymentFrontendHelper.GetDoctorIdFromAppointmentAsync(
            AppointmentClient,
            appointmentId
        );

        // If it's a supplementary payment, DO NOT delete appointment - just clear pending changes
        if (isSupplementaryPayment)
        {
            Logger.LogWarning(
                "{Gateway} Callback #{RequestId} - Supplementary payment failed/cancelled for AppointmentId: {AppointmentId}. Appointment will NOT be deleted, pending changes retained.",
                gatewayName,
                requestId,
                appointmentId
            );

            // Extract IsStaffAssigned to determine correct redirect URL
            var isStaffAssigned = ExtractIsStaffAssigned(metadata);

            // Build appropriate redirect URL based on context
            string redirectUrl;
            if (isStaffAssigned && assignedDoctorId.HasValue)
            {
                // Option 2: Staff-assigned doctor - redirect to ConfirmNewDoctor page
                redirectUrl = PaymentFrontendHelper.BuildConfirmNewDoctorRedirectUrl(
                    FrontendOptions,
                    appointmentId,
                    rescheduleToken!,
                    assignedDoctorId.Value
                );
                Logger.LogInformation(
                    "{Gateway} Callback #{RequestId} - Supplementary payment failed for staff-assigned doctor, redirecting to ConfirmNewDoctor: {RedirectUrl}",
                    gatewayName,
                    requestId,
                    redirectUrl
                );
            }
            else if (pendingDoctorId.HasValue)
            {
                // Option 3: Patient-chosen doctor - redirect to ChooseNewDoctor page (doctor booking)
                redirectUrl = PaymentFrontendHelper.BuildChooseNewDoctorRedirectUrl(
                    FrontendOptions,
                    hospitalId,
                    specialtyId,
                    appointmentId,
                    rescheduleToken!
                );
                Logger.LogInformation(
                    "{Gateway} Callback #{RequestId} - Supplementary payment failed for patient-chosen doctor, redirecting to DoctorList: {RedirectUrl}",
                    gatewayName,
                    requestId,
                    redirectUrl
                );
            }
            else
            {
                // Fallback to appointment page
                redirectUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
                    FrontendOptions,
                    appointmentId,
                    false
                );
                Logger.LogInformation(
                    "{Gateway} Callback #{RequestId} - Supplementary payment failed, redirecting to appointment page: {AppointmentId}",
                    gatewayName,
                    requestId,
                    appointmentId
                );
            }

            return Redirect(redirectUrl);
        }

        // For regular (non-supplementary) payments, proceed with deletion
        Logger.LogInformation(
            "{Gateway} Callback #{RequestId} - Regular payment failed, proceeding with appointment deletion for AppointmentId: {AppointmentId}",
            gatewayName,
            requestId,
            appointmentId
        );

        // Publish appointment deletion event using parameter object approach
        var eventParams = new AppointmentDeleteEventParams
        {
            Payment = payment,
            ResponseCode = GetResponseCodeFromCallback(callbackResult),
            PaymentMethod = gatewayName,
            RequestId = requestId,
            ResponseMessageFunc = responseMessageFunc,
        };

        var dependencies = new PaymentEventDependencies
        {
            AppointmentClient = AppointmentClient,
            EventBus = EventBus,
            Logger = Logger,
        };

        await PaymentEventHelper.PublishAppointmentDeleteEventAsync(eventParams, dependencies);

        if (doctorId.HasValue)
        {
            var doctorBookingUrl = PaymentFrontendHelper.BuildDoctorBookingRedirectUrl(
                FrontendOptions,
                doctorId.Value
            );
            Logger.LogInformation(
                "{Gateway} Callback #{RequestId} - Payment failed, redirecting to doctor booking page: {DoctorId}, URL: {RedirectUrl}",
                gatewayName,
                requestId,
                doctorId.Value,
                doctorBookingUrl
            );

            return Redirect(doctorBookingUrl);
        }

        // Fallback to appointment error page
        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(
            FrontendOptions,
            appointmentId,
            false
        );
        Logger.LogWarning(
            "{Gateway} Callback #{RequestId} - Payment failed, could not get doctorId, redirecting to original error page for appointment: {AppointmentId}",
            gatewayName,
            requestId,
            appointmentId
        );

        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Generate request ID for tracking - common utility
    /// </summary>
    protected static string GenerateRequestId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Abstract method to extract response code from callback - must be implemented by derived classes
    /// </summary>
    protected virtual string GetResponseCodeFromCallback<TResponse>(TResponse callbackResult)
        where TResponse : class
    {
        // Use interface if available, otherwise fallback to abstract implementation
        if (callbackResult is IPaymentCallbackResponse standardCallback)
        {
            return standardCallback.ResponseCode;
        }

        // Fallback for custom implementations
        return GetCustomResponseCodeFromCallback(callbackResult);
    }

    /// <summary>
    /// Abstract method to extract transaction ID from callback - must be implemented by derived classes
    /// </summary>
    protected virtual string? GetTransactionIdFromCallback<TResponse>(TResponse callbackResult)
        where TResponse : class
    {
        // Use interface if available, otherwise fallback to abstract implementation
        if (callbackResult is IPaymentCallbackResponse standardCallback)
        {
            return standardCallback.TransactionId;
        }

        // Fallback for custom implementations
        return GetCustomTransactionIdFromCallback(callbackResult);
    }

    /// <summary>
    /// Fallback method for custom response code extraction - can be overridden by derived classes
    /// </summary>
    protected virtual string GetCustomResponseCodeFromCallback<TResponse>(TResponse callbackResult)
        where TResponse : class
    {
        return "UNKNOWN";
    }

    /// <summary>
    /// Fallback method for custom transaction ID extraction - can be overridden by derived classes
    /// </summary>
    protected virtual string? GetCustomTransactionIdFromCallback<TResponse>(
        TResponse callbackResult
    )
        where TResponse : class
    {
        return null;
    }

    /// <summary>
    /// Common error response for invalid parameters
    /// </summary>
    protected IActionResult CreateParameterValidationError(
        string parameterName,
        string requestId,
        string gatewayName
    )
    {
        Logger.LogWarning(
            "{Gateway} Callback #{RequestId} - {Parameter} parameter validation failed",
            gatewayName,
            requestId,
            parameterName
        );
        return BadRequest($"{parameterName} parameter is invalid or missing");
    }

    /// <summary>
    /// Common response for processing errors
    /// </summary>
    protected IActionResult CreateProcessingErrorResponse(
        Exception ex,
        string requestId,
        string gatewayName,
        params object[] additionalData
    )
    {
        Logger.LogError(
            ex,
            "{Gateway} Callback #{RequestId} - Processing failed - Additional data: {@Data}",
            gatewayName,
            requestId,
            additionalData
        );

        return StatusCode(
            500,
            new
            {
                Message = $"An error occurred while processing {gatewayName} callback",
                RequestId = requestId,
            }
        );
    }

    #region Supplementary Payment Helpers

    /// <summary>
    /// Extract metadata from callback response (VNPay: vnp_OrderInfo, PayOS: Description)
    /// </summary>
    protected virtual string? ExtractMetadataFromCallback<TResponse>(TResponse callbackResult)
        where TResponse : class
    {
        // Try VNPay callback
        if (callbackResult is Models.DTOs.VNPay.VNPayCallbackResponse vnPayCallback)
        {
            return vnPayCallback.vnp_OrderInfo;
        }

        // Try PayOS callback
        if (callbackResult is Models.DTOs.PayOS.PayOSCallbackResponse)
        {
            // PayOS doesn't have Description in callback, need to query webhook data
            // For now, return null and we'll handle it differently
            return null;
        }

        return null;
    }

    /// <summary>
    /// Check if metadata indicates a supplementary payment and extract appointment ID
    /// Format: SUPP_PAYMENT:{suppId}:APPT:{appointmentId}
    /// </summary>
    protected bool IsSupplementaryPayment(string? metadata, out Guid? appointmentId)
    {
        appointmentId = null;

        if (string.IsNullOrEmpty(metadata))
            return false;

        if (!metadata.StartsWith("SUPP_PAYMENT:"))
            return false;

        try
        {
            // Parse: SUPP_PAYMENT:{suppId}:APPT:{appointmentId}
            var parts = metadata.Split(':');
            if (
                parts.Length >= 4
                && parts[2] == "APPT"
                && Guid.TryParse(parts[3].Split(' ')[0], out var apptId)
            )
            {
                appointmentId = apptId;
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to parse supplementary payment metadata: {Metadata}",
                metadata
            );
        }

        return false;
    }

    /// <summary>
    /// Extract IsStaffAssigned flag from supplementary payment metadata
    /// </summary>
    protected bool ExtractIsStaffAssigned(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata) || !metadata.StartsWith("SUPP_PAYMENT:"))
            return false;

        try
        {
            // Parse: SUPP_PAYMENT:{suppId}:APPT:{appointmentId}:STAFF_ASSIGNED:{bool}
            var parts = metadata.Split(':');
            if (
                parts.Length >= 6
                && parts[4] == "STAFF_ASSIGNED"
                && bool.TryParse(parts[5].Split(' ')[0], out var isStaffAssigned)
            )
            {
                return isStaffAssigned;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to extract IsStaffAssigned from metadata: {Metadata}",
                metadata
            );
        }

        return false;
    }

    /// <summary>
    /// Handle supplementary payment success - update existing payment amount and confirm appointment
    /// </summary>
    protected async Task HandleSupplementaryPaymentSuccessAsync<TResponse>(
        Guid appointmentId,
        PaymentResponse currentPayment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        bool staffAssigned
    )
        where TResponse : class
    {
        Logger.LogInformation(
            "{Gateway} Callback #{RequestId} - Processing supplementary payment for AppointmentId: {AppointmentId}",
            gatewayName,
            requestId,
            appointmentId
        );

        try
        {
            // 1. Get existing payment for this appointment
            var existingPayment = await PaymentService.GetByAppointmentIdAsync(appointmentId);
            if (existingPayment == null)
            {
                Logger.LogError(
                    "{Gateway} Callback #{RequestId} - No existing payment found for AppointmentId: {AppointmentId}",
                    gatewayName,
                    requestId,
                    appointmentId
                );
                return;
            }

            // 2. Calculate callback payment amount
            var callbackAmount = GetAmountFromCallback(callbackResult);

            // 3. Update existing payment amount (add supplementary amount)
            var newTotalAmount = existingPayment.Amount + callbackAmount;
            await PaymentService.UpdateStatusAsync(
                new Models.DTOs.Requests.UpdatePaymentStatusRequest
                {
                    Id = existingPayment.Id,
                    Status = Enums.PaymentStatus.COMPLETED,
                    Amount = newTotalAmount,
                }
            );

            Logger.LogInformation(
                "{Gateway} Callback #{RequestId} - Updated payment amount from {OldAmount} to {NewAmount} for PaymentId: {PaymentId}",
                gatewayName,
                requestId,
                existingPayment.Amount,
                newTotalAmount,
                existingPayment.Id
            );

            // 4. Confirm appointment via gRPC
            var confirmRequest = new ConfirmAppointmentRequest
            {
                AppointmentId = appointmentId.ToString(),
                StaffAssigned = staffAssigned,
            };

            var confirmResponse = await AppointmentClient.ConfirmAppointmentAsync(confirmRequest);
            if (confirmResponse.Success)
            {
                Logger.LogInformation(
                    "{Gateway} Callback #{RequestId} - Appointment {AppointmentId} confirmed successfully after supplementary payment",
                    gatewayName,
                    requestId,
                    appointmentId
                );
            }
            else
            {
                Logger.LogWarning(
                    "{Gateway} Callback #{RequestId} - Failed to confirm appointment {AppointmentId}: {Message}",
                    gatewayName,
                    requestId,
                    appointmentId,
                    confirmResponse.Message
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "{Gateway} Callback #{RequestId} - Error processing supplementary payment for AppointmentId: {AppointmentId}",
                gatewayName,
                requestId,
                appointmentId
            );
            throw new InvalidOperationException(
                $"Failed to process supplementary payment for appointment {appointmentId} in {gatewayName} callback #{requestId}. "
                    + $"Payment may be in inconsistent state. Manual intervention may be required.",
                ex
            );
        }
    }

    /// <summary>
    /// Extract payment amount from callback response
    /// </summary>
    protected virtual decimal GetAmountFromCallback<TResponse>(TResponse callbackResult)
        where TResponse : class
    {
        // Try VNPay callback
        if (callbackResult is Models.DTOs.VNPay.VNPayCallbackResponse vnPayCallback)
        {
            return vnPayCallback.GetActualAmount;
        }

        // Try PayOS callback
        if (callbackResult is Models.DTOs.PayOS.PayOSCallbackResponse payOSCallback)
        {
            return payOSCallback.Amount;
        }

        return 0;
    }

    /// <summary>
    /// Handle subscription payment failed/cancelled - redirect to subscription plan page
    /// </summary>
    protected IActionResult HandleSubscriptionPaymentFailed(
        string? planType,
        Guid? hospitalId,
        string requestId,
        string gatewayName,
        string status,
        string? responseCode = null
    )
    {
        var finalPlanType = planType?.ToLowerInvariant() ?? "monthly";
        var frontendUrl =
            $"{FrontendOptions.Admin.BaseUrl}hospitals/subscription-plan?plan-type={finalPlanType}&is_cancelled=true";

        Logger.LogWarning(
            "{Gateway} Callback #{RequestId} - Subscription payment {Status} for HospitalId: {HospitalId}, PlanType: {PlanType}{ResponseCode}",
            gatewayName,
            requestId,
            status,
            hospitalId,
            finalPlanType,
            responseCode != null ? $", ResponseCode: {responseCode}" : string.Empty
        );

        return Redirect(frontendUrl);
    }

    /// <summary>
    /// Handle successful subscription payment - create or upgrade subscription via gRPC
    /// </summary>
    protected async Task HandleSubscriptionPaymentSuccessAsync(
        BookingCare.Services.Hospital.HospitalSubscriptionGrpc.HospitalSubscriptionGrpcClient hospitalSubscriptionClient,
        Guid subscriptionPlanId,
        Guid hospitalId,
        bool isUpgrade,
        Guid? currentHospitalSubscriptionId,
        string requestId,
        string gatewayName
    )
    {
        Logger.LogInformation(
            "{Gateway} Callback #{RequestId} - Handling subscription payment - HospitalId: {HospitalId}, PlanId: {PlanId}, IsUpgrade: {IsUpgrade}",
            gatewayName,
            requestId,
            hospitalId,
            subscriptionPlanId,
            isUpgrade
        );

        try
        {
            if (isUpgrade && currentHospitalSubscriptionId.HasValue)
            {
                // Call gRPC to upgrade subscription
                var upgradeRequest =
                    new BookingCare.Services.Hospital.UpgradeHospitalSubscriptionGrpcRequest
                    {
                        CurrentSubscriptionId = currentHospitalSubscriptionId.Value.ToString(),
                        NewSubscriptionPlanId = subscriptionPlanId.ToString(),
                    };

                var upgradeResponse =
                    await hospitalSubscriptionClient.UpgradeHospitalSubscriptionAsync(
                        upgradeRequest
                    );

                if (!string.IsNullOrEmpty(upgradeResponse?.HospitalSubscriptionId))
                {
                    Logger.LogInformation(
                        "{Gateway} Callback #{RequestId} - Successfully upgraded subscription via gRPC - NewSubscriptionId: {NewSubscriptionId}, Status: {Status}",
                        gatewayName,
                        requestId,
                        upgradeResponse.HospitalSubscriptionId,
                        upgradeResponse.Status
                    );
                }
                else
                {
                    Logger.LogError(
                        "{Gateway} Callback #{RequestId} - Failed to upgrade subscription via gRPC - Empty response",
                        gatewayName,
                        requestId
                    );
                }
            }
            else
            {
                // Call gRPC to create new subscription
                var createRequest =
                    new BookingCare.Services.Hospital.CreateHospitalSubscriptionGrpcRequest
                    {
                        HospitalId = hospitalId.ToString(),
                        SubscriptionId = subscriptionPlanId.ToString(),
                        // StartDate and EndDate are auto-calculated by Hospital Service based on billing cycle
                    };

                var createResponse =
                    await hospitalSubscriptionClient.CreateHospitalSubscriptionAsync(createRequest);

                if (!string.IsNullOrEmpty(createResponse?.HospitalSubscriptionId))
                {
                    Logger.LogInformation(
                        "{Gateway} Callback #{RequestId} - Successfully created subscription via gRPC - SubscriptionId: {SubscriptionId}, Status: {Status}",
                        gatewayName,
                        requestId,
                        createResponse.HospitalSubscriptionId,
                        createResponse.Status
                    );
                }
                else
                {
                    Logger.LogError(
                        "{Gateway} Callback #{RequestId} - Failed to create subscription via gRPC - Empty response",
                        gatewayName,
                        requestId
                    );
                }
            }
        }
        catch (Grpc.Core.RpcException ex)
        {
            Logger.LogError(
                ex,
                "{Gateway} Callback #{RequestId} - gRPC error handling subscription payment - Status: {Status}, Detail: {Detail}",
                gatewayName,
                requestId,
                ex.Status.StatusCode,
                ex.Status.Detail
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "{Gateway} Callback #{RequestId} - Unexpected error handling subscription payment",
                gatewayName,
                requestId
            );
        }
    }

    #endregion
}
