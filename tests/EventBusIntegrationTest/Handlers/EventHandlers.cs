using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace EventBusIntegrationTest.Handlers;

public class TestUserRegisteredEventHandler : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly ILogger<TestUserRegisteredEventHandler> _logger;

    public TestUserRegisteredEventHandler(ILogger<TestUserRegisteredEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 [TEST SERVICE] Processing UserRegisteredEvent: {UserId}", @event.UserId);
        _logger.LogInformation("   User: {Email}, Role: {Role}", @event.Email, @event.Role);
        
        // Simulate processing
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("✅ [TEST SERVICE] UserRegisteredEvent processed successfully");
    }
}

public class TestAppointmentCreatedEventHandler : IIntegrationEventHandler<AppointmentCreatedEvent>
{
    private readonly ILogger<TestAppointmentCreatedEventHandler> _logger;

    public TestAppointmentCreatedEventHandler(ILogger<TestAppointmentCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 [TEST SERVICE] Processing AppointmentCreatedEvent: {AppointmentId}", @event.AppointmentId);
        _logger.LogInformation("   Patient: {PatientId}, Doctor: {DoctorId}, Date: {AppointmentDate}", 
            @event.PatientId, @event.DoctorId, @event.AppointmentDate);
        
        // Simulate processing
        await Task.Delay(150, cancellationToken);
        
        _logger.LogInformation("✅ [TEST SERVICE] AppointmentCreatedEvent processed successfully");
    }
}

public class TestPaymentProcessedEventHandler : IIntegrationEventHandler<PaymentProcessedEvent>
{
    private readonly ILogger<TestPaymentProcessedEventHandler> _logger;

    public TestPaymentProcessedEventHandler(ILogger<TestPaymentProcessedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 [TEST SERVICE] Processing PaymentProcessedEvent: {PaymentId}", @event.PaymentId);
        _logger.LogInformation("   Amount: {Amount}, Method: {PaymentMethod}, Status: {Status}", 
            @event.Amount, @event.PaymentMethod, @event.Status);
        
        // Simulate processing
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("✅ [TEST SERVICE] PaymentProcessedEvent processed successfully");
    }
}

public class TestNotificationSendEventHandler : IIntegrationEventHandler<NotificationSendEvent>
{
    private readonly ILogger<TestNotificationSendEventHandler> _logger;

    public TestNotificationSendEventHandler(ILogger<TestNotificationSendEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(NotificationSendEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 [TEST SERVICE] Processing NotificationSendEvent: {EventId}", @event.Id);
        _logger.LogInformation("   Type: {Type}, UserId: {UserId}, Message: {Message}", 
            @event.Type, @event.UserId, @event.Message);
        
        // Simulate processing
        await Task.Delay(50, cancellationToken);
        
        _logger.LogInformation("✅ [TEST SERVICE] NotificationSendEvent processed successfully");
    }
}
