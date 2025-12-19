using BookingCare.Services.Notification.Setting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookingCare.Services.Notification.Services;

/// <summary>
/// Background service that ensures MongoDB collections are created on startup.
/// </summary>
public class MongoDbInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MongoDbInitializationService> _logger;

    public MongoDbInitializationService(
        IServiceProvider serviceProvider,
        ILogger<MongoDbInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB collections...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var settings = scope.ServiceProvider
                .GetRequiredService<IOptions<MongoDbSettings>>().Value;

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            // Get existing collections
            var existingCollections = await database
                .ListCollectionNamesAsync(cancellationToken: cancellationToken);
            var collectionNames = await existingCollections.ToListAsync(cancellationToken);

            // Create devices collection if not exists
            if (!collectionNames.Contains(settings.DevicesCollectionName))
            {
                await database.CreateCollectionAsync(
                    settings.DevicesCollectionName,
                    cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "Created MongoDB collection: {CollectionName}",
                    settings.DevicesCollectionName);
            }

            // Create notifications collection if not exists
            if (!collectionNames.Contains(settings.NotificationsCollectionName))
            {
                await database.CreateCollectionAsync(
                    settings.NotificationsCollectionName,
                    cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "Created MongoDB collection: {CollectionName}",
                    settings.NotificationsCollectionName);
            }

            _logger.LogInformation("MongoDB collections initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB collections");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
