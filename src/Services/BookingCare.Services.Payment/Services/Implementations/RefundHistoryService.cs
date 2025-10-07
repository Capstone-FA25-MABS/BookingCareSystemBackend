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
/// Service implementation cho RefundHistory
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
    /// L?y refund history theo ID
    /// </summary>
    public async Task<RefundHistoryResponse?> GetByIdAsync(Guid id)
    {
        var refundHistory = await _refundHistoryRepository.GetByIdAsync(id);
        return refundHistory != null ? _mapper.Map<RefundHistoryResponse>(refundHistory) : null;
    }

    /// <summary>
    /// L?y refund history theo payment ID
    /// </summary>
    public async Task<RefundHistoryResponse?> GetByPaymentIdAsync(Guid paymentId)
    {
        var refundHistory = await _refundHistoryRepository.GetByPaymentIdAsync(paymentId);
        return refundHistory != null ? _mapper.Map<RefundHistoryResponse>(refundHistory) : null;
    }

    /// <summary>
    /// L?y danh sách refund histories theo user ID
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetByUserIdAsync(Guid userId)
    {
        var refundHistories = await _refundHistoryRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    /// <summary>
    /// L?y danh sách refund histories theo tr?ng thái
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetByStatusAsync(RefundStatus status)
    {
        var refundHistories = await _refundHistoryRepository.GetByStatusAsync(status);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    /// <summary>
    /// L?y danh sách refund histories v?i phân trang
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
    /// T?o refund history m?i
    /// </summary>
    public async Task<RefundHistoryResponse> CreateAsync(CreateRefundHistoryRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang t?o refund history cho Payment: {PaymentId}, User: {UserId}",
                null, request.PaymentId, request.UserId);

            // Validation
            ValidateRequired(request, nameof(request));

            // Ki?m tra payment có t?n t?i và có th? refund không
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                throw new NotFoundException("Payment", request.PaymentId);
            }

            // Ki?m tra payment ?ã ???c refund ch?a
            var existingRefund = await _refundHistoryRepository.PaymentHasRefundAsync(request.PaymentId);
            if (existingRefund)
            {
                throw new ConflictException($"Payment {request.PaymentId} ?ã có refund history");
            }

            // Ki?m tra payment status ph?i là COMPLETED ?? có th? refund
            if (payment.Status != PaymentStatus.COMPLETED)
            {
                throw new InvalidOperationException($"Ch? có th? refund payment có status COMPLETED. Payment hi?n t?i: {payment.Status}");
            }

            // Ki?m tra s? ti?n refund không v??t quá s? ti?n payment
            if (request.RefundAmount > payment.Amount)
            {
                throw new InvalidOperationException($"S? ti?n refund ({request.RefundAmount}) không ???c v??t quá s? ti?n payment ({payment.Amount})");
            }

            // T? ??ng xác ??nh tr?ng thái và bank account
            var (status, bankAccountId) = await DetermineRefundStatusAsync(request.UserId, request.BankAccountId);

            // T?o entity
            var entity = _mapper.Map<RefundHistoryEntity>(request);
            entity.Status = status;
            entity.BankAccountId = bankAccountId;

            var created = await _refundHistoryRepository.CreateAsync(entity);

            LogInfo("Refund history ???c t?o thành công v?i ID: {Id}, Status: {Status}",
                null, created.Id, created.Status);

            return _mapper.Map<RefundHistoryResponse>(created);
        }, "CreateRefundHistory");
    }

    /// <summary>
    /// C?p nh?t tr?ng thái refund history
    /// </summary>
    public async Task<RefundHistoryResponse> UpdateStatusAsync(UpdateRefundHistoryStatusRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang c?p nh?t refund history v?i ID: {Id} sang status: {Status}",
                null, request.Id, request.Status);

            ValidateRequired(request, nameof(request));

            var existing = await _refundHistoryRepository.GetByIdAsync(request.Id);
            if (existing == null)
            {
                throw new NotFoundException("RefundHistory", request.Id);
            }

            // Validate status transition
            ValidateStatusTransition(existing.Status, request.Status);

            // Validate business rules cho t?ng status
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

            // T? ??ng set transfer date khi status = COMPLETED
            if (request.Status == RefundStatus.COMPLETED && !existing.TransferDate.HasValue)
            {
                existing.TransferDate = DateTime.UtcNow;
            }

            var updated = await _refundHistoryRepository.UpdateAsync(existing);

            LogInfo("Refund history ???c c?p nh?t thành công v?i ID: {Id}, Status: {Status}",
                null, updated.Id, updated.Status);

            return _mapper.Map<RefundHistoryResponse>(updated);
        }, "UpdateRefundHistoryStatus");
    }

    /// <summary>
    /// Xóa refund history
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang xóa refund history v?i ID: {Id}", null, id);

            var existing = await _refundHistoryRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            // Ch? cho phép xóa khi status = WAITING
            if (existing.Status != RefundStatus.WAITING)
            {
                throw new InvalidOperationException($"Ch? có th? xóa refund history có status WAITING. Status hi?n t?i: {existing.Status}");
            }

            var result = await _refundHistoryRepository.DeleteAsync(id);

            LogInfo("Refund history ???c xóa thành công v?i ID: {Id}", null, id);
            return result;
        }, "DeleteRefundHistory");
    }

    /// <summary>
    /// T? ??ng c?p nh?t refund histories t? WAITING sang PENDING khi user có bank account
    /// </summary>
    public async Task<int> ProcessWaitingRefundsAsync()
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("?ang x? lý các refund history WAITING", null);

            var waitingRefunds = await _refundHistoryRepository.GetPendingProcessAsync();
            int processedCount = 0;

            foreach (var refund in waitingRefunds)
            {
                try
                {
                    // L?y default bank account c?a user
                    var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(refund.UserId);
                    if (defaultBankAccount != null)
                    {
                        refund.Status = RefundStatus.PENDING;
                        refund.BankAccountId = defaultBankAccount.Id;
                        await _refundHistoryRepository.UpdateAsync(refund);
                        processedCount++;

                        LogInfo("Refund history {Id} ???c chuy?n sang PENDING v?i bank account {BankAccountId}",
                            null, refund.Id, defaultBankAccount.Id);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "L?i khi x? lý refund history {Id}", null, refund.Id);
                }
            }

            LogInfo("?ã x? lý {Count} refund histories t? WAITING sang PENDING", null, processedCount);
            return processedCount;
        }, "ProcessWaitingRefunds");
    }

    /// <summary>
    /// L?y th?ng kê refund theo tr?ng thái
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
    /// Ki?m tra payment có th? refund không
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
    /// L?y danh sách refund histories theo user ID ch? v?i status PENDING và COMPLETED
    /// </summary>
    public async Task<IEnumerable<RefundHistoryResponse>> GetProcessableRefundsByUserIdAsync(Guid userId)
    {
        var refundHistories = await _refundHistoryRepository.GetProcessableRefundsByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<RefundHistoryResponse>>(refundHistories);
    }

    #region Private Methods

    /// <summary>
    /// Xác ??nh tr?ng thái refund d?a trên user bank account
    /// </summary>
    private async Task<(RefundStatus status, Guid? bankAccountId)> DetermineRefundStatusAsync(Guid userId, Guid? requestedBankAccountId)
    {
        // N?u có ch? ??nh bank account c? th?
        if (requestedBankAccountId.HasValue)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(requestedBankAccountId.Value);
            if (bankAccount != null && bankAccount.UserId == userId && bankAccount.IsActive)
            {
                return (RefundStatus.PENDING, requestedBankAccountId.Value);
            }
        }

        // Ki?m tra default bank account c?a user
        var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(userId);
        if (defaultBankAccount != null)
        {
            return (RefundStatus.PENDING, defaultBankAccount.Id);
        }

        // User ch?a có bank account active nào
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
            [RefundStatus.COMPLETED] = [] // Không th? chuy?n t? COMPLETED sang status khác
        };

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidOperationException($"Không th? chuy?n t? status {currentStatus} sang {newStatus}");
        }
    }

    /// <summary>
    /// Validate business rules khi update status
    /// </summary>
    private async Task ValidateStatusUpdateAsync(RefundHistoryEntity existing, UpdateRefundHistoryStatusRequest request)
    {
        switch (request.Status)
        {
            case RefundStatus.PENDING:
                // Ph?i có bank account khi chuy?n sang PENDING
                if (!request.BankAccountId.HasValue && !existing.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Ph?i có bank account ?? chuy?n sang status PENDING");
                }

                // Validate bank account belongs to user và active
                var bankAccountId = request.BankAccountId ?? existing.BankAccountId!.Value;
                var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
                if (bankAccount == null || bankAccount.UserId != existing.UserId || !bankAccount.IsActive)
                {
                    throw new InvalidOperationException("Bank account không h?p l? ho?c không thu?c v? user");
                }
                break;

            case RefundStatus.COMPLETED:
                // Ph?i có bank account khi hoàn thành
                if (!request.BankAccountId.HasValue && !existing.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Ph?i có bank account ?? hoàn thành refund");
                }

                // Transfer date s? ???c t? ??ng set n?u không có
                break;
        }
    }

    #endregion
}