using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Configuration;
using BookingCare.Shared.EventBus.Connection;
using BookingCare.Shared.EventBus.HealthChecks;

namespace BookingCare.Shared.EventBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string? queueName = null)
    {
        // Configure RabbitMQ settings
        services.Configure<RabbitMQConfiguration>(configuration.GetSection(RabbitMQConfiguration.SectionName));

        // Register RabbitMQ connection factory
        services.AddSingleton<IConnectionFactory>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<RabbitMQConfiguration>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<IConnectionFactory>>();

            var factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
                VirtualHost = config.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(config.ConnectionTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(config.RequestedHeartbeat),
                AutomaticRecoveryEnabled = config.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(config.NetworkRecoveryInterval),
                DispatchConsumersAsync = true
            };

            logger.LogInformation("RabbitMQ connection factory configured for {HostName}:{Port}", config.HostName, config.Port);

            return factory;
        });

        // Register persistent connection
        services.AddSingleton<IRabbitMQPersistentConnection, RabbitMQPersistentConnection>();

        // Register subscription manager
        services.AddSingleton<IEventBusSubscriptionsManager, EventBusSubscriptionsManager>();

        // Register event bus
        services.AddSingleton<IEventBus>(serviceProvider =>
        {
            var persistentConnection = serviceProvider.GetRequiredService<IRabbitMQPersistentConnection>();
            var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQEventBus>>();
            var subsManager = serviceProvider.GetRequiredService<IEventBusSubscriptionsManager>();
            var config = serviceProvider.GetRequiredService<IOptions<RabbitMQConfiguration>>();

            return new RabbitMQEventBus(
                persistentConnection,
                logger,
                subsManager,
                serviceProvider,
                config,
                queueName);
        });

        // Add health check
        services.AddHealthChecks()
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: new[] { "ready", "eventbus" });

        return services;
    }

    public static IServiceCollection AddIntegrationEventHandler<THandler>(this IServiceCollection services)
        where THandler : class
    {
        services.AddTransient<THandler>();
        return services;
    }

    public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, params Type[] handlerTypes)
    {
        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }
        return services;
    }
}
