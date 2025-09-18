using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Services.BackgroundServices;

/// <summary>
/// Background service ?? cleanup các PayOS mapping ?ã h?t h?n
/// Ch?y ??nh k? ?? xóa các mapping c? và ti?t ki?m dung l??ng database
/// </summary>
public class PayOSMappingCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayOSMappingCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public PayOSMappingCleanupService(
        IServiceProvider serviceProvider,
        ILogger<PayOSMappingCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // L?y interval t? configuration, m?c ??nh 1 gi?
        var intervalMinutes = configuration.GetValue<int>("PayOS:CleanupIntervalMinutes", 60);
        _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PayOS Mapping Cleanup Service started with interval: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PayOS Mapping Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during PayOS mapping cleanup");

                // ??i 5 phút tr??c khi th? l?i n?u có l?i
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();

        _logger.LogInformation("Starting PayOS mapping cleanup task");

        var deletedCount = await payOSService.CleanupExpiredMappingsAsync();

        if (deletedCount > 0)
        {
            _logger.LogInformation("PayOS mapping cleanup completed - Deleted {DeletedCount} expired mappings", deletedCount);
        }
        else
        {
            _logger.LogDebug("PayOS mapping cleanup completed - No expired mappings found");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayOS Mapping Cleanup Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}