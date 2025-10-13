using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface for PaymentMethod Repository
/// </summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// Get payment method by ID
    /// </summary>
    Task<PaymentMethodEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetAllAsync();

    /// <summary>
    /// Get only active payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetActiveAsync();

    /// <summary>
    /// Get payment method by name
    /// </summary>
    Task<PaymentMethodEntity?> GetByNameAsync(string name);

    /// <summary>
    /// Update payment method
    /// </summary>
    Task<PaymentMethodEntity> UpdateAsync(PaymentMethodEntity paymentMethod);

    /// <summary>
    /// Check if payment method exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}