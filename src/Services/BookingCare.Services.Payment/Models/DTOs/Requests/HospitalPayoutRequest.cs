using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request to generate payouts for a specific period
/// </summary>
public class GeneratePayoutsRequest
{
    [Required]
    public required DateTime PeriodStartDate { get; set; }

    [Required]
    public required DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Hospital ID requesting the payout
    /// </summary>
    [Required]
    public required Guid HospitalId { get; set; }

    /// <summary>
    /// Hospital name (sent from frontend to avoid gRPC call)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string HospitalName { get; set; }
}

/// <summary>
/// Request to mark a payout as completed
/// </summary>
public class MarkPayoutCompletedRequest
{
    /// <summary>
    /// Optional notes from admin about the payment
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Query parameters for filtering payouts
/// </summary>
public class PayoutQueryRequest
{
    /// <summary>
    /// Filter by period start date
    /// </summary>
    public DateTime? PeriodStartDate { get; set; }

    /// <summary>
    /// Filter by period end date
    /// </summary>
    public DateTime? PeriodEndDate { get; set; }

    /// <summary>
    /// Filter by period (month/year), e.g., "2024-11"
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by hospital ID
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Filter by hospital name (case-insensitive partial match)
    /// </summary>
    public string? HospitalName { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}
