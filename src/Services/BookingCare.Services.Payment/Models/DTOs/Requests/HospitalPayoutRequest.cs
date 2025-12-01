using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Payment.Models.DTOs.Requests;

/// <summary>
/// Request to generate payouts for a specific period
/// </summary>
public class GeneratePayoutsRequest
{
    [Required]
    public DateTime PeriodStartDate { get; set; }

    [Required]
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Optional: specific hospital IDs to generate payouts for
    /// If null/empty, generate for all hospitals with completed appointments
    /// </summary>
    public List<Guid>? HospitalIds { get; set; }
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
