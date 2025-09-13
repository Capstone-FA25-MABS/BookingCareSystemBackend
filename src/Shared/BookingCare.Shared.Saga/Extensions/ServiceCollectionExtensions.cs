using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Manager;
using BookingCare.Shared.Saga.StateStore;
using BookingCare.Shared.Saga.Steps.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookingCare.Shared.Saga.Extensions;

/// <summary>
/// Extension methods for configuring saga services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds saga orchestration services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddSagaOrchestration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SagaOptions>? configureOptions = null)
    {
        var options = new SagaOptions();
        configuration.GetSection("Saga").Bind(options);
        configureOptions?.Invoke(options);

        services.Configure<SagaOptions>(config =>
        {
            config.StateStoreType = options.StateStoreType;
            config.ConnectionString = options.ConnectionString;
            config.ProcessingInterval = options.ProcessingInterval;
            config.DefaultTimeout = options.DefaultTimeout;
        });

        // Register core saga services
        services.AddSingleton<ISagaOrchestrator, SagaOrchestrator>();
        services.AddSingleton<ISagaManager, SagaManager>();

        // Register state store based on configuration
        switch (options.StateStoreType.ToLowerInvariant())
        {
            case "inmemory":
                services.AddSingleton<ISagaStateStore, InMemorySagaStateStore>();
                break;
            case "sqlserver":
                services.AddSingleton<ISagaStateStore>(provider =>
                    new SqlServerSagaStateStore(options.ConnectionString,
                        provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqlServerSagaStateStore>>()));
                break;
            default:
                services.AddSingleton<ISagaStateStore, InMemorySagaStateStore>();
                break;
        }

        // Register background service for processing pending sagas
        services.AddHostedService<SagaBackgroundService>();

        return services;
    }

    /// <summary>
    /// Registers a saga definition
    /// </summary>
    public static IServiceCollection AddSaga<TSaga>(this IServiceCollection services)
        where TSaga : class, ISagaDefinition
    {
        services.AddTransient<TSaga>();
        services.AddTransient<ISagaDefinition, TSaga>();
        return services;
    }

    /// <summary>
    /// Registers multiple saga definitions
    /// </summary>
    public static IServiceCollection AddSagas(this IServiceCollection services, params Type[] sagaTypes)
    {
        foreach (var sagaType in sagaTypes)
        {
            if (!typeof(ISagaDefinition).IsAssignableFrom(sagaType))
            {
                throw new ArgumentException($"Type {sagaType.Name} does not implement ISagaDefinition");
            }

            services.AddTransient(sagaType);
            services.AddTransient(typeof(ISagaDefinition), sagaType);
        }

        return services;
    }

    /// <summary>
    /// Registers a saga step
    /// </summary>
    public static IServiceCollection AddSagaStep<TStep>(this IServiceCollection services)
        where TStep : class, ISagaStep
    {
        services.AddTransient<TStep>();
        return services;
    }

    /// <summary>
    /// Registers multiple saga steps
    /// </summary>
    public static IServiceCollection AddSagaSteps(this IServiceCollection services, params Type[] stepTypes)
    {
        foreach (var stepType in stepTypes)
        {
            if (!typeof(ISagaStep).IsAssignableFrom(stepType))
            {
                throw new ArgumentException($"Type {stepType.Name} does not implement ISagaStep");
            }

            services.AddTransient(stepType);
        }

        return services;
    }

    /// <summary>
    /// Registers gRPC saga steps for distributed transactions
    /// </summary>
    public static IServiceCollection AddGrpcSagaSteps(this IServiceCollection services)
    {
        services.AddTransient<CreateUserAccountGrpcStep>();
        services.AddTransient<CreateUserProfileGrpcStep>();
        services.AddTransient<SendVerificationEmailGrpcStep>();

        return services;
    }

    /// <summary>
    /// Registers a saga event handler
    /// </summary>
    public static IServiceCollection AddSagaEventHandler<THandler>(this IServiceCollection services)
        where THandler : class
    {
        var handlerInterfaces = typeof(THandler).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaEventHandler<>));

        foreach (var handlerInterface in handlerInterfaces)
        {
            services.AddTransient(handlerInterface, typeof(THandler));
        }

        return services;
    }

    /// <summary>
    /// Registers multiple saga event handlers
    /// </summary>
    public static IServiceCollection AddSagaEventHandlers(this IServiceCollection services, params Type[] handlerTypes)
    {
        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaEventHandler<>));

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddTransient(handlerInterface, handlerType);
            }
        }

        return services;
    }
}

/// <summary>
/// Configuration options for saga orchestration
/// </summary>
public class SagaOptions
{
    public string StateStoreType { get; set; } = "InMemory";
    public string ConnectionString { get; set; } = string.Empty;
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
