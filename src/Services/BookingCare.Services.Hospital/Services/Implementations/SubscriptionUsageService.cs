using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Exceptions;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class SubscriptionUsageService : ISubscriptionUsageService
{
    private readonly IHospitalSubscriptionRepository _hospitalSubscriptionRepository;
    private readonly IHospitalRepository _hospitalRepository;
    private readonly ILogger<SubscriptionUsageService> _logger;

    public SubscriptionUsageService(
        IHospitalSubscriptionRepository hospitalSubscriptionRepository,
        IHospitalRepository hospitalRepository,
        ILogger<SubscriptionUsageService> logger)
    {
        _hospitalSubscriptionRepository = hospitalSubscriptionRepository;
        _hospitalRepository = hospitalRepository;
        _logger = logger;
    }

    public async Task<SubscriptionUsageResponse> GetUsageByHospitalIdAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                return new SubscriptionUsageResponse
                {
                    HospitalId = hospitalId,
                    HasActiveSubscription = false,
                    Message = "No active subscription found"
                };
            }

            var plan = activeSubscription.SubscriptionPlan;

            // Get current usage counts from HospitalSubscription entity
            var currentDoctorCount = activeSubscription.DoctorCount;
            var currentSpecialtyCount = activeSubscription.SpecialtyCount;
            var currentAppointmentCount = activeSubscription.AppointmentCount;
            var currentServiceCount = activeSubscription.ServiceCount;

            // Handle nullable limits (null = unlimited) - keep null instead of converting to int.MaxValue
            var maxDoctors = plan.MaxDoctors; // null = unlimited
            var maxSpecialties = plan.MaxSpecialties;
            var maxAppointments = plan.MaxAppointments;
            var maxServices = plan.MaxServices;

            var usage = new SubscriptionUsageResponse
            {
                HospitalId = hospitalId,
                HasActiveSubscription = true,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlanName = plan.Name,
                MaxDoctors = maxDoctors,
                MaxSpecialties = maxSpecialties,
                MaxAppointments = maxAppointments,
                MaxServices = maxServices,
                CurrentDoctorCount = currentDoctorCount,
                CurrentSpecialtyCount = currentSpecialtyCount,
                CurrentAppointmentCount = currentAppointmentCount,
                CurrentServiceCount = currentServiceCount,
                DoctorUsagePercentage = maxDoctors.HasValue && maxDoctors.Value > 0
                    ? (decimal)currentDoctorCount / maxDoctors.Value * 100 : 0,
                SpecialtyUsagePercentage = maxSpecialties.HasValue && maxSpecialties.Value > 0
                    ? (decimal)currentSpecialtyCount / maxSpecialties.Value * 100 : 0,
                AppointmentUsagePercentage = maxAppointments.HasValue && maxAppointments.Value > 0
                    ? (decimal)currentAppointmentCount / maxAppointments.Value * 100 : 0,
                ServiceUsagePercentage = maxServices.HasValue && maxServices.Value > 0
                    ? (decimal)currentServiceCount / maxServices.Value * 100 : 0,
                IsDoctorLimitExceeded = maxDoctors.HasValue && currentDoctorCount >= maxDoctors.Value,
                IsSpecialtyLimitExceeded = maxSpecialties.HasValue && currentSpecialtyCount >= maxSpecialties.Value,
                IsAppointmentLimitExceeded = maxAppointments.HasValue && currentAppointmentCount >= maxAppointments.Value,
                IsServiceLimitExceeded = maxServices.HasValue && currentServiceCount >= maxServices.Value,
                SubscriptionEndDate = activeSubscription.EndDate,
                DaysUntilExpiry = (activeSubscription.EndDate - DateTime.Now).Days
            };

            return usage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to retrieve subscription usage", ex);
        }
    }

    public async Task<bool> CheckDoctorLimitAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null) return false;

            var plan = activeSubscription.SubscriptionPlan;

            // null means unlimited
            if (plan.MaxDoctors == null) return true;

            // Use count from HospitalSubscription entity
            return activeSubscription.DoctorCount < plan.MaxDoctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor limit for hospital {HospitalId}", hospitalId);
            return false;
        }
    }

    public async Task<bool> CheckSpecialtyLimitAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null) return false;

            var plan = activeSubscription.SubscriptionPlan;

            // null means unlimited
            if (plan.MaxSpecialties == null) return true;

            // Use count from HospitalSubscription entity
            return activeSubscription.SpecialtyCount < plan.MaxSpecialties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking specialty limit for hospital {HospitalId}", hospitalId);
            return false;
        }
    }

    public async Task<bool> CheckAppointmentLimitAsync(Guid hospitalId, int additionalAppointments = 1)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null) return false;

            // Note: This would need to be implemented based on your appointment system
            // For now, we'll assume unlimited appointments unless specified in the plan
            var plan = activeSubscription.SubscriptionPlan;

            // null means unlimited
            if (plan.MaxAppointments == null) return true;

            // Use count from HospitalSubscription entity
            return (activeSubscription.AppointmentCount + additionalAppointments) <= plan.MaxAppointments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking appointment limit for hospital {HospitalId}", hospitalId);
            return false;
        }
    }

    public async Task<List<SubscriptionUsageResponse>> GetUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var hospitals = await _hospitalRepository.GetAllAsync();
            var usageReports = new List<SubscriptionUsageResponse>();

            foreach (var hospital in hospitals)
            {
                var usage = await GetUsageByHospitalIdAsync(hospital.Id);
                usageReports.Add(usage);
            }

            // Filter by date range if provided
            if (fromDate.HasValue || toDate.HasValue)
            {
                // This would need to be implemented based on your specific requirements
                // For now, we'll return all reports
            }

            return usageReports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating usage report");
            throw new HospitalOperationException("Failed to generate usage report", ex);
        }
    }

    public async Task<SubscriptionUsageAlertResponse> CheckUsageAlertsAsync(Guid hospitalId)
    {
        try
        {
            var usage = await GetUsageByHospitalIdAsync(hospitalId);

            if (!usage.HasActiveSubscription)
            {
                return new SubscriptionUsageAlertResponse
                {
                    HospitalId = hospitalId,
                    HasAlerts = false,
                    Alerts = new List<string>()
                };
            }

            var alerts = new List<string>();

            // Check doctor limit
            if (usage.IsDoctorLimitExceeded)
            {
                alerts.Add($"Doctor limit exceeded: {usage.CurrentDoctorCount}/{usage.MaxDoctors}");
            }
            else if (usage.DoctorUsagePercentage >= 90)
            {
                alerts.Add($"Doctor limit warning: {usage.CurrentDoctorCount}/{usage.MaxDoctors} ({(int)usage.DoctorUsagePercentage}%)");
            }

            // Check specialty limit
            if (usage.IsSpecialtyLimitExceeded)
            {
                alerts.Add($"Specialty limit exceeded: {usage.CurrentSpecialtyCount}/{usage.MaxSpecialties}");
            }
            else if (usage.SpecialtyUsagePercentage >= 90)
            {
                alerts.Add($"Specialty limit warning: {usage.CurrentSpecialtyCount}/{usage.MaxSpecialties} ({(int)usage.SpecialtyUsagePercentage}%)");
            }

            // Check subscription expiry
            if (usage.DaysUntilExpiry <= 7 && usage.DaysUntilExpiry > 0)
            {
                alerts.Add($"Subscription expires in {usage.DaysUntilExpiry} days");
            }
            else if (usage.DaysUntilExpiry <= 0)
            {
                alerts.Add("Subscription has expired");
            }

            return new SubscriptionUsageAlertResponse
            {
                HospitalId = hospitalId,
                HasAlerts = alerts.Any(),
                Alerts = alerts,
                Usage = usage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking usage alerts for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to check usage alerts", ex);
        }
    }

    public async Task<bool> CheckServiceLimitAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null) return false;

            var plan = activeSubscription.SubscriptionPlan;

            // null means unlimited
            if (plan.MaxServices == null) return true;

            // Use count from HospitalSubscription entity
            return activeSubscription.ServiceCount < plan.MaxServices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service limit for hospital {HospitalId}", hospitalId);
            return false;
        }
    }

    public async Task IncrementDoctorCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            activeSubscription.DoctorCount++;
            activeSubscription.UpdatedAt = DateTime.Now;
            await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing doctor count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to increment doctor count", ex);
        }
    }

    public async Task DecrementDoctorCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            if (activeSubscription.DoctorCount > 0)
            {
                activeSubscription.DoctorCount--;
                activeSubscription.UpdatedAt = DateTime.Now;
                await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing doctor count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to decrement doctor count", ex);
        }
    }

    public async Task IncrementSpecialtyCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            activeSubscription.SpecialtyCount++;
            activeSubscription.UpdatedAt = DateTime.Now;
            await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing specialty count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to increment specialty count", ex);
        }
    }

    public async Task DecrementSpecialtyCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            if (activeSubscription.SpecialtyCount > 0)
            {
                activeSubscription.SpecialtyCount--;
                activeSubscription.UpdatedAt = DateTime.Now;
                await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing specialty count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to decrement specialty count", ex);
        }
    }

    public async Task IncrementAppointmentCountAsync(Guid hospitalId, int count = 1)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            activeSubscription.AppointmentCount += count;
            activeSubscription.UpdatedAt = DateTime.Now;
            await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing appointment count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to increment appointment count", ex);
        }
    }

    public async Task DecrementAppointmentCountAsync(Guid hospitalId, int count = 1)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            if (activeSubscription.AppointmentCount >= count)
            {
                activeSubscription.AppointmentCount -= count;
                activeSubscription.UpdatedAt = DateTime.Now;
                await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing appointment count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to decrement appointment count", ex);
        }
    }

    public async Task IncrementServiceCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            activeSubscription.ServiceCount++;
            activeSubscription.UpdatedAt = DateTime.Now;
            await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing service count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to increment service count", ex);
        }
    }

    public async Task DecrementServiceCountAsync(Guid hospitalId)
    {
        try
        {
            var activeSubscription = await _hospitalSubscriptionRepository
                .GetActiveByHospitalIdAsync(hospitalId);

            if (activeSubscription == null)
            {
                _logger.LogWarning("No active subscription found for hospital {HospitalId}", hospitalId);
                return;
            }

            if (activeSubscription.ServiceCount > 0)
            {
                activeSubscription.ServiceCount--;
                activeSubscription.UpdatedAt = DateTime.Now;
                await _hospitalSubscriptionRepository.UpdateAsync(activeSubscription);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing service count for hospital {HospitalId}", hospitalId);
            throw new HospitalOperationException("Failed to decrement service count", ex);
        }
    }
}
