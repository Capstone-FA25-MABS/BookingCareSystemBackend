using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanResponse?> GetByIdAsync(Guid id);
    Task<SubscriptionPlanResponse?> GetByNameAsync(string name);
    Task<SubscriptionPlanListResponse> GetAllAsync();
    Task<SubscriptionPlanListResponse> GetFilteredAsync(SubscriptionPlanFilterRequest filter);
    Task<SubscriptionPlanDetailResponse> CreateAsync(CreateSubscriptionPlanRequest request);
    Task<SubscriptionPlanResponse> UpdateAsync(Guid id, UpdateSubscriptionPlanRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<List<SubscriptionPlanResponse>> GetActiveAsync();
}
