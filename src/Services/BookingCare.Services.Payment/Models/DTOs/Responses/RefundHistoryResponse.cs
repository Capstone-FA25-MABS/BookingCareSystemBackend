using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO cho RefundHistory
/// </summary>
public class RefundHistoryResponse
{
    /// <summary>
    /// ID c?a refund history
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID c?a bank account ?? refund
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Thông tin bank account (n?u có)
    /// </summary>
    public BankAccountResponse? BankAccount { get; set; }

    /// <summary>
    /// ID c?a user yêu c?u refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tr?ng thái refund
    /// </summary>
    public RefundStatus Status { get; set; }



    /// <summary>
    /// Ngày chuy?n ti?n
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// ID c?a payment ???c refund
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Thông tin payment ???c refund
    /// </summary>
    public PaymentResponse? Payment { get; set; }

    /// <summary>
    /// S? ti?n refund
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Lý do refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Ghi chú t? staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID c?a staff x? lý refund
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Th?i gian t?o yêu c?u refund
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Th?i gian c?p nh?t cu?i cùng
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// S? ngày t? khi t?o yêu c?u
    /// </summary>
    public int DaysFromCreated => (DateTime.UtcNow - CreatedAt).Days;

    /// <summary>
    /// Có th? x? lý refund hay không (ch? khi status = PENDING)
    /// </summary>
    public bool CanProcess => Status == RefundStatus.PENDING;

    /// <summary>
    /// Có th? c?p nh?t bank account hay không (ch? khi status = WAITING)
    /// </summary>
    public bool CanUpdateBankAccount => Status == RefundStatus.WAITING;
}