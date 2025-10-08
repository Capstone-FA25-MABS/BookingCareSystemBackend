using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// Response DTO for RefundHistory
/// </summary>
public class RefundHistoryResponse
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the bank account for the refund
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Bank account information (if any)
    /// </summary>
    public BankAccountResponse? BankAccount { get; set; }

    /// <summary>
    /// ID of the user who requested the refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Refund status
    /// </summary>
    public RefundStatus Status { get; set; }



    /// <summary>
    /// Transfer date
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// ID of the refunded payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Payment information being refunded
    /// </summary>
    public PaymentResponse? Payment { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Notes from staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID of the staff who processed the refund
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Time when the refund request was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of days since the refund request was created
    /// </summary>
    public int DaysFromCreated => (DateTime.UtcNow - CreatedAt).Days;

    /// <summary>
    /// Whether the refund can be processed (only when status = PENDING)
    /// </summary>
    public bool CanProcess => Status == RefundStatus.PENDING;

    /// <summary>
    /// Whether the bank account can be updated (only when status = WAITING)
    /// </summary>
    public bool CanUpdateBankAccount => Status == RefundStatus.WAITING;
}