using BookingCare.Services.Payment.Models.Configurations;
using BookingCare.Services.Payment.Models.DTOs.Stripe;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripeConfig = BookingCare.Services.Payment.Models.Configurations.StripeConfiguration;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation of Stripe Service
/// </summary>
public class StripeService : BaseService, IStripeService
{
    private readonly StripeConfig _stripeConfig;

    public StripeService(IOptions<StripeConfig> stripeConfig, ILogger<StripeService> logger)
        : base(logger)
    {
        _stripeConfig = stripeConfig.Value;
        ValidateConfiguration();

        // Set Stripe API key
        Stripe.StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
    }

    /// <summary>
    /// Validate Stripe configuration
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_stripeConfig.SecretKey))
            throw new InvalidOperationException("Stripe SecretKey is not configured");

        if (string.IsNullOrEmpty(_stripeConfig.PublishableKey))
            throw new InvalidOperationException("Stripe PublishableKey is not configured");

        if (string.IsNullOrEmpty(_stripeConfig.SuccessUrl))
            throw new InvalidOperationException("Stripe SuccessUrl is not configured");

        if (string.IsNullOrEmpty(_stripeConfig.CancelUrl))
            throw new InvalidOperationException("Stripe CancelUrl is not configured");

        if (_stripeConfig.TimeoutInMinutes < 30)
            throw new InvalidOperationException(
                "Stripe TimeoutInMinutes must be at least 30 minutes"
            );

        var keyPrefix = _stripeConfig.PublishableKey.Substring(
            0,
            Math.Min(10, _stripeConfig.PublishableKey.Length)
        );
        LogInfo(
            "Stripe Configuration validated successfully - PublishableKey starts with: {KeyPrefix}",
            null,
            keyPrefix
        );
    }

    /// <summary>
    /// Create Stripe checkout session for payment
    /// </summary>
    public async Task<StripePaymentResponse> CreateCheckoutSessionAsync(
        StripePaymentRequest request
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Creating Stripe checkout session for PaymentId: {PaymentId}",
                    null,
                    request.PaymentId
                );

                // Validation
                ValidateRequired(request, nameof(request));
                ValidateGuid(request.PaymentId, nameof(request.PaymentId));

                if (request.Amount <= 0)
                    throw new ArgumentException("Amount must be greater than 0");

                // Prepare line items
                var lineItems = new List<SessionLineItemOptions>();

                if (request.LineItems?.Any() == true)
                {
                    foreach (var item in request.LineItems)
                    {
                        lineItems.Add(
                            new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    Currency = _stripeConfig.Currency,
                                    UnitAmount = (long)item.Price, // Amount in smallest currency unit (VND doesn't have decimals)
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = item.Name,
                                        Description = item.Description,
                                    },
                                },
                                Quantity = item.Quantity,
                            }
                        );
                    }
                }
                else
                {
                    // Default line item if none provided
                    lineItems.Add(
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = _stripeConfig.Currency,
                                UnitAmount = (long)request.Amount,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.Description,
                                },
                            },
                            Quantity = 1,
                        }
                    );
                }

                // Prepare metadata
                var metadata = new Dictionary<string, string>
                {
                    { "PaymentId", request.PaymentId.ToString() },
                };

                // Add appointment-related metadata
                if (request.AppointmentId.HasValue)
                {
                    metadata["AppointmentId"] = request.AppointmentId.Value.ToString();
                }

                if (request.PatientId.HasValue)
                {
                    metadata["PatientId"] = request.PatientId.Value.ToString();
                }

                // Add subscription-related metadata
                if (request.SubscriptionPlanId.HasValue)
                {
                    metadata["SubscriptionPlanId"] = request.SubscriptionPlanId.Value.ToString();
                }

                if (request.HospitalId.HasValue)
                {
                    metadata["HospitalId"] = request.HospitalId.Value.ToString();
                }

                if (request.IsSubscriptionUpgrade)
                {
                    metadata["IsUpgrade"] = "true";
                }

                if (request.CurrentHospitalSubscriptionId.HasValue)
                {
                    metadata["CurrentSubscriptionId"] =
                        request.CurrentHospitalSubscriptionId.Value.ToString();
                }

                if (!string.IsNullOrEmpty(request.PlanType))
                {
                    metadata["PlanType"] = request.PlanType;
                }

                // Add custom metadata if provided
                if (request.Metadata?.Any() == true)
                {
                    foreach (var kvp in request.Metadata)
                    {
                        if (!metadata.ContainsKey(kvp.Key))
                        {
                            metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Calculate expiration time
                var expiresAt = DateTime.UtcNow.AddMinutes(_stripeConfig.TimeoutInMinutes);

                // Create session options
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = _stripeConfig.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = _stripeConfig.CancelUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    Metadata = metadata,
                    ExpiresAt = expiresAt,
                    PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = metadata },
                };

                // Add customer info if provided
                if (request.CustomerInfo != null)
                {
                    if (!string.IsNullOrEmpty(request.CustomerInfo.Email))
                    {
                        options.CustomerEmail = request.CustomerInfo.Email;
                    }
                }

                // Create the session
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                var response = new StripePaymentResponse
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url,
                    ExpireAt = expiresAt,
                    PaymentIntentId = session.PaymentIntentId ?? string.Empty,
                };

                LogInfo(
                    "Stripe checkout session created successfully - SessionId: {SessionId}, PaymentId: {PaymentId}",
                    null,
                    session.Id,
                    request.PaymentId
                );

                return response;
            },
            "CreateStripeCheckoutSession"
        );
    }

    /// <summary>
    /// Handle webhook from Stripe
    /// </summary>
    public async Task<StripeCallbackResponse> ProcessWebhookAsync(
        string json,
        string stripeSignature
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Processing Stripe webhook event", null);

                // Validation
                ValidateRequired(json, nameof(json));
                ValidateRequired(stripeSignature, nameof(stripeSignature));

                // Verify webhook signature
                if (!VerifyWebhookSignature(json, stripeSignature))
                {
                    throw new UnauthorizedAccessException("Invalid Stripe webhook signature");
                }

                // Parse event
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _stripeConfig.WebhookSecret
                );

                LogInfo("Stripe webhook event type: {EventType}", null, stripeEvent.Type);

                // Handle different event types
                StripeCallbackResponse response;

                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        var session = stripeEvent.Data.Object as Session;
                        response = MapSessionToCallbackResponse(session!);
                        break;

                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        response = MapPaymentIntentToCallbackResponse(paymentIntent!);
                        break;

                    case "payment_intent.payment_failed":
                        var failedPaymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        response = MapPaymentIntentToCallbackResponse(failedPaymentIntent!);
                        break;

                    default:
                        LogInfo("Unhandled Stripe event type: {EventType}", null, stripeEvent.Type);
                        response = new StripeCallbackResponse
                        {
                            Status = "unhandled",
                            Metadata = new Dictionary<string, string>
                            {
                                { "EventType", stripeEvent.Type },
                            },
                        };
                        break;
                }

                LogInfo(
                    "Stripe webhook processed successfully - Status: {Status}",
                    null,
                    response.Status
                );

                await Task.CompletedTask;
                return response;
            },
            "ProcessStripeWebhook"
        );
    }

    /// <summary>
    /// Verify webhook signature from Stripe
    /// </summary>
    public bool VerifyWebhookSignature(string json, string stripeSignature)
    {
        try
        {
            ValidateRequired(json, nameof(json));
            ValidateRequired(stripeSignature, nameof(stripeSignature));

            if (string.IsNullOrEmpty(_stripeConfig.WebhookSecret))
            {
                LogWarning("Stripe webhook secret is not configured, skipping verification", null);
                return false;
            }

            // Stripe SDK will throw exception if signature is invalid
            EventUtility.ConstructEvent(json, stripeSignature, _stripeConfig.WebhookSecret);

            return true;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to verify Stripe webhook signature", null);
            return false;
        }
    }

    /// <summary>
    /// Get session information from Stripe
    /// </summary>
    public async Task<object> GetSessionAsync(string sessionId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Getting Stripe session: {SessionId}", null, sessionId);

                ValidateRequired(sessionId, nameof(sessionId));

                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                LogInfo(
                    "Retrieved Stripe session successfully - SessionId: {SessionId}, Status: {Status}",
                    null,
                    sessionId,
                    session.Status
                );

                return session;
            },
            "GetStripeSession"
        );
    }

    /// <summary>
    /// Cancel/expire a checkout session
    /// </summary>
    public async Task<bool> CancelSessionAsync(string sessionId)
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo("Cancelling Stripe session: {SessionId}", null, sessionId);

                ValidateRequired(sessionId, nameof(sessionId));

                var service = new SessionService();
                var session = await service.ExpireAsync(sessionId);

                var cancelled = session.Status == "expired";

                LogInfo(
                    "Stripe session cancellation result - SessionId: {SessionId}, Cancelled: {Cancelled}",
                    null,
                    sessionId,
                    cancelled
                );

                return cancelled;
            },
            "CancelStripeSession"
        );
    }

    /// <summary>
    /// Map Stripe Session to CallbackResponse
    /// </summary>
    private StripeCallbackResponse MapSessionToCallbackResponse(Session session)
    {
        return new StripeCallbackResponse
        {
            SessionId = session.Id,
            PaymentIntentId = session.PaymentIntentId ?? string.Empty,
            Amount = session.AmountTotal ?? 0,
            Currency = session.Currency ?? _stripeConfig.Currency,
            Status = session.Status ?? "unknown",
            CustomerEmail = session.CustomerEmail,
            PaymentMethodType = session.PaymentMethodTypes?.FirstOrDefault(),
            Metadata =
                session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?? new Dictionary<string, string>(),
        };
    }

    /// <summary>
    /// Map Stripe PaymentIntent to CallbackResponse
    /// </summary>
    private StripeCallbackResponse MapPaymentIntentToCallbackResponse(PaymentIntent paymentIntent)
    {
        return new StripeCallbackResponse
        {
            SessionId = string.Empty,
            PaymentIntentId = paymentIntent.Id,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency ?? _stripeConfig.Currency,
            Status = paymentIntent.Status,
            CustomerEmail = string.Empty, // PaymentIntent doesn't directly have customer email
            PaymentMethodType = string.Empty, // Would need to expand PaymentMethod to get type
            Metadata =
                paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?? new Dictionary<string, string>(),
        };
    }
}
