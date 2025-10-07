using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Services.BackgroundServices;

/// <summary>
/// Background service ?? t? ??ng x? lý refund histories t? WAITING sang PENDING
/// khi user có bank account active
/// </summary>
public class RefundHistoryProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefundHistoryProcessingService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Ch?y m?i 5 phút

    public RefundHistoryProcessingService(
        IServiceProvider serviceProvider,
        ILogger<RefundHistoryProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefundHistoryProcessingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessWaitingRefundsAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RefundHistoryProcessingService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RefundHistoryProcessingService");
                // Continue running even if there's an error
                await Task.Delay(_interval, stoppingToken);
            }
        }

        _logger.LogInformation("RefundHistoryProcessingService stopped");
    }

    private async Task ProcessWaitingRefundsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var refundHistoryService = scope.ServiceProvider.GetRequiredService<IRefundHistoryService>();

            var processedCount = await refundHistoryService.ProcessWaitingRefundsAsync();

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} waiting refund histories", processedCount);
            }
            else
            {
                _logger.LogDebug("No waiting refund histories to process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waiting refund histories");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefundHistoryProcessingService is stopping");
        await base.StopAsync(stoppingToken);
    }
}