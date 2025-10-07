using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface cho RefundHistory
/// </summary>
public interface IRefundHistoryService
{
    /// <summary>
    /// L?y refund history theo ID
    /// </summary>
    Task<RefundHistoryResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y refund history theo payment ID
    /// </summary>
    Task<RefundHistoryResponse?> GetByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// L?y danh sách refund histories theo user ID
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y danh sách refund histories theo user ID ch? v?i status PENDING và COMPLETED
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetProcessableRefundsByUserIdAsync(Guid userId);

    /// <summary>
    /// L?y danh sách refund histories theo tr?ng thái
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetByStatusAsync(RefundStatus status);

    /// <summary>
    /// L?y danh sách refund histories v?i phân trang
    /// </summary>
    Task<PagedResult<RefundHistoryResponse>> GetPagedAsync(GetRefundHistoriesRequest request);

    /// <summary>
    /// T?o refund history m?i
    /// </summary>
    Task<RefundHistoryResponse> CreateAsync(CreateRefundHistoryRequest request);

    /// <summary>
    /// C?p nh?t tr?ng thái refund history
    /// </summary>
    Task<RefundHistoryResponse> UpdateStatusAsync(UpdateRefundHistoryStatusRequest request);

    /// <summary>
    /// Xóa refund history
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// T? ??ng c?p nh?t refund histories t? WAITING sang PENDING khi user có bank account
    /// </summary>
    Task<int> ProcessWaitingRefundsAsync();

    /// <summary>
    /// L?y th?ng kê refund theo tr?ng thái
    /// </summary>
    Task<Dictionary<RefundStatus, int>> GetRefundStatisticsAsync();

    /// <summary>
    /// Ki?m tra payment có th? refund không
    /// </summary>
    Task<bool> CanRefundPaymentAsync(Guid paymentId);
}