using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface cho PaymentMethod Repository
/// </summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// Lấy payment method theo ID
    /// </summary>
    Task<PaymentMethodEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lấy tất cả payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetAllAsync();

    /// <summary>
    /// Lấy chỉ payment methods đang active
    /// </summary>
    Task<IEnumerable<PaymentMethodEntity>> GetActiveAsync();

    /// <summary>
    /// Lấy payment method theo tên
    /// </summary>
    Task<PaymentMethodEntity?> GetByNameAsync(string name);

    /// <summary>
    /// Cập nhật payment method
    /// </summary>
    Task<PaymentMethodEntity> UpdateAsync(PaymentMethodEntity paymentMethod);

    /// <summary>
    /// Kiểm tra payment method có tồn tại không
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}