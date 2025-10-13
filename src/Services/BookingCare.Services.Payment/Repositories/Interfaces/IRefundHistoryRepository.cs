using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Repository interface for RefundHistory
/// </summary>
public interface IRefundHistoryRepository
{
    /// <summary>
    /// Get refund history by ID
    /// </summary>
    Task<RefundHistoryEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get refund history by payment ID
    /// </summary>
    Task<RefundHistoryEntity?> GetByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// Get list of refund histories by user ID
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get list of refund histories by hospital ID
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByHospitalIdAsync(Guid hospitalId);

    /// <summary>
    /// Get list of refund histories by user ID with status PENDING and COMPLETED only
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetProcessableRefundsByUserIdAsync(Guid userId);

    /// <summary>
    /// Get list of refund histories by status
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByStatusAsync(RefundStatus status);

    /// <summary>
    /// Get list of refund histories by user ID and specific status (optimized for update - no includes)
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetByUserIdAndStatusForUpdateAsync(Guid userId, RefundStatus status);

    /// <summary>
    /// Get list of refund histories with pagination and filters
    /// </summary>
    Task<PagedResult<RefundHistoryEntity>> GetPagedAsync(GetRefundHistoriesRequest request);

    /// <summary>
    /// Check if a payment already has a refund history
    /// </summary>
    Task<bool> PaymentHasRefundAsync(Guid paymentId);

    /// <summary>
    /// Count refund histories by status
    /// </summary>
    Task<int> CountByStatusAsync(RefundStatus status);

    /// <summary>
    /// Count refund histories of a user
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId);

    /// <summary>
    /// Create a new refund history
    /// </summary>
    Task<RefundHistoryEntity> CreateAsync(RefundHistoryEntity entity);

    /// <summary>
    /// Update refund history
    /// </summary>
    Task<RefundHistoryEntity> UpdateAsync(RefundHistoryEntity entity);

    /// <summary>
    /// Delete refund history
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Get list of refund histories that need processing (WAITING -> PENDING when user has a bank account)
    /// </summary>
    Task<IEnumerable<RefundHistoryEntity>> GetPendingProcessAsync();

    /// <summary>
    /// Get status counts for a hospital (GROUP BY status)
    /// </summary>
    Task<Dictionary<RefundStatus, int>> GetStatusCountsByHospitalAsync(Guid? hospitalId);
}