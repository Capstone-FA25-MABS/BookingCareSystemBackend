using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for Hospital Payout Service
/// </summary>
public interface IHospitalPayoutService
{
    /// <summary>
    /// Get paginated list of payouts with filters
    /// </summary>
    Task<PagedResult<HospitalPayoutResponse>> GetPayoutsAsync(PayoutQueryRequest query);

    /// <summary>
    /// Get payout details by ID including appointment breakdown
    /// </summary>
    Task<PayoutDetailsResponse> GetPayoutDetailsAsync(Guid payoutId);

    /// <summary>
    /// Generate payouts for specified hospitals and period
    /// Calculates total amount from completed appointments
    /// Hospital initiates this request to get paid
    /// </summary>
    Task<List<HospitalPayoutResponse>> GeneratePayoutsAsync(GeneratePayoutsRequest request);

    /// <summary>
    /// Mark a payout as completed (admin has transferred money)
    /// </summary>
    Task<HospitalPayoutResponse> MarkPayoutCompletedAsync(
        Guid payoutId,
        MarkPayoutCompletedRequest request,
        Guid adminId
    );

    /// <summary>
    /// Get payout statistics
    /// </summary>
    Task<PayoutStatisticsResponse> GetStatisticsAsync();

    /// <summary>
    /// Get list of hospitals with pending payouts for a period
    /// </summary>
    Task<List<PendingHospitalInfoResponse>> GetHospitalsWithPendingPayoutsAsync(
        DateTime periodStart,
        DateTime periodEnd
    );
}
