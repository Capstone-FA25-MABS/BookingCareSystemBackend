using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlanEntity?> GetByIdAsync(Guid id);
    Task<SubscriptionPlanEntity?> GetByNameAsync(string name);
    Task<List<SubscriptionPlanEntity>> GetAllAsync();
    Task<(List<SubscriptionPlanEntity> plans, int totalCount)> GetFilteredAsync(SubscriptionPlanFilterRequest filter);
    Task<SubscriptionPlanEntity> CreateAsync(SubscriptionPlanEntity plan);
    Task<SubscriptionPlanEntity> UpdateAsync(SubscriptionPlanEntity plan);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
    Task<List<SubscriptionPlanEntity>> GetActiveAsync();
}
