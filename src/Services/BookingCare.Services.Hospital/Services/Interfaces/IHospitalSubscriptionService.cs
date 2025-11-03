using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface IHospitalSubscriptionService
{
    Task<HospitalSubscriptionResponse?> GetByIdAsync(Guid id);
    Task<HospitalSubscriptionResponse?> GetActiveByHospitalIdAsync(Guid hospitalId);
    Task<List<HospitalSubscriptionResponse>> GetByHospitalIdAsync(Guid hospitalId);
    Task<HospitalSubscriptionResponse> CreateAsync(CreateHospitalSubscriptionRequest request);
    Task<HospitalSubscriptionResponse> UpdateAsync(Guid id, UpdateHospitalSubscriptionRequest request);
    Task<bool> CancelSubscriptionAsync(Guid id, string cancellationReason = "");
    Task<List<HospitalSubscriptionResponse>> GetExpiringSoonAsync(int days = 30);

    // New advanced methods
    Task<HospitalSubscriptionResponse> UpgradeSubscriptionAsync(Guid currentSubscriptionId, Guid newSubscriptionPlanId);
    Task<HospitalSubscriptionResponse> ExtendSubscriptionAsync(Guid subscriptionId, int additionalMonths);
    Task<HospitalSubscriptionResponse> ConvertTrialToPaidAsync(Guid trialSubscriptionId, Guid paidSubscriptionPlanId);
}
