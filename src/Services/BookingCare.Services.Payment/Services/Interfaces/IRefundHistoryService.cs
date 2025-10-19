using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface for RefundHistory
/// </summary>
public interface IRefundHistoryService
{
    /// <summary>
    /// Get refund history by ID
    /// </summary>
    Task<RefundHistoryResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get refund history by payment ID
    /// </summary>
    Task<RefundHistoryResponse?> GetByPaymentIdAsync(Guid paymentId);

    /// <summary>
    /// Get list of refund histories by user ID
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get list of refund histories by hospital ID
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetByHospitalIdAsync(Guid hospitalId);

    /// <summary>
    /// Get list of refund histories by user ID with status PENDING and COMPLETED only
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetProcessableRefundsByUserIdAsync(Guid userId);

    /// <summary>
    /// Get list of refund histories by status
    /// </summary>
    Task<IEnumerable<RefundHistoryResponse>> GetByStatusAsync(RefundStatus status);

    /// <summary>
    /// Get list of refund histories with pagination and optional status counts
    /// </summary>
    Task<RefundHistoryListResponse> GetPagedAsync(GetRefundHistoriesRequest request);

    /// <summary>
    /// Create new refund history
    /// </summary>
    Task<RefundHistoryResponse> CreateAsync(CreateRefundHistoryRequest request);

    /// <summary>
    /// Update refund history status
    /// </summary>
    Task<RefundHistoryResponse> UpdateStatusAsync(UpdateRefundHistoryStatusRequest request);

    /// <summary>
    /// Delete refund history
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Automatically update refund histories from WAITING to PENDING when user has a bank account
    /// </summary>
    Task<int> ProcessWaitingRefundsAsync();

    /// <summary>
    /// Get refund statistics by status
    /// </summary>
    Task<Dictionary<RefundStatus, int>> GetRefundStatisticsAsync();

    /// <summary>
    /// Check if payment can be refunded
    /// </summary>
    Task<bool> CanRefundPaymentAsync(Guid paymentId);

    /// <summary>
    /// Mark refund as transferred (completed)
    /// Updates refund status to COMPLETED, updates payment status to REFUNDED,
    /// and sends notification to patient
    /// </summary>
    Task<RefundHistoryResponse> MarkAsTransferredAsync(Guid refundHistoryId, string? staffNotes = null);

    /// <summary>
    /// Report bank account issue
    /// Sends notification to patient about incorrect bank account information
    /// </summary>
    Task ReportBankIssueAsync(Guid refundHistoryId, string issueDescription);
}