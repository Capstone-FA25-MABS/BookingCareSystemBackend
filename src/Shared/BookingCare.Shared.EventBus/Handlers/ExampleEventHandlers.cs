using Microsoft.Extensions.Logging;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Shared.EventBus.Handlers;

// Example handlers for common events
public class UserRegisteredEventHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling UserRegisteredEvent for user {UserId} - {Email}",
            @event.UserId, @event.Email);

        // Example: Send welcome email, create user profile, etc.
        await Task.CompletedTask;
    }
}

public class AppointmentCreatedEventHandler : IIntegrationEventHandler<AppointmentCreatedEvent>
{
    private readonly ILogger<AppointmentCreatedEventHandler> _logger;

    public AppointmentCreatedEventHandler(ILogger<AppointmentCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling AppointmentCreatedEvent for appointment {AppointmentId}",
            @event.AppointmentId);

        // Example: Send notification, update availability, etc.
        await Task.CompletedTask;
    }
}

public class PaymentProcessedEventHandler : IIntegrationEventHandler<PaymentProcessedEvent>
{
    private readonly ILogger<PaymentProcessedEventHandler> _logger;

    public PaymentProcessedEventHandler(ILogger<PaymentProcessedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling PaymentProcessedEvent for payment {PaymentId} - Amount: {Amount}",
            @event.PaymentId, @event.Amount);

        // Example: Update appointment status, send receipt, etc.
        await Task.CompletedTask;
    }
}

public class NotificationSendEventHandler : IIntegrationEventHandler<NotificationSendEvent>
{
    private readonly ILogger<NotificationSendEventHandler> _logger;

    public NotificationSendEventHandler(ILogger<NotificationSendEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(NotificationSendEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling NotificationSendEvent for user {UserId} - Type: {Type}",
            @event.UserId, @event.Type);

        // Example: Send email, SMS, push notification based on type
        await Task.CompletedTask;
    }
}
