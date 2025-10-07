using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Repository interface cho RefundHistory
/// </summary>
public interface IRefundHistoryRepository
{
    /// <summary>
    /// L?y refund history theo ID
    /// </summary>
    Task<RefundHistoryEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y refund history theo payment ID
    /// </summary>
    Task<RefundHistoryEntity?> GetByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// L?y danh sách refund histories theo user ID
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y danh sách refund histories theo user ID ch? v?i status PENDING và COMPLETED
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetProcessableRefundsByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y danh sách refund histories theo tr?ng thái
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByStatusAsync(RefundStatus status);

    /// <summary>
    /// L?y danh sách refund histories v?i phân trang và filter
    /// </summary>
    Task<PagedResult<RefundHistoryEntity>> GetPagedAsync(GetRefundHistoriesRequest request);

    /// <summary>
    /// Ki?m tra payment ?ã có refund history ch?a
    /// </summary>
    Task<bool> PaymentHasRefundAsync(Guid paymentId);

    /// <summary>
    /// ??m s? l??ng refund histories theo tr?ng thái
    /// </summary>
    Task<int> CountByStatusAsync(RefundStatus status);

    /// <summary>
    /// ??m s? l??ng refund histories c?a user
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);

    /// <summary>
    /// T?o refund history m?i
    /// </summary>
    Task<RefundHistoryEntity> CreateAsync(RefundHistoryEntity entity);

    /// <summary>
    /// C?p nh?t refund history
    /// </summary>
    Task<RefundHistoryEntity> UpdateAsync(RefundHistoryEntity entity);

    /// <summary>
    /// Xóa refund history
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// L?y danh sách refund histories c?n x? lý (WAITING -> PENDING khi user có bank account)
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetPendingProcessAsync();
}