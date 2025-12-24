using BookingCare.Services.Discount.Models.DTOs;

namespace BookingCare.Services.Discount.Services;

public interface IDiscountService
{
    // Basic CRUD operations
    Task<DiscountResponse> CreateDiscountAsync(CreateDiscountRequest request);
    Task<DiscountResponse?> GetDiscountByIdAsync(Guid id);
    Task<DiscountResponse?> GetDiscountByCodeAsync(string code);
    Task<DiscountResponse> UpdateDiscountAsync(Guid id, UpdateDiscountRequest request);
    Task<bool> DeleteDiscountAsync(Guid id);

    // Query operations
    Task<DiscountListResponse> GetDiscountsAsync(DiscountQueryRequest query);
    Task<List<DiscountResponse>> GetActiveDiscountsByHospitalAsync(Guid hospitalId);
    Task<List<DiscountResponse>> GetApplicableDiscountsAsync(
        Guid hospitalId,
        Guid? specialtyId = null,
        Guid? doctorId = null
    );

    // Validation and usage operations
    Task<DiscountValidationResponse> ValidateDiscountAsync(ValidateDiscountRequest request);
    Task<DiscountUsageResponse> UseDiscountAsync(UseDiscountRequest request);
    Task<bool> RevertDiscountUsageAsync(string code, Guid hospitalId);

    // Administrative operations
    Task<bool> ActivateDiscountAsync(Guid id);
    Task<bool> DeactivateDiscountAsync(Guid id);
    Task<int> UpdateExpiredDiscountsAsync();
    Task<bool> IsDiscountValidAsync(
        string code,
        Guid hospitalId,
        Guid? specialtyId = null,
        Guid? doctorId = null
    );

    // Calculation helpers
    Task<decimal> CalculateDiscountAmountAsync(
        string code,
        decimal originalAmount,
        Guid hospitalId,
        Guid? specialtyId = null,
        Guid? doctorId = null
    );
}
