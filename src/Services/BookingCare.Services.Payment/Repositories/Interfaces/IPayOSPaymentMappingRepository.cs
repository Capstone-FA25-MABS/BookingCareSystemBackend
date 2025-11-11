using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface for PayOS Payment Mapping Repository
/// </summary>
public interface IPayOSPaymentMappingRepository
{
    /// <summary>
    /// Create a new mapping between PaymentId and OrderCode
    /// </summary>
    /// <param name="paymentId">ID of the payment</param>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <param name="expiresAt">Expiration time (optional)</param>
    /// <returns>The created mapping entity</returns>
    Task<PayOSPaymentMappingEntity> CreateMappingAsync(
        Guid paymentId,
        long orderCode,
        DateTime? expiresAt = null
    );

    /// <summary>
    /// Create a new mapping for subscription payment
    /// </summary>
    /// <param name="paymentId">ID of the payment</param>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <param name="subscriptionPlanId">Subscription Plan ID</param>
    /// <param name="hospitalId">Hospital ID</param>
    /// <param name="isUpgrade">Is this an upgrade (nullable)</param>
    /// <param name="currentHospitalSubscriptionId">Current subscription ID (for upgrade)</param>
    /// <param name="planType">Plan type (billing cycle): MONTHLY, QUARTERLY, YEARLY</param>
    /// <param name="expiresAt">Expiration time (optional)</param>
    /// <returns>The created mapping entity</returns>
    Task<PayOSPaymentMappingEntity> CreateSubscriptionMappingAsync(
        Guid paymentId,
        long orderCode,
        Guid subscriptionPlanId,
        Guid hospitalId,
        bool? isUpgrade,
        Guid? currentHospitalSubscriptionId,
        string? planType,
        DateTime? expiresAt
    );

    /// <summary>
    /// Get PaymentId by OrderCode
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <returns>PaymentId if found, null otherwise</returns>
    Task<Guid?> GetPaymentIdByOrderCodeAsync(long orderCode);

    /// <summary>
    /// Get OrderCode by PaymentId
    /// </summary>
    /// <param name="paymentId">ID of the payment</param>
    /// <returns>OrderCode if found, null otherwise</returns>
    Task<long?> GetOrderCodeByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// Get mapping by OrderCode
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <returns>Mapping entity if found, null otherwise</returns>
    Task<PayOSPaymentMappingEntity?> GetMappingByOrderCodeAsync(long orderCode);

    /// <summary>
    /// Delete mapping by OrderCode (after payment completed)
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <returns>True if deletion succeeded</returns>
    Task<bool> DeleteMappingAsync(long orderCode);

    /// <summary>
    /// Delete expired mappings (cleanup job)
    /// </summary>
    /// <returns>Number of mappings deleted</returns>
    Task<int> CleanupExpiredMappingsAsync();

    /// <summary>
    /// Check if a mapping exists
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <returns>True if exists</returns>
    Task<bool> MappingExistsAsync(long orderCode);
}
