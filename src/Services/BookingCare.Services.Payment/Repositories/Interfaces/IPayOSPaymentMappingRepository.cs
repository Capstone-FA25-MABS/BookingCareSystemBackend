using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface cho PayOS Payment Mapping Repository
/// </summary>
public interface IPayOSPaymentMappingRepository
{
    /// <summary>
    /// T?o mapping m?i gi?a PaymentId và OrderCode
    /// </summary>
    /// <param name="paymentId">ID c?a payment</param>
    /// <param name="orderCode">Order code t? PayOS</param>
    /// <param name="expiresAt">Th?i gian h?t h?n (tùy ch?n)</param>
    /// <returns>Mapping entity ?ã t?o</returns>
    Task<PayOSPaymentMappingEntity> CreateMappingAsync(Guid paymentId, long orderCode, DateTime? expiresAt = null);

    /// <summary>
    /// L?y PaymentId theo OrderCode
    /// </summary>
    /// <param name="orderCode">Order code t? PayOS</param>
    /// <returns>PaymentId n?u tìm th?y, null n?u không</returns>
    Task<Guid?> GetPaymentIdByOrderCodeAsync(long orderCode);

    /// <summary>
    /// L?y OrderCode theo PaymentId
    /// </summary>
    /// <param name="paymentId">ID c?a payment</param>
    /// <returns>OrderCode n?u tìm th?y, null n?u không</returns>
    Task<long?> GetOrderCodeByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// L?y mapping theo OrderCode
    /// </summary>
    /// <param name="orderCode">Order code t? PayOS</param>
    /// <returns>Mapping entity n?u tìm th?y, null n?u không</returns>
    Task<PayOSPaymentMappingEntity?> GetMappingByOrderCodeAsync(long orderCode);

    /// <summary>
    /// Xóa mapping theo OrderCode (sau khi thanh toán hoàn t?t)
    /// </summary>
    /// <param name="orderCode">Order code t? PayOS</param>
    /// <returns>True n?u xóa thành công</returns>
    Task<bool> DeleteMappingAsync(long orderCode);

    /// <summary>
    /// Xóa các mapping ?ã h?t h?n (cleanup job)
    /// </summary>
    /// <returns>S? l??ng mapping ?ã xóa</returns>
    Task<int> CleanupExpiredMappingsAsync();

    /// <summary>
    /// Ki?m tra mapping có t?n t?i không
    /// </summary>
    /// <param name="orderCode">Order code t? PayOS</param>
    /// <returns>True n?u t?n t?i</returns>
    Task<bool> MappingExistsAsync(long orderCode);
}