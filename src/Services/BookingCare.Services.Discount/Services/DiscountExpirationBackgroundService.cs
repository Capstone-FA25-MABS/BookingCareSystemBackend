using BookingCare.Services.Discount.Services;

namespace BookingCare.Services.Discount.Services;

public class DiscountExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscountExpirationBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1); // Check every hour

    public DiscountExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DiscountExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking for expired discounts at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var discountService = scope.ServiceProvider.GetRequiredService<IDiscountService>();

                var expiredCount = await discountService.UpdateExpiredDiscountsAsync();

                if (expiredCount > 0)
                {
                    _logger.LogInformation("Updated {Count} expired discounts", expiredCount);
                }
                else
                {
                    _logger.LogDebug("No expired discounts found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating expired discounts");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }
}
