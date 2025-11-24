using BookingCare.Services.Payment.Models.DTOs.Stripe;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for Stripe Service
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create Stripe checkout session for payment
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <returns>Checkout session information with URL</returns>
    Task<StripePaymentResponse> CreateCheckoutSessionAsync(StripePaymentRequest request);

    /// <summary>
    /// Handle webhook from Stripe
    /// </summary>
    /// <param name="json">Raw JSON payload from Stripe</param>
    /// <param name="stripeSignature">Stripe signature header</param>
    /// <returns>Callback handling result</returns>
    Task<StripeCallbackResponse> ProcessWebhookAsync(string json, string stripeSignature);

    /// <summary>
    /// Verify webhook signature from Stripe
    /// </summary>
    /// <param name="json">Raw JSON payload</param>
    /// <param name="stripeSignature">Stripe signature header</param>
    /// <returns>True if signature is valid</returns>
    bool VerifyWebhookSignature(string json, string stripeSignature);

    /// <summary>
    /// Get session information from Stripe
    /// </summary>
    /// <param name="sessionId">Stripe session ID</param>
    /// <returns>Session information</returns>
    Task<object> GetSessionAsync(string sessionId);

    /// <summary>
    /// Cancel/expire a checkout session
    /// </summary>
    /// <param name="sessionId">Stripe session ID</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelSessionAsync(string sessionId);
}
