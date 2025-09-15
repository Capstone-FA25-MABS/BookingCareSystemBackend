using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface cho PaymentMethod Repository
/// </summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// L?y payment method theo ID
    /// </summary>
    Task<PaymentMethodEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y t?t c? payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetAllAsync();

    /// <summary>
    /// L?y ch? payment methods ?ang active
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetActiveAsync();

    /// <summary>
    /// L?y payment method theo t?n
    /// </summary>
    Task<PaymentMethodEntity?> GetByNameAsync(string name);

    /// <summary>
    /// C?p nh?t payment method
    /// </summary>
    Task<PaymentMethodEntity> UpdateAsync(PaymentMethodEntity paymentMethod);

    /// <summary>
    /// Ki?m tra payment method c? t?n t?i kh?ng
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}