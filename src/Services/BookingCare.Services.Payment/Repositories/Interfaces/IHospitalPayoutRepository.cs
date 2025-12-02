using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface for Hospital Payout Repository
/// </summary>
public interface IHospitalPayoutRepository
{
    /// <summary>
    /// Get payout by ID
    /// </summary>
    Task<HospitalPayoutEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get paginated list of payouts with optional filters
    /// </summary>
    Task<PagedResult<HospitalPayoutEntity>> GetPayoutsAsync(PayoutQueryRequest query);

    /// <summary>
    /// Get payouts for a specific hospital
    /// </summary>
    Task<List<HospitalPayoutEntity>> GetByHospitalIdAsync(Guid hospitalId);

    /// <summary>
    /// Check if a payout exists for a hospital in a specific period
    /// </summary>
    Task<bool> ExistsForPeriodAsync(Guid hospitalId, DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Create a new payout record
    /// </summary>
    Task<HospitalPayoutEntity> CreateAsync(HospitalPayoutEntity payout);

    /// <summary>
    /// Update an existing payout
    /// </summary>
    Task<HospitalPayoutEntity> UpdateAsync(HospitalPayoutEntity payout);

    /// <summary>
    /// Get total pending payout amount for a hospital
    /// </summary>
    Task<decimal> GetTotalPendingAmountAsync(Guid? hospitalId = null);

    /// <summary>
    /// Get payout statistics
    /// </summary>
    Task<(
        int PendingCount,
        decimal PendingAmount,
        int CompletedCount,
        decimal CompletedAmount
    )> GetStatisticsAsync();
}
