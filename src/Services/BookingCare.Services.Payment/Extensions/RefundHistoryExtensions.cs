using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Extensions;

/// <summary>
/// Extension methods for RefundHistoryEntity
/// </summary>
public static class RefundHistoryExtensions
{
    /// <summary>
    /// Check if the refund history can be processed
    /// </summary>
    public static bool CanProcess(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.PENDING;
    }

    /// <summary>
    /// Check if the refund history can update bank account
    /// </summary>
    public static bool CanUpdateBankAccount(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.WAITING;
    }

    /// <summary>
    /// Check if the refund history can be deleted
    /// </summary>
    public static bool CanDelete(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status == RefundStatus.WAITING;
    }

    /// <summary>
    /// Get human-readable status display name
    /// </summary>
    public static string GetStatusDisplayName(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.Status switch
        {
            RefundStatus.WAITING => "Waiting",
            RefundStatus.PENDING => "Pending",
            RefundStatus.COMPLETED => "Completed",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get number of days since creation
    /// </summary>
    public static int GetDaysFromCreated(this RefundHistoryEntity refundHistory)
    {
        return (DateTime.UtcNow - refundHistory.CreatedAt).Days;
    }

    /// <summary>
    /// Check if the refund history is overdue (more than 30 days)
    /// </summary>
    public static bool IsOverdue(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.GetDaysFromCreated() > 30;
    }

    /// <summary>
    /// Check if the refund history is a priority case (more than 7 days)
    /// </summary>
    public static bool IsPriority(this RefundHistoryEntity refundHistory)
    {
        return refundHistory.GetDaysFromCreated() > 7 && refundHistory.Status == RefundStatus.PENDING;
    }

    /// <summary>
    /// Check if transition to a new status is allowed
    /// </summary>
    public static bool CanTransitionTo(this RefundHistoryEntity refundHistory, RefundStatus newStatus)
    {
        return refundHistory.Status switch
        {
            RefundStatus.WAITING => newStatus is RefundStatus.PENDING or RefundStatus.COMPLETED,
            RefundStatus.PENDING => newStatus is RefundStatus.COMPLETED or RefundStatus.WAITING,
            RefundStatus.COMPLETED => false, // Cannot transition from COMPLETED to another status
            _ => false
        };
    }

    /// <summary>
    /// Validate business rules before status transition
    /// </summary>
    public static void ValidateStatusTransition(this RefundHistoryEntity refundHistory, RefundStatus newStatus, Guid? bankAccountId = null)
    {
        if (!refundHistory.CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from status {refundHistory.Status} to {newStatus}");
        }

        switch (newStatus)
        {
            case RefundStatus.PENDING:
                if (!bankAccountId.HasValue && !refundHistory.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("A bank account is required to transition to PENDING");
                }
                break;

            case RefundStatus.COMPLETED:
                if (!bankAccountId.HasValue && !refundHistory.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("A bank account is required to complete the refund");
                }
                break;
        }
    }

    /// <summary>
    /// Update status with validation
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

        // Automatically set transfer date when status = COMPLETED
        if (newStatus == RefundStatus.COMPLETED && !refundHistory.TransferDate.HasValue)
        {
            refundHistory.TransferDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get priority level for sorting
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