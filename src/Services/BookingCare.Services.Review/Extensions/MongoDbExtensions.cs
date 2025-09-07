namespace BookingCare.Services.Review.Extensions;

/// <summary>
/// Extension methods for MongoDB configuration
/// </summary>
public static class MongoDbExtensions
{
    /// <summary>
    /// Adds MongoDB services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB")
                              ?? configuration["MongoDbSettings:ConnectionString"];
        var databaseName = configuration["MongoDbSettings:DatabaseName"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("MongoDB connection string is not configured");
        }

        if (string.IsNullOrEmpty(databaseName))
        {
            throw new InvalidOperationException("MongoDB database name is not configured");
        }

        // Register MongoDB client as singleton
        services.AddSingleton<MongoDB.Driver.IMongoClient>(serviceProvider =>
        {
            return new MongoDB.Driver.MongoClient(connectionString);
        });

        // Register MongoDB database as singleton
        services.AddSingleton<MongoDB.Driver.IMongoDatabase>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<MongoDB.Driver.IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        return services;
    }
}