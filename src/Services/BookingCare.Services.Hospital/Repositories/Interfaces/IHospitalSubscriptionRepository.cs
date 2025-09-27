using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Repositories.Interfaces;

public interface IHospitalSubscriptionRepository
{
    Task<HospitalSubscriptionEntity?> GetByIdAsync(Guid id);
    Task<List<HospitalSubscriptionEntity>> GetAllAsync();
    Task<(List<HospitalSubscriptionEntity> subscriptions, int totalCount)> GetFilteredAsync(HospitalSubscriptionFilterRequest filter);
    Task<HospitalSubscriptionEntity> CreateAsync(HospitalSubscriptionEntity subscription);
    Task<HospitalSubscriptionEntity> UpdateAsync(HospitalSubscriptionEntity subscription);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<List<HospitalSubscriptionEntity>> GetByHospitalIdAsync(Guid hospitalId);
    Task<List<HospitalSubscriptionEntity>> GetBySubscriptionIdAsync(Guid subscriptionId);
    Task<HospitalSubscriptionEntity?> GetActiveByHospitalIdAsync(Guid hospitalId);
    Task<List<HospitalSubscriptionEntity>> GetExpiringSoonAsync(int days = 30);
}
