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
    /// ID of the hospital that needs to process the refund
    /// </summary>
    public Guid HospitalId { get; set; }

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

/// <summary>
/// Refund status counts for all statuses
/// </summary>
public class RefundStatusCounts
{
    public int Waiting { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Rejected { get; set; }
    public int Total { get; set; }
}

/// <summary>
/// Response for refund history list with pagination
/// </summary>
public class RefundHistoryListResponse
{
    public List<RefundHistoryResponse> RefundHistories { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Counts for each status - only populated when IncludeStatusCounts = true
    /// </summary>
    public RefundStatusCounts? StatusCounts { get; set; }
}