using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Models.Entities;

namespace BookingCare.Services.Hospital.Services.Implementations;

public class SubscriptionRenewalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionRenewalService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Run daily

    public SubscriptionRenewalService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionRenewalService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Renewal Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiringSubscriptionsAsync();
                await ProcessExpiredSubscriptionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription renewal processing");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Subscription Renewal Service stopped");
    }

    private async Task ProcessExpiringSubscriptionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var hospitalSubscriptionRepository = scope.ServiceProvider
            .GetRequiredService<IHospitalSubscriptionRepository>();

        try
        {
            // Get subscriptions expiring in the next 7 days
            var expiringSubscriptions = await hospitalSubscriptionRepository
                .GetExpiringSoonAsync(7);

            _logger.LogInformation("Found {Count} subscriptions expiring soon", expiringSubscriptions.Count);

            foreach (var subscription in expiringSubscriptions)
            {
                if (subscription.Status != SubscriptionStatus.ACTIVE)
                    continue;

                var daysUntilExpiry = (subscription.EndDate.Date - DateTime.Now.Date).Days;

                // Send expiry notification for subscriptions expiring soon
                if (daysUntilExpiry <= 7 && daysUntilExpiry >= 0)
                {
                    await SendExpiryNotificationAsync(subscription, daysUntilExpiry);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expiring subscriptions");
        }
    }

    private async Task ProcessExpiredSubscriptionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var hospitalSubscriptionRepository = scope.ServiceProvider
            .GetRequiredService<IHospitalSubscriptionRepository>();

        try
        {
            // Get all subscriptions and update their status based on current date
            var allSubscriptions = await hospitalSubscriptionRepository.GetAllAsync();
            var expiredSubscriptions = allSubscriptions
                .Where(s => s.Status == SubscriptionStatus.ACTIVE &&
                           s.EndDate < DateTime.Now)
                .ToList();

            _logger.LogInformation("Found {Count} expired subscriptions to update",
                expiredSubscriptions.Count);

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = SubscriptionStatus.EXPIRED;
                subscription.UpdatedAt = DateTime.Now;
                await hospitalSubscriptionRepository.UpdateAsync(subscription);

                await SendExpiryNotificationAsync(subscription, 0); // 0 days = expired
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired subscriptions");
        }
    }

    private async Task SendExpiryNotificationAsync(HospitalSubscriptionEntity subscription, int daysUntilExpiry)
    {
        try
        {
            // This would integrate with your notification service
            _logger.LogInformation(
                "Subscription expiry reminder: Hospital {HospitalId}, " +
                "Plan {PlanName}, expires in {Days} days (Subscription ID: {SubscriptionId})",
                subscription.HospitalId,
                subscription.SubscriptionPlan.Name,
                daysUntilExpiry,
                subscription.HospitalSubscriptionId);

            // In real implementation, you would:
            // 1. Send email notification to hospital admin
            // 2. Send in-app notification
            // 3. Send SMS if configured
            // 4. Create notification record in database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry notification for subscription {SubscriptionId}",
                subscription.HospitalSubscriptionId);
        }
    }

    private async Task UpdateSubscriptionAsync(HospitalSubscriptionEntity subscription)
    {
        using var scope = _serviceProvider.CreateScope();
        var hospitalSubscriptionRepository = scope.ServiceProvider
            .GetRequiredService<IHospitalSubscriptionRepository>();

        await hospitalSubscriptionRepository.UpdateAsync(subscription);
    }

}
