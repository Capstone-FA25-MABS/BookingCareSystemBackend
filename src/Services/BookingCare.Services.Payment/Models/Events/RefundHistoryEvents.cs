using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Events;

/// <summary>
/// Event published when a refund history record is created
/// </summary>
public class RefundHistoryCreatedEvent
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the refunded payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID of the user who requested the refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Initial status
    /// </summary>
    public RefundStatus Status { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ID of the bank account (if any)
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

/// <summary>
/// Event published when a refund history status is changed
/// </summary>
public class RefundHistoryStatusChangedEvent
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the refunded payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID of the user associated with the refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Old status
    /// </summary>
    public RefundStatus OldStatus { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public RefundStatus NewStatus { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// ID of the staff who processed the change
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Notes from staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// Transfer date (if status = COMPLETED)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// ID of the bank account (if any)
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

/// <summary>
/// Event published when a refund history is completed
/// </summary>
public class RefundHistoryCompletedEvent
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the refunded payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID of the user receiving the refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Amount refunded
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// ID of the receiving bank account
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// Bank account information
    /// </summary>
    public BankAccountInfo BankAccount { get; set; } = new();

    /// <summary>
    /// Transfer date
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// ID of the staff who processed the refund
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Notes from staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// Completion time
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Bank account information included in the event
/// </summary>
public class BankAccountInfo
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // masked
    public string AccountName { get; set; } = string.Empty;
}