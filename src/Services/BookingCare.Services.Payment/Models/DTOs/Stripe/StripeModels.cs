using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Payment.Models.Interfaces;

namespace BookingCare.Services.Payment.Models.DTOs.Stripe;

/// <summary>
/// Request to create a Stripe payment session
/// </summary>
public class StripePaymentRequest
{
    /// <summary>
    /// Payment ID in the system
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Payment amount (VND)
    /// </summary>
    [Required]
    [JsonRequired]
    public decimal Amount { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Customer information (optional)
    /// </summary>
    public StripeCustomerInfo? CustomerInfo { get; set; }

    /// <summary>
    /// Line items information (optional)
    /// </summary>
    public List<StripeLineItem>? LineItems { get; set; }

    /// <summary>
    /// Subscription Plan ID (for subscription payments)
    /// </summary>
    public Guid? SubscriptionPlanId { get; set; }

    /// <summary>
    /// Hospital ID (for subscription payments)
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Is subscription upgrade
    /// </summary>
    [JsonRequired]
    public bool IsSubscriptionUpgrade { get; set; }

    /// <summary>
    /// Current Hospital Subscription ID (for upgrade)
    /// </summary>
    public Guid? CurrentHospitalSubscriptionId { get; set; }

    /// <summary>
    /// Plan type (billing cycle): MONTHLY, QUARTERLY, YEARLY
    /// </summary>
    public string? PlanType { get; set; }

    /// <summary>
    /// Appointment ID (for appointment payments)
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// Patient ID (for appointment payments)
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Custom metadata to pass through checkout
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response containing Stripe payment session information
/// </summary>
public class StripePaymentResponse
{
    /// <summary>
    /// Checkout Session ID from Stripe
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Checkout URL to redirect the user to Stripe
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// Transaction expiration time
    /// </summary>
    public DateTime ExpireAt { get; set; }

    /// <summary>
    /// Payment Intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;
}

/// <summary>
/// Customer information for Stripe
/// </summary>
public class StripeCustomerInfo
{
    /// <summary>
    /// Customer name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Customer phone
    /// </summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Line item information for Stripe
/// </summary>
public class StripeLineItem
{
    /// <summary>
    /// Item name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Price per unit (in VND)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Item description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Callback response from Stripe webhook
/// </summary>
public class StripeCallbackResponse : IPaymentCallbackResponse
{
    /// <summary>
    /// Stripe Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Payment Intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid (in cents/smallest currency unit)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment status (succeeded, pending, failed)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Customer email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Payment method type (card, etc.)
    /// </summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>
    /// Metadata passed through from payment creation
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Payment ID from our system
    /// </summary>
    [JsonIgnore]
    public Guid PaymentId
    {
        get
        {
            if (Metadata != null && Metadata.TryGetValue("PaymentId", out var paymentIdStr))
            {
                if (Guid.TryParse(paymentIdStr, out var paymentId))
                {
                    return paymentId;
                }
            }
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Whether the payment was successful
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Status == "succeeded" || Status == "complete";

    /// <summary>
    /// Response code for compatibility with IPaymentCallbackResponse
    /// </summary>
    [JsonIgnore]
    public string ResponseCode => IsSuccess ? "00" : "99";

    /// <summary>
    /// Transaction ID for compatibility with IPaymentCallbackResponse
    /// </summary>
    [JsonIgnore]
    public string TransactionId => PaymentIntentId ?? SessionId;
}

/// <summary>
/// Stripe webhook event data
/// </summary>
public class StripeWebhookEvent
{
    /// <summary>
    /// Event type (checkout.session.completed, payment_intent.succeeded, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Event data
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Event ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public long Created { get; set; }
}

/// <summary>
/// Request to create a Stripe refund
/// </summary>
public class StripeRefundRequest
{
    /// <summary>
    /// Payment ID to refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Refund amount (optional - full refund if not specified)
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Payment Intent ID from Stripe (auto-retrieved from payment record if not provided)
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;
}

/// <summary>
/// Response from Stripe refund operation
/// </summary>
public class StripeRefundResponse
{
    /// <summary>
    /// Refund ID from Stripe
    /// </summary>
    public string RefundId { get; set; } = string.Empty;

    /// <summary>
    /// Status of the refund
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Refunded amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Payment Intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether refund is successful
    /// </summary>
    public bool IsSuccess => Status == "succeeded" || Status == "pending";

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
