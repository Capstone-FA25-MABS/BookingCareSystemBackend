using AutoMapper;
using BookingCare.Services.Discount.Exceptions;
using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Models.Entities;
using BookingCare.Services.Discount.Repositories;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Discount.Services;

public class DiscountService : BaseService, IDiscountService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IMapper _mapper;

    public DiscountService(
        IDiscountRepository discountRepository, 
        IMapper mapper, 
        ILogger<DiscountService> logger) : base(logger)
    {
        _discountRepository = discountRepository;
        _mapper = mapper;
    }

    public async Task<DiscountResponse> CreateDiscountAsync(CreateDiscountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating discount with code: {Code}", null, request.Code);

            // Validate business rules
            ValidateCreateDiscountRequest(request);

            // Check if code already exists
            if (await _discountRepository.CodeExistsAsync(request.Code))
            {
                throw new DiscountBusinessException($"Discount code '{request.Code}' already exists");
            }

            // Validate dates
            if (request.StartDate >= request.EndDate)
            {
                throw new DiscountValidationException(new List<ValidationError>
                {
                    new("StartDate", "Start date must be before end date", request.StartDate),
                    new("EndDate", "End date must be after start date", request.EndDate)
                });
            }

            if (request.EndDate <= DateTime.UtcNow)
            {
                throw new DiscountValidationException(new List<ValidationError>
                {
                    new("EndDate", "End date must be in the future", request.EndDate)
                });
            }

            // Validate percentage discount
            if (request.DiscountType == "PERCENTAGE" && request.Amount > 100)
            {
                throw new DiscountValidationException(new List<ValidationError>
                {
                    new("Amount", "Percentage discount cannot exceed 100%", request.Amount)
                });
            }

            var discountEntity = _mapper.Map<DiscountEntity>(request);
            var createdDiscount = await _discountRepository.CreateAsync(discountEntity);

            LogInfo("Discount created successfully with ID: {Id}", null, createdDiscount.Id);
            return _mapper.Map<DiscountResponse>(createdDiscount);
        }, "CreateDiscount");
    }

    public async Task<DiscountResponse?> GetDiscountByIdAsync(long id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        return discount != null ? _mapper.Map<DiscountResponse>(discount) : null;
    }

    public async Task<DiscountResponse?> GetDiscountByCodeAsync(string code)
    {
        var discount = await _discountRepository.GetByCodeAsync(code);
        return discount != null ? _mapper.Map<DiscountResponse>(discount) : null;
    }

    public async Task<DiscountResponse> UpdateDiscountAsync(UpdateDiscountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating discount with ID: {Id}", null, request.Id);
            ValidateRequired(request, nameof(request));

            var existingDiscount = await _discountRepository.GetByIdAsync(request.Id);
            if (existingDiscount == null)
            {
                throw new DiscountNotFoundException(request.Id);
            }

            // Validate dates if provided
            var startDate = request.StartDate ?? existingDiscount.StartDate;
            var endDate = request.EndDate ?? existingDiscount.EndDate;

            if (startDate >= endDate)
            {
                throw new DiscountValidationException(new List<ValidationError>
                {
                    new("StartDate", "Start date must be before end date", startDate),
                    new("EndDate", "End date must be after start date", endDate)
                });
            }

            // Validate amount if provided
            if (request.Amount.HasValue)
            {
                if (existingDiscount.DiscountType == "PERCENTAGE" && request.Amount > 100)
                {
                    throw new DiscountValidationException(new List<ValidationError>
                    {
                        new("Amount", "Percentage discount cannot exceed 100%", request.Amount)
                    });
                }
            }

            // Apply updates
            _mapper.Map(request, existingDiscount);
            var updatedDiscount = await _discountRepository.UpdateAsync(existingDiscount);

            LogInfo("Discount updated successfully with ID: {Id}", null, updatedDiscount.Id);
            return _mapper.Map<DiscountResponse>(updatedDiscount);
        }, "UpdateDiscount");
    }

    public async Task<bool> DeleteDiscountAsync(long id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting discount with ID: {Id}", null, id);

            var discount = await _discountRepository.GetByIdAsync(id);
            if (discount == null)
            {
                throw new DiscountNotFoundException(id);
            }

            // Check if discount has been used
            if (discount.UsesCount > 0)
            {
                throw new DiscountBusinessException("Cannot delete discount that has been used");
            }

            var result = await _discountRepository.DeleteAsync(id);
            
            if (result)
            {
                LogInfo("Discount deleted successfully with ID: {Id}", null, id);
            }

            return result;
        }, "DeleteDiscount");
    }

    public async Task<DiscountListResponse> GetDiscountsAsync(DiscountQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequired(query, "Query request is required");
            
            LogInfo("Getting discounts - Page: {Page}, PageSize: {PageSize}", null, query.PageNumber, query.PageSize);
            
            var (discounts, totalCount) = await _discountRepository.GetDiscountsAsync(query);
            
            var response = _mapper.Map<DiscountListResponse>((discounts, totalCount));
            response.PageNumber = query.PageNumber;
            response.PageSize = query.PageSize;
            response.TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            return response;
        }, "GetDiscounts");
    }

    public async Task<List<DiscountResponse>> GetActiveDiscountsByClinicAsync(long clinicId)
    {
        var discounts = await _discountRepository.GetActiveDiscountsByClinicAsync(clinicId);
        return _mapper.Map<List<DiscountResponse>>(discounts);
    }

    public async Task<List<DiscountResponse>> GetApplicableDiscountsAsync(long clinicId, long? specialtyId = null, long? doctorId = null)
    {
        var discounts = await _discountRepository.GetApplicableDiscountsAsync(clinicId, specialtyId, doctorId);
        return _mapper.Map<List<DiscountResponse>>(discounts);
    }

    public async Task<DiscountValidationResponse> ValidateDiscountAsync(ValidateDiscountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Validating discount code: {Code}", null, request.Code);
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Code, nameof(request.Code));

            var discount = await _discountRepository.GetValidDiscountAsync(
                request.Code, 
                request.ClinicId, 
                request.SpecialtyId, 
                request.DoctorId);

            if (discount == null)
            {
                return new DiscountValidationResponse
                {
                    IsValid = false,
                    Message = "Discount code is not valid or has expired"
                };
            }

            var discountAmount = CalculateDiscountAmount(discount, request.TotalAmount);
            var finalAmount = request.TotalAmount - discountAmount;

            return new DiscountValidationResponse
            {
                IsValid = true,
                Message = "Discount is valid",
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                Discount = _mapper.Map<DiscountResponse>(discount)
            };
        }, "ValidateDiscount");
    }

    public async Task<DiscountUsageResponse> UseDiscountAsync(UseDiscountRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Using discount code: {Code}", null, request.Code);
            ValidateRequired(request, nameof(request));
            ValidateRequiredString(request.Code, nameof(request.Code));

            var discount = await _discountRepository.GetValidDiscountAsync(
                request.Code, 
                request.ClinicId, 
                request.SpecialtyId, 
                request.DoctorId);

            if (discount == null)
            {
                return new DiscountUsageResponse
                {
                    Success = false,
                    Message = "Discount code is not valid or has expired"
                };
            }

            // Check if usage limit exceeded
            if (discount.MaxUses.HasValue && discount.UsesCount >= discount.MaxUses.Value)
            {
                return new DiscountUsageResponse
                {
                    Success = false,
                    Message = "Discount usage limit has been reached"
                };
            }

            var discountAmount = CalculateDiscountAmount(discount, request.TotalAmount);
            var finalAmount = request.TotalAmount - discountAmount;

            // Increment usage count
            await _discountRepository.IncrementUsageAsync(discount.Id);

            var remainingUses = discount.MaxUses.HasValue 
                ? Math.Max(0, discount.MaxUses.Value - discount.UsesCount - 1)
                : int.MaxValue;

            LogInfo("Discount used successfully. ID: {Id}, Remaining uses: {RemainingUses}", 
                null, discount.Id, remainingUses);

            return new DiscountUsageResponse
            {
                Success = true,
                Message = "Discount applied successfully",
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                DiscountId = discount.Id,
                RemainingUses = remainingUses
            };
        }, "UseDiscount");
    }

    public async Task<bool> RevertDiscountUsageAsync(string code, long clinicId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Reverting discount usage for code: {Code}", null, code);
            ValidateRequiredString(code, nameof(code));

            var discount = await _discountRepository.GetByCodeAsync(code);
            if (discount == null || discount.ClinicId != clinicId)
            {
                return false;
            }

            var result = await _discountRepository.DecrementUsageAsync(discount.Id);
            
            if (result)
            {
                LogInfo("Discount usage reverted successfully for ID: {Id}", null, discount.Id);
            }

            return result;
        }, "RevertDiscountUsage");
    }

    public async Task<bool> ActivateDiscountAsync(long id)
    {
        return await _discountRepository.UpdateStatusAsync(id, "ACTIVE");
    }

    public async Task<bool> DeactivateDiscountAsync(long id)
    {
        return await _discountRepository.UpdateStatusAsync(id, "INACTIVE");
    }

    public async Task<int> UpdateExpiredDiscountsAsync()
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating expired discounts");
            var count = await _discountRepository.UpdateExpiredDiscountsAsync();
            LogInfo("Updated {Count} expired discounts", null, count);
            return count;
        }, "UpdateExpiredDiscounts");
    }

    public async Task<bool> IsDiscountValidAsync(string code, long clinicId, long? specialtyId = null, long? doctorId = null)
    {
        var discount = await _discountRepository.GetValidDiscountAsync(code, clinicId, specialtyId, doctorId);
        return discount != null;
    }

    public async Task<decimal> CalculateDiscountAmountAsync(string code, decimal originalAmount, long clinicId, long? specialtyId = null, long? doctorId = null)
    {
        var discount = await _discountRepository.GetValidDiscountAsync(code, clinicId, specialtyId, doctorId);
        if (discount == null) return 0;

        return CalculateDiscountAmount(discount, originalAmount);
    }

    private static decimal CalculateDiscountAmount(DiscountEntity discount, decimal originalAmount)
    {
        return discount.DiscountType switch
        {
            "PERCENTAGE" => originalAmount * (discount.Amount / 100),
            "FIXED_AMOUNT" => Math.Min(discount.Amount, originalAmount),
            _ => 0
        };
    }

    private static void ValidateCreateDiscountRequest(CreateDiscountRequest request)
    {
        // Validate applicable_to and related fields
        switch (request.ApplicableTo)
        {
            case "SPECIALTY":
                if (!request.SpecialtyId.HasValue)
                {
                    throw new DiscountValidationException("SpecialtyId is required when ApplicableTo is SPECIALTY");
                }
                break;
            case "DOCTOR":
                if (!request.DoctorId.HasValue)
                {
                    throw new DiscountValidationException("DoctorId is required when ApplicableTo is DOCTOR");
                }
                break;
            case "ALL":
                // No additional validation needed
                break;
            default:
                throw new DiscountValidationException("ApplicableTo must be one of: ALL, SPECIALTY, DOCTOR");
        }

        // Validate discount type
        if (request.DiscountType != "PERCENTAGE" && request.DiscountType != "FIXED_AMOUNT")
        {
            throw new DiscountValidationException("DiscountType must be either PERCENTAGE or FIXED_AMOUNT");
        }

        // Validate status
        if (request.Status != "ACTIVE" && request.Status != "INACTIVE")
        {
            throw new DiscountValidationException("Status must be either ACTIVE or INACTIVE");
        }
    }
}
