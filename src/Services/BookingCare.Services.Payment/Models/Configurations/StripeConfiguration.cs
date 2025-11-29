namespace BookingCare.Services.Payment.Models.Configurations;

/// <summary>
/// Stripe configuration
/// </summary>
public class StripeConfiguration
{
    /// <summary>
    /// Stripe Secret Key (API Key)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Publishable Key (for client-side integration)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook Secret for signature verification
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Success URL to redirect after successful payment
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel URL to redirect when user cancels payment
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (e.g. USD, VND)
    /// Note: Stripe supports VND as a presentment currency
    /// </summary>
    public string Currency { get; set; } = "vnd";

    /// <summary>
    /// Transaction timeout in minutes
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to use webhook
    /// </summary>
    public bool UseWebhook { get; set; } = true;
}
