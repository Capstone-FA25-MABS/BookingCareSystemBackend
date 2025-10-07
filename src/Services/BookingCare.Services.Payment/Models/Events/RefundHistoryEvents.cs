using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.Events;

/// <summary>
/// Event ???c publish khi refund history ???c t?o
/// </summary>
public class RefundHistoryCreatedEvent
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID c?a payment ???c refund
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID c?a user yêu c?u refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// S? ti?n refund
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Tr?ng thái ban ??u
    /// </summary>
    public RefundStatus Status { get; set; }

    /// <summary>
    /// Lý do refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Th?i gian t?o
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ID c?a bank account (n?u có)
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

/// <summary>
/// Event ???c publish khi tr?ng thái refund history thay ??i
/// </summary>
public class RefundHistoryStatusChangedEvent
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID c?a payment ???c refund
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID c?a user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tr?ng thái c?
    /// </summary>
    public RefundStatus OldStatus { get; set; }

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    public RefundStatus NewStatus { get; set; }

    /// <summary>
    /// S? ti?n refund
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// ID c?a staff x? lý
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Ghi chú t? staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// Ngày chuy?n ti?n (n?u status = COMPLETED)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Th?i gian c?p nh?t
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// ID c?a bank account (n?u có)
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

/// <summary>
/// Event ???c publish khi refund history ???c hoàn thành
/// </summary>
public class RefundHistoryCompletedEvent
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID c?a payment ???c refund
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID c?a user nh?n refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// S? ti?n ?ã refund
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// ID c?a bank account nh?n ti?n
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// Thông tin bank account
    /// </summary>
    public BankAccountInfo BankAccount { get; set; } = new();

    /// <summary>
    /// Ngày chuy?n ti?n
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// ID c?a staff x? lý
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Ghi chú t? staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// Th?i gian hoàn thành
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Thông tin bank account ?? include trong event
/// </summary>
public class BankAccountInfo
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // ?ã mask
    public string AccountName { get; set; } = string.Empty;
}