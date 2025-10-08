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
}