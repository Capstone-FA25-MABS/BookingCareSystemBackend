using BookingCare.Services.Discount.Enums;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Models.Entities;

namespace BookingCare.Services.Discount.Repositories;

public interface IDiscountRepository
{
    // Basic CRUD operations
    Task<DiscountEntity?> GetByIdAsync(Guid id);
    Task<DiscountEntity?> GetByCodeAsync(string code);
    Task<DiscountEntity> CreateAsync(DiscountEntity discount);
    Task<DiscountEntity> UpdateAsync(DiscountEntity discount);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);

    // Query operations
    Task<(List<DiscountEntity> Discounts, int TotalCount)> GetDiscountsAsync(DiscountQueryRequest query);
    Task<List<DiscountEntity>> GetActiveDiscountsByClinicAsync(Guid clinicId);
    Task<List<DiscountEntity>> GetApplicableDiscountsAsync(Guid clinicId, Guid? specialtyId = null, Guid? doctorId = null);
    Task<DiscountEntity?> GetValidDiscountAsync(string code, Guid clinicId, Guid? specialtyId = null, Guid? doctorId = null);

    // Usage operations
    Task<bool> IncrementUsageAsync(Guid discountId);
    Task<bool> DecrementUsageAsync(Guid discountId);
    Task<int> GetRemainingUsesAsync(Guid discountId);

    // Status operations
    Task<bool> UpdateStatusAsync(Guid id, DiscountStatus status);
    Task<List<DiscountEntity>> GetExpiredDiscountsAsync();
    Task<int> UpdateExpiredDiscountsAsync();
}
