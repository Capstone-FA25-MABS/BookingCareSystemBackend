using AutoMapper;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalSubscriptionService : IHospitalSubscriptionService
{
    private readonly IHospitalSubscriptionRepository _hospitalSubscriptionRepository;
    private readonly IHospitalRepository _hospitalRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly ILogger<HospitalSubscriptionService> _logger;

    public HospitalSubscriptionService(
        IHospitalSubscriptionRepository hospitalSubscriptionRepository,
        IHospitalRepository hospitalRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IMapper mapper,
        IEventBus eventBus,
        ILogger<HospitalSubscriptionService> logger
    )
    {
        _hospitalSubscriptionRepository = hospitalSubscriptionRepository;
        _hospitalRepository = hospitalRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _mapper = mapper;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<HospitalSubscriptionResponse?> GetByIdAsync(Guid id)
    {
        var subscription = await _hospitalSubscriptionRepository.GetByIdAsync(id);
        if (subscription != null)
        {
            // Update status based on current date
            await UpdateSubscriptionStatusAsync(subscription);
        }
        return subscription != null
            ? _mapper.Map<HospitalSubscriptionResponse>(subscription)
            : null;
    }

    public async Task<HospitalSubscriptionResponse?> GetActiveByHospitalIdAsync(Guid hospitalId)
    {
        try
        {
            // First check if hospital exists
            var hospitalExists = await _hospitalRepository.ExistsAsync(hospitalId);
            if (!hospitalExists)
            {
                throw new HospitalNotFoundException(hospitalId);
            }

            var subscription = await _hospitalSubscriptionRepository.GetActiveByHospitalIdAsync(
                hospitalId
            );
            if (subscription == null)
            {
                return null;
            }

            // Validate that related entities exist (should not be null due to Include, but check for safety)
            if (subscription.Hospital == null)
            {
                // Hospital exists but subscription's Hospital navigation property is null
                // This might indicate a data inconsistency, but we'll proceed with mapping
                // AutoMapper should handle null gracefully if configured properly
            }

            if (subscription.SubscriptionPlan == null)
            {
                throw new SubscriptionPlanNotFoundException(subscription.SubscriptionId);
            }

            // Update status based on current date
            try
            {
                await UpdateSubscriptionStatusAsync(subscription);
            }
            catch (Exception)
            {
                // Log but don't fail if status update fails
                // Status update is not critical for retrieval
            }

            var result = _mapper.Map<HospitalSubscriptionResponse>(subscription);

            // Break circular reference: set Hospital to null to avoid serialization issues
            // The hospitalId is already available in the response, so we don't need the full Hospital object
            if (result != null)
            {
                result.Hospital = null;
            }

            return result;
        }
        catch (HospitalNotFoundException)
        {
            throw;
        }
        catch (SubscriptionPlanNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException(
                $"Failed to retrieve active subscription for hospital {hospitalId}",
                ex
            );
        }
    }

    public async Task<List<HospitalSubscriptionResponse>> GetByHospitalIdAsync(Guid hospitalId)
    {
        var subscriptions = await _hospitalSubscriptionRepository.GetByHospitalIdAsync(hospitalId);

        // Update statuses based on current date
        foreach (var subscription in subscriptions)
        {
            await UpdateSubscriptionStatusAsync(subscription);
        }

        return _mapper.Map<List<HospitalSubscriptionResponse>>(subscriptions);
    }

    public async Task<HospitalSubscriptionResponse> CreateAsync(
        CreateHospitalSubscriptionRequest request
    )
    {
        // Enhanced validation
        await ValidateHospitalSubscriptionCreationAsync(request);

        // Check for existing active subscription
        var existingActive = await _hospitalSubscriptionRepository.GetActiveByHospitalIdAsync(
            request.HospitalId
        );

        if (existingActive != null)
        {
            throw new HospitalOperationException(
                "Hospital already has an active subscription. Please cancel existing subscription first or use upgrade functionality."
            );
        }

        var subscription = _mapper.Map<HospitalSubscriptionEntity>(request);

        // Always use today's date and current time for new subscriptions
        // This ensures start date is always the current date regardless of what frontend sends
        var now = DateTime.Now;

        // Get subscription plan to determine billing cycle
        var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(
            request.SubscriptionId
        );
        if (subscriptionPlan == null)
        {
            throw new SubscriptionPlanNotFoundException(request.SubscriptionId);
        }

        // Set StartDate to today with current time
        subscription.StartDate = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second,
            now.Millisecond,
            DateTimeKind.Utc
        );

        // Calculate EndDate based on billing cycle months (not days to avoid timezone issues)
        var billingCycleMonths = GetBillingCycleMonths(subscriptionPlan.BillingCycle);
        subscription.EndDate = subscription.StartDate.AddMonths(billingCycleMonths);

        // Set initial status based on dates
        subscription.Status = DetermineInitialStatus(subscription.StartDate, subscription.EndDate);

        try
        {
            var createdSubscription = await _hospitalSubscriptionRepository.CreateAsync(
                subscription
            );
            var response = _mapper.Map<HospitalSubscriptionResponse>(createdSubscription);

            // Publish event for email notification
            try
            {
                // Get hospital information for email
                var hospital = await _hospitalRepository.GetByIdAsync(request.HospitalId);
                if (hospital != null && !string.IsNullOrWhiteSpace(hospital.Email))
                {
                    var subscriptionCreatedEvent = new HospitalSubscriptionCreatedEvent
                    {
                        HospitalSubscriptionId = createdSubscription.HospitalSubscriptionId,
                        HospitalId = createdSubscription.HospitalId,
                        HospitalName = hospital.Name,
                        HospitalEmail = hospital.Email,
                        ContactPersonName = "Quý bệnh viện",
                        SubscriptionPlanId = subscriptionPlan.Id,
                        PlanName = subscriptionPlan.Name,
                        BillingCycle = subscriptionPlan.BillingCycle,
                        Price = subscriptionPlan.Price,
                        StartDate = createdSubscription.StartDate,
                        EndDate = createdSubscription.EndDate,
                        MaxDoctors = subscriptionPlan.MaxDoctors,
                        MaxAppointmentsPerMonth = subscriptionPlan.MaxAppointments,
                        Features = subscriptionPlan.Features,
                        CreatedAt = createdSubscription.CreatedAt,
                    };

                    await _eventBus.PublishAsync(subscriptionCreatedEvent);
                    _logger.LogInformation(
                        "Published HospitalSubscriptionCreatedEvent for HospitalId: {HospitalId}, SubscriptionId: {SubscriptionId}",
                        createdSubscription.HospitalId,
                        createdSubscription.HospitalSubscriptionId
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the subscription creation
                _logger.LogError(
                    ex,
                    "Failed to publish HospitalSubscriptionCreatedEvent for HospitalId: {HospitalId}",
                    request.HospitalId
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to create hospital subscription", ex);
        }
    }

    public async Task<HospitalSubscriptionResponse> UpdateAsync(
        Guid id,
        UpdateHospitalSubscriptionRequest request
    )
    {
        var existingSubscription = await _hospitalSubscriptionRepository.GetByIdAsync(id);
        if (existingSubscription == null)
        {
            throw new HospitalOperationException($"Hospital subscription with ID {id} not found");
        }

        // Update properties
        _mapper.Map(request, existingSubscription);

        // Update status if dates changed or status was explicitly set
        if (request.StartDate.HasValue || request.EndDate.HasValue)
        {
            existingSubscription.Status = DetermineInitialStatus(
                existingSubscription.StartDate,
                existingSubscription.EndDate
            );
        }

        try
        {
            var updatedSubscription = await _hospitalSubscriptionRepository.UpdateAsync(
                existingSubscription
            );
            return _mapper.Map<HospitalSubscriptionResponse>(updatedSubscription);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to update hospital subscription", ex);
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid id, string cancellationReason = "")
    {
        var subscription = await _hospitalSubscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            return false;
        }

        subscription.Status = SubscriptionStatus.CANCELLED;

        try
        {
            await _hospitalSubscriptionRepository.UpdateAsync(subscription);
            return true;
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to cancel subscription", ex);
        }
    }

    public async Task<List<HospitalSubscriptionResponse>> GetExpiringSoonAsync(int days = 30)
    {
        var subscriptions = await _hospitalSubscriptionRepository.GetExpiringSoonAsync(days);

        // Update statuses based on current date
        foreach (var subscription in subscriptions)
        {
            await UpdateSubscriptionStatusAsync(subscription);
        }

        return _mapper.Map<List<HospitalSubscriptionResponse>>(subscriptions);
    }

    private async Task UpdateSubscriptionStatusAsync(HospitalSubscriptionEntity subscription)
    {
        var currentStatus = subscription.Status;
        var newStatus = DetermineCurrentStatus(
            subscription.StartDate,
            subscription.EndDate,
            subscription.Status
        );

        if (currentStatus != newStatus)
        {
            subscription.Status = newStatus;
            await _hospitalSubscriptionRepository.UpdateAsync(subscription);
        }
    }

    private static SubscriptionStatus DetermineInitialStatus(DateTime startDate, DateTime endDate)
    {
        var now = DateTime.Now;

        if (startDate > now)
        {
            return SubscriptionStatus.PENDING;
        }

        if (endDate < now)
        {
            return SubscriptionStatus.EXPIRED;
        }

        return SubscriptionStatus.ACTIVE;
    }

    private SubscriptionStatus DetermineCurrentStatus(
        DateTime startDate,
        DateTime endDate,
        SubscriptionStatus currentStatus
    )
    {
        var now = DateTime.Now;

        // Don't change manually set statuses like CANCELLED or TRIAL
        if (
            currentStatus == SubscriptionStatus.CANCELLED
            || currentStatus == SubscriptionStatus.TRIAL
        )
        {
            // But check if trial has expired
            if (currentStatus == SubscriptionStatus.TRIAL && endDate < now)
            {
                return SubscriptionStatus.EXPIRED;
            }
            return currentStatus;
        }

        // Auto-update based on dates
        if (endDate < now)
        {
            return SubscriptionStatus.EXPIRED;
        }

        if (startDate > now)
        {
            return SubscriptionStatus.PENDING;
        }

        return SubscriptionStatus.ACTIVE;
    }

    public async Task<HospitalSubscriptionResponse> UpgradeSubscriptionAsync(
        Guid currentSubscriptionId,
        Guid newSubscriptionPlanId
    )
    {
        var currentSubscription = await _hospitalSubscriptionRepository.GetByIdAsync(
            currentSubscriptionId
        );
        if (currentSubscription == null)
        {
            throw new HospitalOperationException("Current subscription not found");
        }

        var newPlan = await _subscriptionPlanRepository.GetByIdAsync(newSubscriptionPlanId);
        if (newPlan == null)
        {
            throw new SubscriptionPlanNotFoundException(newSubscriptionPlanId);
        }

        var currentPlan = await _subscriptionPlanRepository.GetByIdAsync(
            currentSubscription.SubscriptionId
        );
        if (currentPlan == null)
        {
            throw new SubscriptionPlanNotFoundException(currentSubscription.SubscriptionId);
        }

        await ValidateUpgradeRequestAsync(currentSubscription, currentPlan, newPlan);

        var now = DateTime.Now;
        var additionalDaysForNewPlan = CalculateBonusDaysForUpgrade(
            currentSubscription,
            currentPlan,
            newPlan,
            now
        );

        var newSubscriptionEndDate = CalculateNewSubscriptionEndDate(
            newPlan,
            additionalDaysForNewPlan,
            now
        );

        ValidateSubscriptionEndDate(newSubscriptionEndDate, newPlan, additionalDaysForNewPlan, now);

        currentSubscription.Status = SubscriptionStatus.CANCELLED;
        currentSubscription.UpdatedAt = now;

        var newSubscription = new HospitalSubscriptionEntity
        {
            HospitalId = currentSubscription.HospitalId,
            SubscriptionId = newSubscriptionPlanId,
            StartDate = now,
            EndDate = newSubscriptionEndDate,
            Status = SubscriptionStatus.ACTIVE,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await _hospitalSubscriptionRepository.UpdateAsync(currentSubscription);
            var createdSubscription = await _hospitalSubscriptionRepository.CreateAsync(
                newSubscription
            );
            var response = _mapper.Map<HospitalSubscriptionResponse>(createdSubscription);

            await PublishUpgradeEventAsync(
                createdSubscription,
                currentSubscription,
                currentPlan,
                newPlan,
                additionalDaysForNewPlan,
                now
            );

            return response;
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to upgrade subscription", ex);
        }
    }

    private async Task ValidateUpgradeRequestAsync(
        HospitalSubscriptionEntity currentSubscription,
        SubscriptionPlanEntity currentPlan,
        SubscriptionPlanEntity newPlan
    )
    {
        if (currentSubscription.SubscriptionId == newPlan.Id)
        {
            throw new HospitalOperationException("Cannot upgrade to the same subscription plan");
        }

        if (newPlan.Status != Status.ACTIVE)
        {
            throw new HospitalOperationException("Target subscription plan is not active");
        }

        var currentBillingCycleValue = GetBillingCycleValue(currentPlan.BillingCycle);
        var newBillingCycleValue = GetBillingCycleValue(newPlan.BillingCycle);

        if (currentBillingCycleValue > newBillingCycleValue)
        {
            throw new HospitalOperationException(
                $"Không thể chuyển từ gói {GetBillingCycleDisplayName(currentPlan.BillingCycle)} xuống gói {GetBillingCycleDisplayName(newPlan.BillingCycle)}. "
                    + $"Vui lòng đợi gói hiện tại hết hạn trước khi đăng ký gói mới."
            );
        }

        if (currentBillingCycleValue == newBillingCycleValue && currentPlan.Price > newPlan.Price)
        {
            throw new HospitalOperationException(
                $"Không thể chuyển từ gói {currentPlan.Name} ({GetBillingCycleDisplayName(currentPlan.BillingCycle)}) "
                    + $"xuống gói {newPlan.Name} ({GetBillingCycleDisplayName(newPlan.BillingCycle)}). "
                    + $"Vui lòng đợi gói hiện tại hết hạn trước khi đăng ký gói mới."
            );
        }
    }

    private double CalculateBonusDaysForUpgrade(
        HospitalSubscriptionEntity currentSubscription,
        SubscriptionPlanEntity currentPlan,
        SubscriptionPlanEntity newPlan,
        DateTime now
    )
    {
        if (currentSubscription.EndDate <= now)
        {
            return 0.0;
        }

        var timeSpan = currentSubscription.EndDate - now;
        var remainingDaysDecimal = timeSpan.TotalDays;

        if (remainingDaysDecimal <= 0)
        {
            return 0.0;
        }

        var billingCycleDaysForCurrentPlan = GetBillingCycleDays(currentPlan.BillingCycle);
        if (billingCycleDaysForCurrentPlan <= 0)
        {
            return 0.0;
        }

        var dailyRateForCurrentPlan = currentPlan.Price / billingCycleDaysForCurrentPlan;
        var remainingValue = (decimal)dailyRateForCurrentPlan * (decimal)remainingDaysDecimal;

        var billingCycleDaysForNewPlan = GetBillingCycleDays(newPlan.BillingCycle);
        var dailyRateForNewPlan =
            billingCycleDaysForNewPlan > 0 ? newPlan.Price / billingCycleDaysForNewPlan : 0;

        if (dailyRateForNewPlan <= 0)
        {
            return 0.0;
        }

        var calculatedDays = (double)(remainingValue / dailyRateForNewPlan);
        var additionalDaysForNewPlan = Math.Ceiling(calculatedDays);

        return additionalDaysForNewPlan < 0 ? 0.0 : additionalDaysForNewPlan;
    }

    private DateTime CalculateNewSubscriptionEndDate(
        SubscriptionPlanEntity newPlan,
        double additionalDaysForNewPlan,
        DateTime now
    )
    {
        var billingCycleMonths = GetBillingCycleMonths(newPlan.BillingCycle);
        var newSubscriptionEndDate = now.AddMonths(billingCycleMonths);

        if (additionalDaysForNewPlan > 0)
        {
            newSubscriptionEndDate = newSubscriptionEndDate.AddDays(additionalDaysForNewPlan);
        }

        return newSubscriptionEndDate;
    }

    private void ValidateSubscriptionEndDate(
        DateTime newSubscriptionEndDate,
        SubscriptionPlanEntity newPlan,
        double additionalDaysForNewPlan,
        DateTime now
    )
    {
        var billingCycleMonths = GetBillingCycleMonths(newPlan.BillingCycle);
        var actualDays = (newSubscriptionEndDate - now).TotalDays;
        var expectedMinDays = billingCycleMonths switch
        {
            3 => 85, // QUARTERLY: at least ~85 days
            12 => 360, // YEARLY: at least ~360 days
            _ =>
                25 // MONTHLY: at least ~25 days
            ,
        };

        if (actualDays < expectedMinDays)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WARNING: Calculated subscription duration seems incorrect. "
                    + $"Start: {now:yyyy-MM-dd HH:mm:ss}, End: {newSubscriptionEndDate:yyyy-MM-dd HH:mm:ss}, "
                    + $"Days: {actualDays:F2}, Expected min: {expectedMinDays}, "
                    + $"BillingCycle: {newPlan.BillingCycle}, BonusDays: {additionalDaysForNewPlan}"
            );
        }
    }

    private async Task PublishUpgradeEventAsync(
        HospitalSubscriptionEntity createdSubscription,
        HospitalSubscriptionEntity currentSubscription,
        SubscriptionPlanEntity currentPlan,
        SubscriptionPlanEntity newPlan,
        double additionalDaysForNewPlan,
        DateTime now
    )
    {
        try
        {
            var hospital = await _hospitalRepository.GetByIdAsync(currentSubscription.HospitalId);
            if (hospital == null || string.IsNullOrWhiteSpace(hospital.Email))
            {
                return;
            }

            var subscriptionUpgradedEvent = new HospitalSubscriptionUpgradedEvent
            {
                NewHospitalSubscriptionId = createdSubscription.HospitalSubscriptionId,
                PreviousHospitalSubscriptionId = currentSubscription.HospitalSubscriptionId,
                HospitalId = createdSubscription.HospitalId,
                HospitalName = hospital.Name,
                HospitalEmail = hospital.Email,
                ContactPersonName = "Quý bệnh viện",
                PreviousPlanName = currentPlan.Name,
                PreviousBillingCycle = currentPlan.BillingCycle,
                PreviousPrice = currentPlan.Price,
                NewSubscriptionPlanId = newPlan.Id,
                NewPlanName = newPlan.Name,
                NewBillingCycle = newPlan.BillingCycle,
                NewPrice = newPlan.Price,
                NewStartDate = createdSubscription.StartDate,
                NewEndDate = createdSubscription.EndDate,
                BonusDays = additionalDaysForNewPlan,
                NewMaxDoctors = newPlan.MaxDoctors,
                NewMaxAppointmentsPerMonth = newPlan.MaxAppointments,
                NewFeatures = newPlan.Features,
                UpgradedAt = now,
            };

            await _eventBus.PublishAsync(subscriptionUpgradedEvent);
            _logger.LogInformation(
                "Published HospitalSubscriptionUpgradedEvent for HospitalId: {HospitalId}, NewSubscriptionId: {NewSubscriptionId}",
                createdSubscription.HospitalId,
                createdSubscription.HospitalSubscriptionId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish HospitalSubscriptionUpgradedEvent for HospitalId: {HospitalId}",
                currentSubscription.HospitalId
            );
        }
    }

    public async Task<HospitalSubscriptionResponse> ExtendSubscriptionAsync(
        Guid subscriptionId,
        int additionalMonths
    )
    {
        var subscription = await _hospitalSubscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            throw new HospitalOperationException("Subscription not found");
        }

        if (
            subscription.Status != SubscriptionStatus.ACTIVE
            && subscription.Status != SubscriptionStatus.TRIAL
        )
        {
            throw new HospitalOperationException("Can only extend active or trial subscriptions");
        }

        subscription.EndDate = subscription.EndDate.AddMonths(additionalMonths);
        subscription.UpdatedAt = DateTime.Now;

        try
        {
            var updatedSubscription = await _hospitalSubscriptionRepository.UpdateAsync(
                subscription
            );
            return _mapper.Map<HospitalSubscriptionResponse>(updatedSubscription);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to extend subscription", ex);
        }
    }

    public async Task<HospitalSubscriptionResponse> ConvertTrialToPaidAsync(
        Guid trialSubscriptionId,
        Guid paidSubscriptionPlanId
    )
    {
        var trialSubscription = await _hospitalSubscriptionRepository.GetByIdAsync(
            trialSubscriptionId
        );
        if (trialSubscription == null)
        {
            throw new HospitalOperationException("Trial subscription not found");
        }

        if (trialSubscription.Status != SubscriptionStatus.TRIAL)
        {
            throw new HospitalOperationException("Subscription is not in trial status");
        }

        var paidPlan = await _subscriptionPlanRepository.GetByIdAsync(paidSubscriptionPlanId);
        if (paidPlan == null)
        {
            throw new SubscriptionPlanNotFoundException(paidSubscriptionPlanId);
        }

        // Cancel trial subscription
        trialSubscription.Status = SubscriptionStatus.CANCELLED;
        trialSubscription.UpdatedAt = DateTime.Now;

        // Create paid subscription
        var paidSubscription = new HospitalSubscriptionEntity
        {
            HospitalId = trialSubscription.HospitalId,
            SubscriptionId = paidSubscriptionPlanId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(GetBillingCycleMonths(paidPlan.BillingCycle)),
            Status = SubscriptionStatus.ACTIVE,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        try
        {
            await _hospitalSubscriptionRepository.UpdateAsync(trialSubscription);
            var createdSubscription = await _hospitalSubscriptionRepository.CreateAsync(
                paidSubscription
            );

            return _mapper.Map<HospitalSubscriptionResponse>(createdSubscription);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException(
                "Failed to convert trial to paid subscription",
                ex
            );
        }
    }

    private async Task ValidateHospitalSubscriptionCreationAsync(
        CreateHospitalSubscriptionRequest request
    )
    {
        // Validate hospital exists
        if (!await _hospitalRepository.ExistsAsync(request.HospitalId))
        {
            throw new HospitalNotFoundException(request.HospitalId);
        }

        // Validate subscription plan exists
        if (!await _subscriptionPlanRepository.ExistsAsync(request.SubscriptionId))
        {
            throw new SubscriptionPlanNotFoundException(request.SubscriptionId);
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            throw new HospitalOperationException("End date must be after start date");
        }

        // Validate start date is not too far in the past
        if (request.StartDate < DateTime.Now.AddDays(-30))
        {
            throw new HospitalOperationException(
                "Start date cannot be more than 30 days in the past"
            );
        }

        // Validate subscription duration is reasonable
        var duration = request.EndDate - request.StartDate;
        if (duration.TotalDays > 365 * 2) // Max 2 years
        {
            throw new HospitalOperationException("Subscription duration cannot exceed 2 years");
        }
    }

    private decimal CalculateProrationAmount(
        HospitalSubscriptionEntity currentSubscription,
        SubscriptionPlanEntity newPlan
    )
    {
        var remainingDays = (currentSubscription.EndDate - DateTime.Now).Days;

        if (remainingDays <= 0)
            return 0;

        // Calculate daily rate based on new plan's billing cycle using actual date calculation
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Use a reference date
        var billingCycleEndDate = baseDate.AddMonths(GetBillingCycleMonths(newPlan.BillingCycle));
        var billingCycleDays = (billingCycleEndDate - baseDate).Days;

        if (billingCycleDays <= 0)
            return 0;

        var dailyRate = newPlan.Price / billingCycleDays;
        return dailyRate * remainingDays;
    }

    private int GetBillingCycleDays(string billingCycle)
    {
        return billingCycle?.ToUpper() switch
        {
            "MONTHLY" => 30,
            "QUARTERLY" => 90,
            "YEARLY" => 365,
            _ => 30,
        };
    }

    private int GetBillingCycleMonths(string billingCycle)
    {
        return billingCycle?.ToUpper() switch
        {
            "MONTHLY" => 1,
            "QUARTERLY" => 3,
            "YEARLY" => 12,
            _ => 1,
        };
    }

    private int GetBillingCycleValue(string billingCycle)
    {
        // Returns a numeric value for comparison: MONTHLY=1, QUARTERLY=3, YEARLY=12
        return billingCycle?.ToUpper() switch
        {
            "MONTHLY" => 1,
            "QUARTERLY" => 3,
            "YEARLY" => 12,
            _ => 1,
        };
    }

    private string GetBillingCycleDisplayName(string billingCycle)
    {
        return billingCycle?.ToUpper() switch
        {
            "MONTHLY" => "Tháng",
            "QUARTERLY" => "Quý",
            "YEARLY" => "Năm",
            _ => "Tháng",
        };
    }
}
