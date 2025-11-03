using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IHospitalSubscriptionRepository _hospitalSubscriptionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionPlanService> _logger;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IHospitalSubscriptionRepository hospitalSubscriptionRepository,
        IMapper mapper,
        ILogger<SubscriptionPlanService> logger)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _hospitalSubscriptionRepository = hospitalSubscriptionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SubscriptionPlanResponse?> GetByIdAsync(Guid id)
    {
        try
        {
            var plan = await _subscriptionPlanRepository.GetByIdAsync(id);
            return plan != null ? _mapper.Map<SubscriptionPlanResponse>(plan) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan with ID {PlanId}", id);
            throw new SubscriptionPlanOperationException("Failed to retrieve subscription plan", ex);
        }
    }

    public async Task<SubscriptionPlanResponse?> GetByNameAsync(string name)
    {
        try
        {
            var plan = await _subscriptionPlanRepository.GetByNameAsync(name);
            return plan != null ? _mapper.Map<SubscriptionPlanResponse>(plan) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan with name {PlanName}", name);
            throw new SubscriptionPlanOperationException("Failed to retrieve subscription plan", ex);
        }
    }

    public async Task<SubscriptionPlanListResponse> GetAllAsync()
    {
        try
        {
            var plans = await _subscriptionPlanRepository.GetAllAsync();
            var response = _mapper.Map<List<SubscriptionPlanResponse>>(plans);

            return new SubscriptionPlanListResponse
            {
                SubscriptionPlans = response,
                TotalCount = response.Count,
                Page = 1,
                PageSize = response.Count,
                TotalPages = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all subscription plans");
            throw new SubscriptionPlanOperationException("Failed to retrieve subscription plans", ex);
        }
    }

    public async Task<SubscriptionPlanListResponse> GetFilteredAsync(SubscriptionPlanFilterRequest filter)
    {
        try
        {
            _logger.LogInformation("GetFilteredAsync started with filter: Page={Page}, PageSize={PageSize}, SortBy={SortBy}, SortOrder={SortOrder}",
                filter.Page, filter.PageSize, filter.SortBy, filter.SortOrder);

            // Validate pagination parameters
            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            if (filter.PageSize < 1)
            {
                filter.PageSize = 10; // Default page size
            }

            if (filter.PageSize > 100)
            {
                filter.PageSize = 100; // Max page size
            }

            _logger.LogInformation("Fetching filtered plans from repository...");
            var (plans, totalCount) = await _subscriptionPlanRepository.GetFilteredAsync(filter);
            _logger.LogInformation("Retrieved {Count} plans from repository, totalCount={TotalCount}", plans.Count, totalCount);

            _logger.LogInformation("Mapping plans to response DTOs...");
            var response = _mapper.Map<List<SubscriptionPlanResponse>>(plans);
            _logger.LogInformation("Mapped {Count} plans to response DTOs", response.Count);

            var totalPages = filter.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / filter.PageSize)
                : 1;

            var result = new SubscriptionPlanListResponse
            {
                SubscriptionPlans = response,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };

            _logger.LogInformation("GetFilteredAsync completed successfully. Returning {Count} plans", response.Count);
            return result;
        }
        catch (AutoMapper.AutoMapperMappingException ex)
        {
            _logger.LogError(ex, "AutoMapper mapping error while retrieving filtered subscription plans. Filter: {@Filter}", filter);
            throw new SubscriptionPlanOperationException($"Mapping error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered subscription plans. Filter: {@Filter}, ErrorType: {ErrorType}, Message: {Message}",
                filter, ex.GetType().Name, ex.Message);
            throw new SubscriptionPlanOperationException($"Failed to retrieve filtered subscription plans: {ex.Message}", ex);
        }
    }

    public async Task<SubscriptionPlanDetailResponse> CreateAsync(CreateSubscriptionPlanRequest request)
    {
        try
        {
            // Business validation
            await ValidateSubscriptionPlanCreationAsync(request);

            var plan = _mapper.Map<SubscriptionPlanEntity>(request);

            // Convert -1 to null for unlimited
            plan.MaxDoctors = plan.MaxDoctors == -1 ? null : plan.MaxDoctors;
            plan.MaxSpecialties = plan.MaxSpecialties == -1 ? null : plan.MaxSpecialties;
            plan.MaxAppointments = plan.MaxAppointments == -1 ? null : plan.MaxAppointments;

            plan.Status = Status.ACTIVE;
            plan.CreatedAt = DateTime.Now;
            plan.UpdatedAt = DateTime.Now;

            var createdPlan = await _subscriptionPlanRepository.CreateAsync(plan);

            // Get active subscriptions count
            var activeSubscriptions = await _hospitalSubscriptionRepository
                .GetBySubscriptionIdAsync(createdPlan.Id);

            var detailResponse = _mapper.Map<SubscriptionPlanDetailResponse>(createdPlan);
            detailResponse.ActiveSubscriptionsCount = activeSubscriptions
                .Count(s => s.Status == Enums.SubscriptionStatus.ACTIVE);
            detailResponse.HospitalSubscriptions = _mapper.Map<List<HospitalSubscriptionResponse>>(activeSubscriptions);

            _logger.LogInformation("Created subscription plan {PlanName} with ID {PlanId}",
                createdPlan.Name, createdPlan.Id);

            return detailResponse;
        }
        catch (SubscriptionPlanAlreadyExistsException)
        {
            throw;
        }
        catch (InvalidSubscriptionPlanDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan {PlanName}", request.Name);
            throw new SubscriptionPlanOperationException("Failed to create subscription plan", ex);
        }
    }

    public async Task<SubscriptionPlanResponse> UpdateAsync(Guid id, UpdateSubscriptionPlanRequest request)
    {
        try
        {
            var existingPlan = await _subscriptionPlanRepository.GetByIdAsync(id);
            if (existingPlan == null)
            {
                throw new SubscriptionPlanNotFoundException(id);
            }

            // Business validation
            await ValidateSubscriptionPlanUpdateAsync(id, request);

            // Update properties
            _mapper.Map(request, existingPlan);

            // Xử lý không giới hạn: FE gửi null sẽ set null trong DB
            if (request.MaxDoctors == null) existingPlan.MaxDoctors = null;
            if (request.MaxSpecialties == null) existingPlan.MaxSpecialties = null;
            if (request.MaxAppointments == null) existingPlan.MaxAppointments = null;

            existingPlan.UpdatedAt = DateTime.Now;

            var updatedPlan = await _subscriptionPlanRepository.UpdateAsync(existingPlan);

            _logger.LogInformation("Updated subscription plan {PlanName} with ID {PlanId}",
                updatedPlan.Name, updatedPlan.Id);

            return _mapper.Map<SubscriptionPlanResponse>(updatedPlan);
        }
        catch (SubscriptionPlanNotFoundException)
        {
            throw;
        }
        catch (SubscriptionPlanAlreadyExistsException)
        {
            throw;
        }
        catch (InvalidSubscriptionPlanDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan with ID {PlanId}", id);
            throw new SubscriptionPlanOperationException("Failed to update subscription plan", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var plan = await _subscriptionPlanRepository.GetByIdAsync(id);
            if (plan == null)
            {
                return false;
            }

            // Check if plan is being used by any hospital
            var activeSubscriptions = await _hospitalSubscriptionRepository
                .GetBySubscriptionIdAsync(id);

            if (activeSubscriptions.Any(s => s.Status == Enums.SubscriptionStatus.ACTIVE))
            {
                throw new SubscriptionPlanOperationException(
                    "Cannot delete subscription plan that is currently active. Please cancel all active subscriptions first.");
            }

            var result = await _subscriptionPlanRepository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("Deleted subscription plan {PlanName} with ID {PlanId}",
                    plan.Name, plan.Id);
            }

            return result;
        }
        catch (SubscriptionPlanOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan with ID {PlanId}", id);
            throw new SubscriptionPlanOperationException("Failed to delete subscription plan", ex);
        }
    }

    public async Task<List<SubscriptionPlanResponse>> GetActiveAsync()
    {
        try
        {
            var activePlans = await _subscriptionPlanRepository.GetActiveAsync();
            return _mapper.Map<List<SubscriptionPlanResponse>>(activePlans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subscription plans");
            throw new SubscriptionPlanOperationException("Failed to retrieve active subscription plans", ex);
        }
    }

    private async Task ValidateSubscriptionPlanCreationAsync(CreateSubscriptionPlanRequest request)
    {
        // Check name and billing cycle combination uniqueness
        if (await _subscriptionPlanRepository.NameAndBillingCycleExistsAsync(request.Name, request.BillingCycle))
        {
            throw new SubscriptionPlanAlreadyExistsException($"{request.Name} ({request.BillingCycle})");
        }

        ValidateBillingCycle(request.BillingCycle);
        ValidateDescriptionLength(request.Description);
        ValidateFeaturesJson(request.Features);
        ValidateCreationLimits(request);
        ValidatePrice(request.Price);
        ValidateCreationName(request.Name);
    }

    private static void ValidateDescriptionLength(string? description)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 1000)
        {
            throw new InvalidSubscriptionPlanDataException("Description cannot exceed 1000 characters");
        }
    }

    private static void ValidateCreationLimits(CreateSubscriptionPlanRequest request)
    {
        if (request.MaxDoctors < 0)
        {
            throw new InvalidSubscriptionPlanDataException("Max doctors must be non-negative");
        }

        if (request.MaxSpecialties < 0)
        {
            throw new InvalidSubscriptionPlanDataException("Max specialties must be non-negative");
        }

        if (request.MaxAppointments < 0)
        {
            throw new InvalidSubscriptionPlanDataException("Max appointments must be non-negative");
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new InvalidSubscriptionPlanDataException("Price must be non-negative");
        }
    }

    private static void ValidateCreationName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidSubscriptionPlanDataException("Plan name is required");
        }

        if (name.Length < 2)
        {
            throw new InvalidSubscriptionPlanDataException("Plan name must be at least 2 characters");
        }

        if (name.Length > 100)
        {
            throw new InvalidSubscriptionPlanDataException("Plan name cannot exceed 100 characters");
        }
    }

    private async Task ValidateSubscriptionPlanUpdateAsync(Guid id, UpdateSubscriptionPlanRequest request)
    {
        await ValidateNameUniquenessAsync(id, request);

        if (!string.IsNullOrEmpty(request.BillingCycle))
        {
            ValidateBillingCycle(request.BillingCycle);
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 1000)
        {
            throw new InvalidSubscriptionPlanDataException("Description cannot exceed 1000 characters");
        }

        ValidateFeaturesJson(request.Features);
        ValidateLimits(request);

        if (request.Price.HasValue && request.Price.Value < 0)
        {
            throw new InvalidSubscriptionPlanDataException("Price must be non-negative");
        }

        ValidateNameLength(request.Name);
    }

    private async Task ValidateNameUniquenessAsync(Guid id, UpdateSubscriptionPlanRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return;
        }

        if (!string.IsNullOrEmpty(request.BillingCycle))
        {
            if (await _subscriptionPlanRepository.NameAndBillingCycleExistsAsync(request.Name, request.BillingCycle, id))
            {
                throw new SubscriptionPlanAlreadyExistsException($"{request.Name} ({request.BillingCycle})");
            }
            return;
        }

        var existingPlan = await _subscriptionPlanRepository.GetByIdAsync(id);
        if (existingPlan != null &&
            await _subscriptionPlanRepository.NameAndBillingCycleExistsAsync(request.Name, existingPlan.BillingCycle, id))
        {
            throw new SubscriptionPlanAlreadyExistsException($"{request.Name} ({existingPlan.BillingCycle})");
        }
    }

    private static void ValidateFeaturesJson(string? features)
    {
        if (string.IsNullOrWhiteSpace(features))
        {
            return;
        }

        if (features.Length > 5000)
        {
            throw new InvalidSubscriptionPlanDataException("Features JSON cannot exceed 5000 characters");
        }

        try
        {
            var featuresArray = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(features);
            if (featuresArray.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                throw new InvalidSubscriptionPlanDataException("Features must be a valid JSON array");
            }
        }
        catch (System.Text.Json.JsonException)
        {
            throw new InvalidSubscriptionPlanDataException("Features must be a valid JSON format");
        }
    }

    private static void ValidateLimits(UpdateSubscriptionPlanRequest request)
    {
        if (request.MaxDoctors.HasValue && request.MaxDoctors.Value < -1)
        {
            throw new InvalidSubscriptionPlanDataException("Max doctors value is invalid (use -1 for unlimited)");
        }

        if (request.MaxSpecialties.HasValue && request.MaxSpecialties.Value < -1)
        {
            throw new InvalidSubscriptionPlanDataException("Max specialties value is invalid (use -1 for unlimited)");
        }

        if (request.MaxAppointments.HasValue && request.MaxAppointments.Value < -1)
        {
            throw new InvalidSubscriptionPlanDataException("Max appointments value is invalid (use -1 for unlimited)");
        }
    }

    private static void ValidateNameLength(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (name.Length < 2)
        {
            throw new InvalidSubscriptionPlanDataException("Plan name must be at least 2 characters");
        }

        if (name.Length > 100)
        {
            throw new InvalidSubscriptionPlanDataException("Plan name cannot exceed 100 characters");
        }
    }

    private static void ValidateBillingCycle(string billingCycle)
    {
        if (!IsValidBillingCycle(billingCycle))
        {
            throw new InvalidSubscriptionPlanDataException(
                $"Invalid billing cycle: {billingCycle}. Must be MONTHLY, QUARTERLY, or YEARLY");
        }
    }

    private static bool IsValidBillingCycle(string billingCycle)
    {
        return billingCycle?.ToUpper() switch
        {
            "MONTHLY" => true,
            "QUARTERLY" => true,
            "YEARLY" => true,
            _ => false
        };
    }
}
