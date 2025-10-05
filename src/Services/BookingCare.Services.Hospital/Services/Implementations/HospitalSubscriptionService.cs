using AutoMapper;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class HospitalSubscriptionService : IHospitalSubscriptionService
{
    private readonly IHospitalSubscriptionRepository _hospitalSubscriptionRepository;
    private readonly IHospitalRepository _hospitalRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IMapper _mapper;

    public HospitalSubscriptionService(
        IHospitalSubscriptionRepository hospitalSubscriptionRepository,
        IHospitalRepository hospitalRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IMapper mapper)
    {
        _hospitalSubscriptionRepository = hospitalSubscriptionRepository;
        _hospitalRepository = hospitalRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _mapper = mapper;
    }

    public async Task<HospitalSubscriptionResponse?> GetByIdAsync(Guid id)
    {
        var subscription = await _hospitalSubscriptionRepository.GetByIdAsync(id);
        if (subscription != null)
        {
            // Update status based on current date
            await UpdateSubscriptionStatusAsync(subscription);
        }
        return subscription != null ? _mapper.Map<HospitalSubscriptionResponse>(subscription) : null;
    }

    public async Task<HospitalSubscriptionResponse?> GetActiveByHospitalIdAsync(Guid hospitalId)
    {
        var subscription = await _hospitalSubscriptionRepository.GetActiveByHospitalIdAsync(hospitalId);
        if (subscription != null)
        {
            // Update status based on current date
            await UpdateSubscriptionStatusAsync(subscription);
        }
        return subscription != null ? _mapper.Map<HospitalSubscriptionResponse>(subscription) : null;
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

    public async Task<HospitalSubscriptionResponse> CreateAsync(CreateHospitalSubscriptionRequest request)
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

        var subscription = _mapper.Map<HospitalSubscriptionEntity>(request);

        // Set initial status based on dates
        subscription.Status = DetermineInitialStatus(subscription.StartDate, subscription.EndDate);

        try
        {
            var createdSubscription = await _hospitalSubscriptionRepository.CreateAsync(subscription);
            return _mapper.Map<HospitalSubscriptionResponse>(createdSubscription);
        }
        catch (Exception ex)
        {
            throw new HospitalOperationException("Failed to create hospital subscription", ex);
        }
    }

    public async Task<HospitalSubscriptionResponse> UpdateAsync(Guid id, UpdateHospitalSubscriptionRequest request)
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
                existingSubscription.EndDate);
        }

        try
        {
            var updatedSubscription = await _hospitalSubscriptionRepository.UpdateAsync(existingSubscription);
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
        var newStatus = DetermineCurrentStatus(subscription.StartDate, subscription.EndDate, subscription.Status);

        if (currentStatus != newStatus)
        {
            subscription.Status = newStatus;
            await _hospitalSubscriptionRepository.UpdateAsync(subscription);
        }
    }

    private SubscriptionStatus DetermineInitialStatus(DateTime startDate, DateTime endDate)
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

    private SubscriptionStatus DetermineCurrentStatus(DateTime startDate, DateTime endDate, SubscriptionStatus currentStatus)
    {
        var now = DateTime.Now;

        // Don't change manually set statuses like CANCELLED or TRIAL
        if (currentStatus == SubscriptionStatus.CANCELLED || currentStatus == SubscriptionStatus.TRIAL)
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
}
