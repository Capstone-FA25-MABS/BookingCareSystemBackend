using BookingCare.Services.Discount.Models.DTOs;

namespace BookingCare.Services.Discount.Services;

public interface IDiscountService
{
    // Basic CRUD operations
    Task<DiscountResponse> CreateDiscountAsync(CreateDiscountRequest request);
    Task<DiscountResponse?> GetDiscountByIdAsync(long id);
    Task<DiscountResponse?> GetDiscountByCodeAsync(string code);
    Task<DiscountResponse> UpdateDiscountAsync(UpdateDiscountRequest request);
    Task<bool> DeleteDiscountAsync(long id);

    // Query operations
    Task<DiscountListResponse> GetDiscountsAsync(DiscountQueryRequest query);
    Task<List<DiscountResponse>> GetActiveDiscountsByClinicAsync(long clinicId);
    Task<List<DiscountResponse>> GetApplicableDiscountsAsync(long clinicId, long? specialtyId = null, long? doctorId = null);

    // Validation and usage operations
    Task<DiscountValidationResponse> ValidateDiscountAsync(ValidateDiscountRequest request);
    Task<DiscountUsageResponse> UseDiscountAsync(UseDiscountRequest request);
    Task<bool> RevertDiscountUsageAsync(string code, long clinicId);

    // Administrative operations
    Task<bool> ActivateDiscountAsync(long id);
    Task<bool> DeactivateDiscountAsync(long id);
    Task<int> UpdateExpiredDiscountsAsync();
    Task<bool> IsDiscountValidAsync(string code, long clinicId, long? specialtyId = null, long? doctorId = null);

    // Calculation helpers
    Task<decimal> CalculateDiscountAmountAsync(string code, decimal originalAmount, long clinicId, long? specialtyId = null, long? doctorId = null);
}
