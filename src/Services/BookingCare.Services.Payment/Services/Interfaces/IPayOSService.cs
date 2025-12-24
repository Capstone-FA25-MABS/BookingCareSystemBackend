using BookingCare.Services.Payment.Models.DTOs.PayOS;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for PayOS Service
/// </summary>
public interface IPayOSService
{
    /// <summary>
    /// Create PayOS payment link
    /// </summary>
    /// <param name="request">Payment information</param>
    /// <returns>Payment link and payment information</returns>
    Task<PayOSPaymentResponse> CreatePaymentLinkAsync(PayOSPaymentRequest request);

    /// <summary>
    /// Handle callback from PayOS (when user returns from PayOS)
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <param name="code">Response code from PayOS</param>
    /// <param name="cancel">Was it cancelled</param>
    /// <returns>Callback handling result</returns>
    Task<PayOSCallbackResponse> ProcessCallbackAsync(long orderCode, string code, bool cancel);

    /// <summary>
    /// Get payment info from PayOS
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Detailed payment information</returns>
    Task<object> GetPaymentInfoAsync(long orderCode);

    /// <summary>
    /// Cancel PayOS payment link
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <param name="cancellationReason">Reason for cancellation</param>
    /// <returns>Result of payment cancellation</returns>
    Task<bool> CancelPaymentLinkAsync(long orderCode, string cancellationReason = "");

    /// <summary>
    /// Cleanup expired mappings
    /// </summary>
    /// <returns>Number of mappings deleted</returns>
    Task<int> CleanupExpiredMappingsAsync();

    /// <summary>
    /// Get subscription metadata from PayOS payment mapping
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Subscription metadata or null if not a subscription payment</returns>
    Task<(
        Guid? SubscriptionPlanId,
        Guid? HospitalId,
        bool IsSubscriptionUpgrade,
        Guid? CurrentHospitalSubscriptionId,
        string? PlanType
    )?> GetSubscriptionMetadataAsync(long orderCode);
}
