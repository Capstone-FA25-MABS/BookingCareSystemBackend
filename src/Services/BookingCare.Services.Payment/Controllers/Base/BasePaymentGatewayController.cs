using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Helpers;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Shared.EventBus.Abstractions;

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

    protected BasePaymentGatewayController(
        IPaymentService paymentService,
        IEventBus eventBus,
        IOptions<FrontendOptions> frontendOptions,
        ILogger logger,
        AppointmentService.AppointmentServiceClient appointmentClient)
    {
        PaymentService = paymentService;
        EventBus = eventBus;
        FrontendOptions = frontendOptions.Value;
        Logger = logger;
        AppointmentClient = appointmentClient;
    }

    /// <summary>
    /// Common health check implementation
    /// </summary>
    protected IActionResult CreateHealthCheckResponse(string serviceName)
    {
        return Success(new
        {
            Service = $"{serviceName} Integration",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = ApiVersions.V1_0
        }, $"{serviceName} service is healthy");
    }

    /// <summary>
    /// Common payment retrieval with validation
    /// </summary>
    protected async Task<PaymentResponse?> GetPaymentWithValidation(Guid paymentId, string requestId, string gatewayName)
    {
        var payment = await PaymentService.GetByIdAsync(paymentId);
        if (payment == null)
        {
            Logger.LogWarning("{Gateway} Callback #{RequestId} - Payment not found for PaymentId: {PaymentId}",
                gatewayName, requestId, paymentId);
        }
        return payment;
    }

    /// <summary>
    /// Handle successful payment scenario - common logic
    /// </summary>
    protected IActionResult HandleSuccessfulPayment<TResponse>(
        PaymentResponse payment,
        TResponse callbackResult,
        string requestId,
        string gatewayName,
        Func<PaymentResponse, TResponse, string, IActionResult> createResponseFunc)
        where TResponse : class
    {
        var appointmentId = payment.AppointmentId;

        if (PaymentFrontendHelper.ShouldRedirectToFrontend(appointmentId))
        {
            var apptId = appointmentId!.Value;
            var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(FrontendOptions, apptId, true);
            Logger.LogInformation("{Gateway} Callback #{RequestId} - Redirecting to frontend for appointment: {AppointmentId}, URL: {RedirectUrl}",
                gatewayName, requestId, apptId, frontendUrl);

            return Redirect(frontendUrl);
        }

        return createResponseFunc(payment, callbackResult, requestId);
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
        Func<PaymentResponse, TResponse, string, IActionResult> createResponseFunc)
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
                responseMessageFunc);
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
        Func<string, string> responseMessageFunc)
        where TResponse : class
    {
        // Try to get doctorId using gRPC
        var doctorId = await PaymentFrontendHelper.GetDoctorIdFromAppointmentAsync(AppointmentClient, appointmentId);

        // Publish appointment deletion event using parameter object approach
        var eventParams = new AppointmentDeleteEventParams
        {
            Payment = payment,
            ResponseCode = GetResponseCodeFromCallback(callbackResult),
            PaymentMethod = gatewayName,
            RequestId = requestId,
            ResponseMessageFunc = responseMessageFunc
        };

        var dependencies = new PaymentEventDependencies
        {
            AppointmentClient = AppointmentClient,
            EventBus = EventBus,
            Logger = Logger
        };

        await PaymentEventHelper.PublishAppointmentDeleteEventAsync(eventParams, dependencies);

        if (doctorId.HasValue)
        {
            var doctorBookingUrl = PaymentFrontendHelper.BuildDoctorBookingRedirectUrl(FrontendOptions, doctorId.Value);
            Logger.LogInformation("{Gateway} Callback #{RequestId} - Payment failed, redirecting to doctor booking page: {DoctorId}, URL: {RedirectUrl}",
                gatewayName, requestId, doctorId.Value, doctorBookingUrl);

            return Redirect(doctorBookingUrl);
        }

        // Fallback to appointment error page
        var frontendUrl = PaymentFrontendHelper.BuildAppointmentRedirectUrl(FrontendOptions, appointmentId, false);
        Logger.LogWarning("{Gateway} Callback #{RequestId} - Payment failed, could not get doctorId, redirecting to original error page for appointment: {AppointmentId}",
            gatewayName, requestId, appointmentId);

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
    protected abstract string GetResponseCodeFromCallback<TResponse>(TResponse callbackResult) where TResponse : class;

    /// <summary>
    /// Common error response for invalid parameters
    /// </summary>
    protected IActionResult CreateParameterValidationError(string parameterName, string requestId, string gatewayName)
    {
        Logger.LogWarning("{Gateway} Callback #{RequestId} - {Parameter} parameter validation failed",
            gatewayName, requestId, parameterName);
        return BadRequest($"{parameterName} parameter is invalid or missing");
    }

    /// <summary>
    /// Common response for processing errors
    /// </summary>
    protected IActionResult CreateProcessingErrorResponse(Exception ex, string requestId, string gatewayName, params object[] additionalData)
    {
        Logger.LogError(ex, "{Gateway} Callback #{RequestId} - Processing failed - Additional data: {@Data}",
            gatewayName, requestId, additionalData);

        return StatusCode(500, new
        {
            Message = $"An error occurred while processing {gatewayName} callback",
            RequestId = requestId
        });
    }
}