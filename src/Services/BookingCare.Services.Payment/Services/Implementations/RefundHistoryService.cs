using AutoMapper;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Service implementation for RefundHistory
/// </summary>
public class RefundHistoryService : BaseService, IRefundHistoryService
{
    private readonly IRefundHistoryRepository _refundHistoryRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IMapper _mapper;

    public RefundHistoryService(
        IRefundHistoryRepository refundHistoryRepository,
        IPaymentRepository paymentRepository,
        IBankAccountRepository bankAccountRepository,
        IMapper mapper,
        ILogger<RefundHistoryService> logger) : base(logger)
    {
        _refundHistoryRepository = refundHistoryRepository;
        _paymentRepository = paymentRepository;
        _bankAccountRepository = bankAccountRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Get refund history by ID
    /// </summary>
    public async Task<RefundHistoryResponse?> GetByIdAsync(Guid id)
    {
        var refundHistory = await _refundHistoryRepository.GetByIdAsync(id);
        return refundHistory != null ? _mapper.Map<RefundHistoryResponse>(refundHistory) : null;
    }

    /// <summary>
    /// Get refund history by payment ID
    /// </summary>
    public async Task<RefundHistoryResponse?> GetByPaymentIdAsync(Guid paymentId)
    {
        var refundHistory = await _refundHistoryRepository.GetByPaymentIdAsync(paymentId);
        return refundHistory != null ? _mapper.Map<RefundHistoryResponse>(refundHistory) : null;
    }

    /// <summary>
    /// Get list of refund histories by user ID
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetByUserIdAsync(Guid userId)
    {
        var refundHistories = await _refundHistoryRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    /// <summary>
    /// Get list of refund histories by hospital ID
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetByHospitalIdAsync(Guid hospitalId)
    {
        var refundHistories = await _refundHistoryRepository.GetByHospitalIdAsync(hospitalId);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    /// <summary>
    /// Get list of refund histories by status
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetByStatusAsync(RefundStatus status)
    {
        var refundHistories = await _refundHistoryRepository.GetByStatusAsync(status);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    /// <summary>
    /// Get list of refund histories with pagination
    /// </summary>
    public async Task<PagedResult<RefundHistoryResponse>> GetPagedAsync(GetRefundHistoriesRequest request)
    {
        var pagedResult = await _refundHistoryRepository.GetPagedAsync(request);
        var mappedItems = _mapper.Map<List<RefundHistoryResponse>>(pagedResult.Items);

        return new PagedResult<RefundHistoryResponse>
        {
            Items = mappedItems,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
    }

    /// <summary>
    /// Create new refund history
    /// </summary>
    public async Task<RefundHistoryResponse> CreateAsync(CreateRefundHistoryRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating refund history for Payment: {PaymentId}, User: {UserId}, Hospital: {HospitalId}",
                null, request.PaymentId, request.UserId, request.HospitalId);

            // Validation
            ValidateRequired(request, nameof(request));

            // Check if payment exists and can be refunded
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                throw new NotFoundException("Payment", request.PaymentId);
            }

            // Validate that the HospitalId matches the payment's HospitalId (if payment has one)
            if (payment.HospitalId.HasValue && payment.HospitalId.Value != request.HospitalId)
            {
                throw new InvalidOperationException($"Hospital ID mismatch. Payment belongs to hospital {payment.HospitalId}, but refund is for hospital {request.HospitalId}");
            }

            // If payment doesn't have HospitalId (appointment payment), we still allow the refund with the provided HospitalId
            // This handles cases where the hospital needs to process refunds for appointments

            // Check if payment already has a refund history
            var existingRefund = await _refundHistoryRepository.PaymentHasRefundAsync(request.PaymentId);
            if (existingRefund)
            {
                throw new ConflictException($"Payment {request.PaymentId} already has a refund history");
            }

            // Check if payment status is COMPLETED to allow refund
            if (payment.Status != PaymentStatus.COMPLETED)
            {
                throw new InvalidOperationException($"Only payments with status COMPLETED can be refunded. Current payment status: {payment.Status}");
            }

            // Check if refund amount does not exceed payment amount
            if (request.RefundAmount > payment.Amount)
            {
                throw new InvalidOperationException($"Refund amount ({request.RefundAmount}) cannot exceed payment amount ({payment.Amount})");
            }

            // Automatically determine status and bank account
            var (status, bankAccountId) = await DetermineRefundStatusAsync(request.UserId, request.BankAccountId);

            // Create entity
            var entity = _mapper.Map<RefundHistoryEntity>(request);
            entity.Status = status;
            entity.BankAccountId = bankAccountId;
            entity.HospitalId = request.HospitalId; // Ensure HospitalId is set

            var created = await _refundHistoryRepository.CreateAsync(entity);

            LogInfo("Refund history created successfully with ID: {Id}, Status: {Status}, Hospital: {HospitalId}",
                null, created.Id, created.Status, created.HospitalId);

            return _mapper.Map<RefundHistoryResponse>(created);
        }, "CreateRefundHistory");
    }

    /// <summary>
    /// Update refund history status
    /// </summary>
    public async Task<RefundHistoryResponse> UpdateStatusAsync(UpdateRefundHistoryStatusRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating refund history with ID: {Id} to status: {Status}",
                null, request.Id, request.Status);

            ValidateRequired(request, nameof(request));

            var existing = await _refundHistoryRepository.GetByIdAsync(request.Id);
            if (existing == null)
            {
                throw new NotFoundException("RefundHistory", request.Id);
            }

            // Validate status transition
            ValidateStatusTransition(existing.Status, request.Status);

            // Validate business rules for each status
            await ValidateStatusUpdateAsync(existing, request);

            // Update entity
            existing.Status = request.Status;

            if (request.BankAccountId.HasValue)
                existing.BankAccountId = request.BankAccountId.Value;

            if (request.TransferDate.HasValue)
                existing.TransferDate = request.TransferDate.Value;

            if (!string.IsNullOrEmpty(request.StaffNotes))
                existing.StaffNotes = request.StaffNotes;

            if (request.ProcessedByStaffId.HasValue)
                existing.ProcessedByStaffId = request.ProcessedByStaffId.Value;

            // Automatically set transfer date when status = COMPLETED
            if (request.Status == RefundStatus.COMPLETED && !existing.TransferDate.HasValue)
            {
                existing.TransferDate = DateTime.UtcNow;
            }

            var updated = await _refundHistoryRepository.UpdateAsync(existing);

            LogInfo("Refund history updated successfully with ID: {Id}, Status: {Status}",
                null, updated.Id, updated.Status);

            return _mapper.Map<RefundHistoryResponse>(updated);
        }, "UpdateRefundHistoryStatus");
    }

    /// <summary>
    /// Delete refund history
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting refund history with ID: {Id}", null, id);

            var existing = await _refundHistoryRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            // Only allow deletion when status = WAITING
            if (existing.Status != RefundStatus.WAITING)
            {
                throw new InvalidOperationException($"Only refund histories with status WAITING can be deleted. Current status: {existing.Status}");
            }

            var result = await _refundHistoryRepository.DeleteAsync(id);

            LogInfo("Refund history deleted successfully with ID: {Id}", null, id);
            return result;
        }, "DeleteRefundHistory");
    }

    /// <summary>
    /// Automatically update refund histories from WAITING to PENDING when user has a bank account
    /// </summary>
    public async Task<int> ProcessWaitingRefundsAsync()
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Processing WAITING refund histories", null);

            var waitingRefunds = await _refundHistoryRepository.GetPendingProcessAsync();
            int processedCount = 0;

            foreach (var refund in waitingRefunds)
            {
                try
                {
                    // Get user's default bank account
                    var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(refund.UserId);
                    if (defaultBankAccount != null)
                    {
                        refund.Status = RefundStatus.PENDING;
                        refund.BankAccountId = defaultBankAccount.Id;
                        await _refundHistoryRepository.UpdateAsync(refund);
                        processedCount++;

                        LogInfo("Refund history {Id} moved to PENDING with bank account {BankAccountId}",
                            null, refund.Id, defaultBankAccount.Id);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error processing refund history {Id}", null, refund.Id);
                }
            }

            LogInfo("Processed {Count} refund histories from WAITING to PENDING", null, processedCount);
            return processedCount;
        }, "ProcessWaitingRefunds");
    }

    /// <summary>
    /// Get refund statistics by status
    /// </summary>
    public async Task<Dictionary<RefundStatus, int>> GetRefundStatisticsAsync()
    {
        var statistics = new Dictionary<RefundStatus, int>();

        foreach (RefundStatus status in Enum.GetValues<RefundStatus>())
        {
            var count = await _refundHistoryRepository.CountByStatusAsync(status);
            statistics[status] = count;
        }

        return statistics;
    }

    /// <summary>
    /// Check if payment can be refunded
    /// </summary>
    public async Task<bool> CanRefundPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null || payment.Status != PaymentStatus.COMPLETED)
        {
            return false;
        }

        var hasRefund = await _refundHistoryRepository.PaymentHasRefundAsync(paymentId);
        return !hasRefund;
    }

    /// <summary>
    /// Get list of refund histories by user ID with status PENDING and COMPLETED only
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetProcessableRefundsByUserIdAsync(Guid userId)
    {
        var refundHistories = await _refundHistoryRepository.GetProcessableRefundsByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    #region Private Methods

    /// <summary>
    /// Determine refund status based on user bank account
    /// </summary>
    private async Task<(RefundStatus status, Guid? bankAccountId)> DetermineRefundStatusAsync(Guid userId, Guid? requestedBankAccountId)
    {
        // If a specific bank account is provided
        if (requestedBankAccountId.HasValue)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(requestedBankAccountId.Value);
            if (bankAccount != null && bankAccount.UserId == userId && bankAccount.IsActive)
            {
                return (RefundStatus.PENDING, requestedBankAccountId.Value);
            }
        }

        // Check user's default bank account
        var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(userId);
        if (defaultBankAccount != null)
        {
            return (RefundStatus.PENDING, defaultBankAccount.Id);
        }

        // User does not have any active bank account
        return (RefundStatus.WAITING, null);
    }

    /// <summary>
    /// Validate status transition
    /// </summary>
    private static void ValidateStatusTransition(RefundStatus currentStatus, RefundStatus newStatus)
    {
        var validTransitions = new Dictionary<RefundStatus, RefundStatus[]>
        {
            [RefundStatus.WAITING] = [RefundStatus.PENDING, RefundStatus.COMPLETED],
            [RefundStatus.PENDING] = [RefundStatus.COMPLETED, RefundStatus.WAITING],
            [RefundStatus.COMPLETED] = [] // Cannot transition from COMPLETED to another status
        };

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from status {currentStatus} to {newStatus}");
        }
    }

    /// <summary>
    /// Validate business rules when updating status
    /// </summary>
    private async Task ValidateStatusUpdateAsync(RefundHistoryEntity existing, UpdateRefundHistoryStatusRequest request)
    {
        switch (request.Status)
        {
            case RefundStatus.PENDING:
                // Must have bank account when moving to PENDING
                if (!request.BankAccountId.HasValue && !existing.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Must have a bank account to move to status PENDING");
                }

                // Validate bank account belongs to user and is active
                var bankAccountId = request.BankAccountId ?? existing.BankAccountId!.Value;
                var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
                if (bankAccount == null || bankAccount.UserId != existing.UserId || !bankAccount.IsActive)
                {
                    throw new InvalidOperationException("Bank account is invalid or does not belong to user");
                }
                break;

            case RefundStatus.COMPLETED:
                // Must have bank account when completing refund
                if (!request.BankAccountId.HasValue && !existing.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Must have a bank account to complete refund");
                }

                // Transfer date will be set automatically if not provided
                break;
        }
    }

    #endregion
}