using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Options;
using BookingCare.Shared.Cache.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BookingCare.Shared.Cache.Extensions;

/// <summary>
/// Extension methods for configuring cache services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Redis cache services to the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cache options
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        return AddRedisCacheInternal(services, cacheOptions);
    }

    /// <summary>
    /// Add Redis cache services with custom options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure cache options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<CacheOptions> configureOptions)
    {
        var cacheOptions = new CacheOptions();
        configureOptions(cacheOptions);

        services.Configure<CacheOptions>(options =>
        {
            options.ConnectionString = cacheOptions.ConnectionString;
            options.DefaultExpirationInMinutes = cacheOptions.DefaultExpirationInMinutes;
            options.KeyPrefix = cacheOptions.KeyPrefix;
            options.Database = cacheOptions.Database;
            options.Enabled = cacheOptions.Enabled;
            options.RetryCount = cacheOptions.RetryCount;
            options.ConnectTimeout = cacheOptions.ConnectTimeout;
            options.CommandTimeout = cacheOptions.CommandTimeout;
        });

        return AddRedisCacheInternal(services, cacheOptions);
    }

    /// <summary>
    /// Internal method to configure Redis cache services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="cacheOptions">Cache options</param>
    /// <returns>Service collection for chaining</returns>
    private static IServiceCollection AddRedisCacheInternal(IServiceCollection services, CacheOptions cacheOptions)
    {
        if (!cacheOptions.Enabled)
        {
            // Register a no-op cache service if caching is disabled
            services.AddSingleton<ICacheService, NoCacheService>();
            return services;
        }

        // Configure Redis connection
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = cacheOptions.ConnectionString;
            var configurationOptions = ConfigurationOptions.Parse(connectionString);

            configurationOptions.ConnectTimeout = cacheOptions.ConnectTimeout * 1000; // Convert to milliseconds
            configurationOptions.SyncTimeout = cacheOptions.CommandTimeout * 1000; // Convert to milliseconds
            configurationOptions.ConnectRetry = cacheOptions.RetryCount;
            configurationOptions.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Configure distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = cacheOptions.ConnectionString;
            //options.InstanceName = cacheOptions.KeyPrefix.TrimEnd(':');
        });

        // Register cache service
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}