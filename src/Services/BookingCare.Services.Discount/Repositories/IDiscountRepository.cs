using BookingCare.Services.Discount.Enums;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Models.Entities;

namespace BookingCare.Services.Discount.Repositories;

public interface IDiscountRepository
{
    // Basic CRUD operations
    Task<DiscountEntity?> GetByIdAsync(long id);
    Task<DiscountEntity?> GetByCodeAsync(string code);
    Task<DiscountEntity> CreateAsync(DiscountEntity discount);
    Task<DiscountEntity> UpdateAsync(DiscountEntity discount);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null);

    // Query operations
    Task<(List<DiscountEntity> Discounts, int TotalCount)> GetDiscountsAsync(DiscountQueryRequest query);
    Task<List<DiscountEntity>> GetActiveDiscountsByClinicAsync(long clinicId);
    Task<List<DiscountEntity>> GetApplicableDiscountsAsync(long clinicId, long? specialtyId = null, long? doctorId = null);
    Task<DiscountEntity?> GetValidDiscountAsync(string code, long clinicId, long? specialtyId = null, long? doctorId = null);

    // Usage operations
    Task<bool> IncrementUsageAsync(long discountId);
    Task<bool> DecrementUsageAsync(long discountId);
    Task<int> GetRemainingUsesAsync(long discountId);

    // Status operations
    Task<bool> UpdateStatusAsync(long id, DiscountStatus status);
    Task<List<DiscountEntity>> GetExpiredDiscountsAsync();
    Task<int> UpdateExpiredDiscountsAsync();
}
