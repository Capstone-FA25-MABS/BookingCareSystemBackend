using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using EventBusIntegrationTest.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add EventBus with specific queue name for this test service
builder.Services.AddRabbitMQEventBus(builder.Configuration, "eventbus-test-service-queue");

// Register event handlers
builder.Services.AddIntegrationEventHandler<TestUserRegisteredEventHandler>();
builder.Services.AddIntegrationEventHandler<TestAppointmentCreatedEventHandler>();
builder.Services.AddIntegrationEventHandler<TestPaymentProcessedEventHandler>();
builder.Services.AddIntegrationEventHandler<TestNotificationSendEventHandler>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// Configure EventBus subscriptions
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<UserRegisteredEvent, TestUserRegisteredEventHandler>();
    eventBus.Subscribe<AppointmentCreatedEvent, TestAppointmentCreatedEventHandler>();
    eventBus.Subscribe<PaymentProcessedEvent, TestPaymentProcessedEventHandler>();

    // Subscribe with routing keys
    eventBus.Subscribe<NotificationSendEvent, TestNotificationSendEventHandler>("notification.email");
});

app.Run();
