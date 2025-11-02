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

            // Get current usage counts
            var currentDoctorCount = await GetCurrentDoctorCountAsync();
            var currentSpecialtyCount = await GetCurrentSpecialtyCountAsync();
            var currentAppointmentCount = await GetCurrentAppointmentCountAsync();

            // Handle nullable limits (null = unlimited)
            var maxDoctors = plan.MaxDoctors ?? int.MaxValue; // null = unlimited
            var maxSpecialties = plan.MaxSpecialties ?? int.MaxValue;
            var maxAppointments = plan.MaxAppointments ?? int.MaxValue;

            var usage = new SubscriptionUsageResponse
            {
                HospitalId = hospitalId,
                HasActiveSubscription = true,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlanName = plan.Name,
                MaxDoctors = maxDoctors,
                MaxSpecialties = maxSpecialties,
                CurrentDoctorCount = currentDoctorCount,
                CurrentSpecialtyCount = currentSpecialtyCount,
                CurrentAppointmentCount = currentAppointmentCount,
                DoctorUsagePercentage = maxDoctors > 0 && maxDoctors < int.MaxValue
                    ? (decimal)currentDoctorCount / maxDoctors * 100 : 0,
                SpecialtyUsagePercentage = maxSpecialties > 0 && maxSpecialties < int.MaxValue
                    ? (decimal)currentSpecialtyCount / maxSpecialties * 100 : 0,
                IsDoctorLimitExceeded = maxDoctors < int.MaxValue && currentDoctorCount > maxDoctors,
                IsSpecialtyLimitExceeded = maxSpecialties < int.MaxValue && currentSpecialtyCount > maxSpecialties,
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

            var currentDoctorCount = await GetCurrentDoctorCountAsync();
            return currentDoctorCount < plan.MaxDoctors;
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

            var currentSpecialtyCount = await GetCurrentSpecialtyCountAsync();
            return currentSpecialtyCount < plan.MaxSpecialties;
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

            var currentAppointmentCount = await GetCurrentAppointmentCountAsync();
            return (currentAppointmentCount + additionalAppointments) <= plan.MaxAppointments;
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

    private async Task<int> GetCurrentDoctorCountAsync()
    {
        // This would need to be implemented based on your doctor service
        // For now, we'll return a mock value
        // In real implementation, you would call the doctor service or repository
        return 0; // Placeholder
    }

    private async Task<int> GetCurrentSpecialtyCountAsync()
    {
        // This would need to be implemented based on your specialty service
        // For now, we'll return a mock value
        // In real implementation, you would call the specialty service or repository
        return 0; // Placeholder
    }

    private async Task<int> GetCurrentAppointmentCountAsync()
    {
        // This would need to be implemented based on your appointment service
        // For now, we'll return a mock value
        // In real implementation, you would call the appointment service or repository
        return 0; // Placeholder
    }
}
