using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;

namespace BookingCare.Services.Hospital.Services.Helpers;

/// <summary>
/// Wrapper class for subscription-related services to reduce constructor parameters
/// </summary>
public class SubscriptionServices
{
    public ISubscriptionPlanRepository SubscriptionPlanRepository { get; }
    public IHospitalSubscriptionService HospitalSubscriptionService { get; }
    public ISubscriptionUsageService SubscriptionUsageService { get; }

    public SubscriptionServices(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IHospitalSubscriptionService hospitalSubscriptionService,
        ISubscriptionUsageService subscriptionUsageService)
    {
        SubscriptionPlanRepository = subscriptionPlanRepository;
        HospitalSubscriptionService = hospitalSubscriptionService;
        SubscriptionUsageService = subscriptionUsageService;
    }
}

