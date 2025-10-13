using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request to create a new refund history
/// </summary>
public class CreateRefundHistoryRequest
{
    /// <summary>
    /// ID of the payment to refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID of the user requesting the refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID of the hospital that needs to process the refund
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid HospitalId { get; set; }

    /// <summary>
    /// ID of the bank account for refund (optional - may be null)
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    [Required]
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    [MaxLength(500, ErrorMessage = "Refund reason must not exceed 500 characters")]
    public string? RefundReason { get; set; }
}

/// <summary>
/// Request to update refund history status
/// </summary>
public class UpdateRefundHistoryStatusRequest
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    [Required]
    [JsonRequired]
    public Guid Id { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    [Required]
    [JsonRequired]
    public RefundStatus Status { get; set; }

    /// <summary>
    /// ID of the bank account for refund (when transitioning from WAITING to PENDING)
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Transfer date (when status = COMPLETED)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Staff notes
    /// </summary>
    [MaxLength(500, ErrorMessage = "Staff notes must not exceed 500 characters")]
    public string? StaffNotes { get; set; }

    /// <summary>
    /// ID of the staff who processed the refund
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }
}

/// <summary>
/// Request to get a paged list of refund histories
/// </summary>
public class GetRefundHistoriesRequest
{
    /// <summary>
    /// User ID (optional - get all if null)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Hospital ID (optional - get all if null)
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Refund status (optional - get all if null)
    /// </summary>
    public RefundStatus? Status { get; set; }

    /// <summary>
    /// From date (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// To date (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Page number (starts from 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Include status counts in response (for all statuses)
    /// </summary>
    public bool IncludeStatusCounts { get; set; } = false;
}

/// <summary>
/// Request to mark refund as transferred (completed)
/// </summary>
public class MarkAsTransferredRequest
{
    /// <summary>
    /// Staff notes about the transfer
    /// </summary>
    [MaxLength(500, ErrorMessage = "Staff notes must not exceed 500 characters")]
    public string? StaffNotes { get; set; }
}

/// <summary>
/// Request to report bank account issue
/// </summary>
public class ReportBankIssueRequest
{
    /// <summary>
    /// Description of the bank account issue
    /// </summary>
    [Required(ErrorMessage = "Issue description is required")]
    [MaxLength(1000, ErrorMessage = "Issue description must not exceed 1000 characters")]
    public required string IssueDescription { get; set; }
}