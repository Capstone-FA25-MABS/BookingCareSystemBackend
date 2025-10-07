using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Extensions;

/// <summary>
/// Extension methods cho RefundHistoryEntity
/// </summary>
public static class RefundHistoryExtensions
{
    /// <summary>
    /// Ki?m tra refund history có th? x? lý không
    /// </summary>
    public static bool CanProcess(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.PENDING;
    }

    /// <summary>
    /// Ki?m tra refund history có th? c?p nh?t bank account không
    /// </summary>
    public static bool CanUpdateBankAccount(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.WAITING;
    }

    /// <summary>
    /// Ki?m tra refund history có th? xóa không
    /// </summary>
    public static bool CanDelete(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.WAITING;
    }

    /// <summary>
    /// L?y tên tr?ng thái ?? hi?n th?
    /// </summary>
    public static string GetStatusDisplayName(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status switch
        {
            RefundStatus.WAITING => "?ang ch?",
            RefundStatus.PENDING => "?ang x? lý",
            RefundStatus.COMPLETED => "Hoàn thành",
            _ => "Không xác ??nh"
        };
    }

    /// <summary>
    /// L?y s? ngày t? khi t?o yêu c?u
    /// </summary>
    public static int GetDaysFromCreated(this RefundHistoryEntity refundHistory)
    {
        return (DateTime.UtcNow - refundHistory.CreatedAt).Days;
    }

    /// <summary>
    /// Ki?m tra refund history có quá h?n không (quá 30 ngày)
    /// </summary>
    public static bool IsOverdue(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.GetDaysFromCreated() > 30;
    }

    /// <summary>
    /// Ki?m tra refund history có ph?i là priority case không (quá 7 ngày)
    /// </summary>
    public static bool IsPriority(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.GetDaysFromCreated() > 7 && refundHistory.Status == RefundStatus.PENDING;
    }

    /// <summary>
    /// Ki?m tra có th? chuy?n sang status m?i không
    /// </summary>
    public static bool CanTransitionTo(this RefundHistoryEntity refundHistory, RefundStatus newStatus)
    {
        return refundHistory.Status switch
        {
            RefundStatus.WAITING => newStatus is RefundStatus.PENDING or RefundStatus.COMPLETED,
            RefundStatus.PENDING => newStatus is RefundStatus.COMPLETED or RefundStatus.WAITING,
            RefundStatus.COMPLETED => false, // Không th? chuy?n t? COMPLETED sang status khác
            _ => false
        };
    }

    /// <summary>
    /// Validate business rules tr??c khi chuy?n status
    /// </summary>
    public static void ValidateStatusTransition(this RefundHistoryEntity refundHistory, RefundStatus newStatus, Guid? bankAccountId = null)
    {
        if (!refundHistory.CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Không th? chuy?n t? status {refundHistory.Status} sang {newStatus}");
        }

        switch (newStatus)
        {
            case RefundStatus.PENDING:
                if (!bankAccountId.HasValue && !refundHistory.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Ph?i có bank account ?? chuy?n sang status PENDING");
                }
                break;

            case RefundStatus.COMPLETED:
                if (!bankAccountId.HasValue && !refundHistory.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Ph?i có bank account ?? hoàn thành refund");
                }
                break;
        }
    }

    /// <summary>
    /// C?p nh?t status v?i validation
    /// </summary>
    public static void UpdateStatus(this RefundHistoryEntity refundHistory, RefundStatus newStatus, Guid? bankAccountId = null, string? staffNotes = null, Guid? processedByStaffId = null)
    {
        refundHistory.ValidateStatusTransition(newStatus, bankAccountId);

        refundHistory.Status = newStatus;
        refundHistory.UpdatedAt = DateTime.UtcNow;

        if (bankAccountId.HasValue)
            refundHistory.BankAccountId = bankAccountId.Value;

        if (!string.IsNullOrEmpty(staffNotes))
            refundHistory.StaffNotes = staffNotes;

        if (processedByStaffId.HasValue)
            refundHistory.ProcessedByStaffId = processedByStaffId.Value;

        // T? ??ng set transfer date khi status = COMPLETED
        if (newStatus == RefundStatus.COMPLETED && !refundHistory.TransferDate.HasValue)
        {
            refundHistory.TransferDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// L?y priority level cho sorting
    /// </summary>
    public static int GetPriorityLevel(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status switch
        {
            RefundStatus.PENDING when refundHistory.IsPriority() => 1, // Highest priority
            RefundStatus.PENDING => 2,
            RefundStatus.WAITING when refundHistory.IsOverdue() => 3,
            RefundStatus.WAITING => 4,
            RefundStatus.COMPLETED => 5, // Lowest priority
            _ => 6
        };
    }
}