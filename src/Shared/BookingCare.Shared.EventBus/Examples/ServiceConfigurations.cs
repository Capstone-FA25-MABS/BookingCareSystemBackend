using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Handlers;

namespace BookingCare.Shared.EventBus.Examples;

/// <summary>
/// Example configuration for User Service
/// </summary>
public static class UserServiceEventBusConfiguration
{
    public static IServiceCollection AddUserServiceEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the event bus
        services.AddRabbitMQEventBus(configuration, "user-service-queue");

        // Register event handlers specific to User Service
        services.AddIntegrationEventHandler<AppointmentCreatedEventHandler>();
        services.AddIntegrationEventHandler<PaymentProcessedEventHandler>();
        services.AddIntegrationEventHandler<NotificationSendEventHandler>();

        return services;
    }

    public static IApplicationBuilder UseUserServiceEventBus(this IApplicationBuilder app)
    {
        return app.UseEventBus(eventBus =>
        {
            // Subscribe to events that User Service cares about
            eventBus.Subscribe<AppointmentCreatedEvent, AppointmentCreatedEventHandler>();
            eventBus.Subscribe<PaymentProcessedEvent, PaymentProcessedEventHandler>();
            eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>();
        });
    }
}

/// <summary>
/// Example configuration for Appointment Service
/// </summary>
public static class AppointmentServiceEventBusConfiguration
{
    public static IServiceCollection AddAppointmentServiceEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMQEventBus(configuration, "appointment-service-queue");

        services.AddIntegrationEventHandler<UserRegisteredEventHandler>();
        services.AddIntegrationEventHandler<PaymentProcessedEventHandler>();

        return services;
    }

    public static IApplicationBuilder UseAppointmentServiceEventBus(this IApplicationBuilder app)
    {
        return app.UseEventBus(eventBus =>
        {
            eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
            eventBus.Subscribe<PaymentProcessedEvent, PaymentProcessedEventHandler>();
        });
    }
}

/// <summary>
/// Example configuration for Payment Service
/// </summary>
public static class PaymentServiceEventBusConfiguration
{
    public static IServiceCollection AddPaymentServiceEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMQEventBus(configuration, "payment-service-queue");

        services.AddIntegrationEventHandler<AppointmentCreatedEventHandler>();

        return services;
    }

    public static IApplicationBuilder UsePaymentServiceEventBus(this IApplicationBuilder app)
    {
        return app.UseEventBus(eventBus =>
        {
            eventBus.Subscribe<AppointmentCreatedEvent, AppointmentCreatedEventHandler>();
        });
    }
}

/// <summary>
/// Example configuration for Notification Service
/// </summary>
public static class NotificationServiceEventBusConfiguration
{
    public static IServiceCollection AddNotificationServiceEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMQEventBus(configuration, "notification-service-queue");

        services.AddIntegrationEventHandler<UserRegisteredEventHandler>();
        services.AddIntegrationEventHandler<AppointmentCreatedEventHandler>();
        services.AddIntegrationEventHandler<PaymentProcessedEventHandler>();
        services.AddIntegrationEventHandler<NotificationSendEventHandler>();

        return services;
    }

    public static IApplicationBuilder UseNotificationServiceEventBus(this IApplicationBuilder app)
    {
        return app.UseEventBus(eventBus =>
        {
            // Subscribe to all events that should trigger notifications
            eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
            eventBus.Subscribe<AppointmentCreatedEvent, AppointmentCreatedEventHandler>();
            eventBus.Subscribe<PaymentProcessedEvent, PaymentProcessedEventHandler>();

            // Use routing keys for different notification types
            eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>("notification.email");
            eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>("notification.sms");
            eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>("notification.push");
        });
    }
}
