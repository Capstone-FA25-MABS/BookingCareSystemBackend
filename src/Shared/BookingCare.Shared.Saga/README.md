# BookingCare Shared Saga Orchestration

A comprehensive saga orchestration library for coordinating distributed transactions across microservices in the BookingCare system.

## Overview

The saga pattern is used to manage data consistency across microservices in distributed transaction scenarios. A saga is a sequence of local transactions where each transaction updates data within a single service. If a local transaction fails, the saga executes compensating transactions to undo the changes made by preceding transactions.

## Features

- **Orchestrator-based Saga Pattern**: Central coordinator manages saga execution
- **Compensating Actions**: Automatic rollback through compensation steps
- **Event-driven Integration**: Seamless integration with event bus
- **State Persistence**: Pluggable state store implementations
- **Retry Mechanisms**: Built-in retry policies with exponential backoff
- **Timeout Handling**: Global and step-level timeout configuration
- **Monitoring & Logging**: Comprehensive logging and event publishing
- **Background Processing**: Automatic processing of pending sagas

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Saga Manager  │───▶│ Saga Orchestrator│───▶│ Saga State Store│
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Event Bus     │    │   Saga Steps    │    │  Compensation   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Quick Start

### 1. Add Package Reference

```xml
<ProjectReference Include="..\..\Shared\BookingCare.Shared.Saga\BookingCare.Shared.Saga.csproj" />
```

### 2. Configuration

Add saga configuration to your `appsettings.json`:

```json
{
  "Saga": {
    "StateStoreType": "InMemory", // or "SqlServer"
    "ConnectionString": "", // required for SqlServer
    "ProcessingInterval": "00:01:00", // 1 minute
    "DefaultTimeout": "00:30:00" // 30 minutes
  }
}
```

### 3. Service Registration

In your `Program.cs`:

```csharp
using BookingCare.Shared.Saga.Extensions;
using BookingCare.Shared.Saga.Examples;

// Add saga orchestration
services.AddSagaOrchestration(configuration);

// Register saga definitions
services.AddSaga<BookingAppointmentSaga>();
services.AddSaga<UserRegistrationSaga>();

// Register saga steps (if using dependency injection in steps)
services.AddSagaStep<ValidateUserStep>();
services.AddSagaStep<ProcessPaymentStep>();

// Register saga event handlers
services.AddSagaEventHandler<AppointmentBookingSagaEventHandler>();
```

### 4. Creating a Saga

```csharp
public class BookingAppointmentSaga : SagaDefinitionBase
{
    public override string SagaName => "BookingAppointment";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(15);

    public BookingAppointmentSaga()
    {
        AddStep<ValidateUserStep>();
        AddStep<CheckDoctorAvailabilityStep>();
        AddStep<ReserveTimeSlotStep>();
        AddStep<ProcessPaymentStep>();
        AddStep<CreateAppointmentStep>();
        AddStep<SendConfirmationNotificationStep>();
    }
}
```

### 5. Creating Saga Steps

#### Compensatable Step

```csharp
public class ProcessPaymentStep : CompensatableSagaStepBase
{
    public override string StepName => "ProcessPayment";
    public override int Order => 4;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var amount = context.GetData<decimal>("Amount");
        
        // Process payment logic
        var transactionId = await _paymentService.ProcessPaymentAsync(amount);
        
        context.SetData("TransactionId", transactionId);
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var transactionId = context.GetData<string>("TransactionId");
        
        if (!string.IsNullOrEmpty(transactionId))
        {
            // Refund payment
            await _paymentService.RefundPaymentAsync(transactionId);
        }
        
        return Success();
    }
}
```

#### Non-compensatable Step

```csharp
public class SendNotificationStep : ExecutableSagaStepBase
{
    public override string StepName => "SendNotification";
    public override int Order => 5;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var email = context.GetData<string>("UserEmail");
        
        // Send notification (cannot be undone)
        await _notificationService.SendEmailAsync(email, "Appointment Confirmed");
        
        return Success();
    }
}
```

### 6. Starting a Saga

```csharp
[ApiController]
[Route("api/[controller]")]
public class AppointmentController : ControllerBase
{
    private readonly ISagaManager _sagaManager;

    public AppointmentController(ISagaManager sagaManager)
    {
        _sagaManager = sagaManager;
    }

    [HttpPost("book")]
    public async Task<IActionResult> BookAppointment(BookAppointmentRequest request)
    {
        var context = new SagaContext();
        context.SetData("UserId", request.UserId);
        context.SetData("DoctorId", request.DoctorId);
        context.SetData("AppointmentDate", request.AppointmentDate);
        context.SetData("Amount", request.Amount);

        var sagaId = await _sagaManager.StartSagaAsync<BookingAppointmentSaga>(context);

        return Ok(new { SagaId = sagaId });
    }
}
```

### 7. Event-driven Saga Execution

```csharp
public class AppointmentBookingSagaEventHandler : ISagaEventHandler<AppointmentBookingRequestedEvent>
{
    private readonly ISagaManager _sagaManager;

    public AppointmentBookingSagaEventHandler(ISagaManager sagaManager)
    {
        _sagaManager = sagaManager;
    }

    public async Task HandleAsync(AppointmentBookingRequestedEvent @event, SagaContext context, CancellationToken cancellationToken = default)
    {
        context.SetData("UserId", @event.UserId);
        context.SetData("DoctorId", @event.DoctorId);
        context.SetData("AppointmentDate", @event.AppointmentDate);
        context.SetData("Amount", @event.Amount);

        await _sagaManager.StartSagaAsync<BookingAppointmentSaga>(context, cancellationToken);
    }
}
```

## State Management

### In-Memory State Store
Suitable for development and testing:

```json
{
  "Saga": {
    "StateStoreType": "InMemory"
  }
}
```

### SQL Server State Store
For production environments:

```json
{
  "Saga": {
    "StateStoreType": "SqlServer",
    "ConnectionString": "Server=localhost;Database=BookingCare;Trusted_Connection=true;"
  }
}
```

## Monitoring and Events

The saga framework publishes events for monitoring:

- `SagaStartedEvent`
- `SagaCompletedEvent`
- `SagaFailedEvent`
- `SagaCompensationStartedEvent`
- `SagaCompensationCompletedEvent`
- `SagaStepCompletedEvent`
- `SagaStepFailedEvent`
- `SagaTimedOutEvent`

## Best Practices

### 1. Design for Idempotency
Ensure saga steps can be safely retried:

```csharp
public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
{
    var reservationId = context.GetData<string>("ReservationId");
    
    // Check if already processed
    if (!string.IsNullOrEmpty(reservationId))
    {
        return Success(); // Already processed
    }
    
    // Process the reservation
    reservationId = await _reservationService.CreateReservationAsync();
    context.SetData("ReservationId", reservationId);
    
    return Success();
}
```

### 2. Implement Proper Compensation

```csharp
public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
{
    var reservationId = context.GetData<string>("ReservationId");
    
    if (!string.IsNullOrEmpty(reservationId))
    {
        var cancelled = await _reservationService.CancelReservationAsync(reservationId);
        if (!cancelled)
        {
            return Failure("Failed to cancel reservation", shouldRetry: true);
        }
    }
    
    return Success();
}
```

### 3. Handle Timeouts

```csharp
public class LongRunningStep : CompensatableSagaStepBase
{
    public override TimeSpan Timeout => TimeSpan.FromMinutes(10); // Custom timeout
    
    // Implementation...
}
```

### 4. Use Meaningful Step Names

```csharp
public override string StepName => "ValidatePaymentMethod";
public override int Order => 1;
```

### 5. Context Data Management

```csharp
// Set data
context.SetData("PaymentAmount", 100.00m);
context.SetData("PaymentMethod", "CreditCard");

// Get data with type safety
var amount = context.GetData<decimal>("PaymentAmount");
var method = context.GetData<string>("PaymentMethod");

// Check if data exists
if (context.HasData("TransactionId"))
{
    // Handle existing transaction
}
```

## Error Handling

### Retry Configuration

```csharp
public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
{
    try
    {
        // Attempt operation
        await SomeExternalServiceCall();
        return Success();
    }
    catch (TransientException ex)
    {
        // Retry with backoff
        return Failure(ex.Message, ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
    }
    catch (PermanentException ex)
    {
        // Don't retry
        return Failure(ex.Message, ex, shouldRetry: false);
    }
}
```

## Testing

### Unit Testing Saga Steps

```csharp
[Test]
public async Task ProcessPaymentStep_Success_ReturnsSuccess()
{
    // Arrange
    var step = new ProcessPaymentStep();
    var context = new SagaContext();
    context.SetData("Amount", 100.00m);
    
    // Act
    var result = await step.ExecuteAsync(context);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsTrue(context.HasData("TransactionId"));
}
```

### Integration Testing

```csharp
[Test]
public async Task BookingAppointmentSaga_FullExecution_CompletesSuccessfully()
{
    // Arrange
    var saga = new BookingAppointmentSaga();
    var context = new SagaContext();
    // Set up context data
    
    // Act
    var result = await _orchestrator.ExecuteAsync(Guid.NewGuid(), context);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(SagaStatus.Completed, result.Status);
}
```

## Examples

The library includes several example sagas:

1. **BookingAppointmentSaga**: Coordinates appointment booking across multiple services
2. **UserRegistrationSaga**: Handles user registration process
3. **OrderProcessingSaga**: Manages order processing with payment and fulfillment

See the `Examples` folder for complete implementations.

## Contributing

When adding new saga patterns:

1. Create saga definition inheriting from `SagaDefinitionBase`
2. Implement saga steps inheriting from appropriate base classes
3. Add proper compensation logic
4. Include comprehensive logging
5. Write unit and integration tests
6. Update documentation

## Performance Considerations

- Use appropriate timeouts to prevent hanging sagas
- Implement efficient state persistence
- Monitor saga execution times
- Use background processing for better scalability
- Consider partitioning for high-volume scenarios

## Troubleshooting

### Common Issues

1. **Saga hangs**: Check step timeouts and external service availability
2. **Compensation fails**: Ensure idempotent compensation logic
3. **State not persisted**: Verify state store configuration
4. **Memory leaks**: Ensure proper cleanup of completed sagas

### Debugging

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "BookingCare.Shared.Saga": "Debug"
    }
  }
}
```


BookingCare.Shared.Saga/
├── Abstractions/          # ISaga, ISagaStep, ISagaOrchestrator interfaces
├── Core/                  # SagaDefinitionBase, SagaOrchestrator implementation
├── Models/                # SagaContext, SagaState, SagaStepResult models  
├── StateStore/            # InMemory and SQL Server persistence
├── Manager/               # SagaManager for event handling and coordination
├── Events/                # Saga-specific events and handlers
├── Extensions/            # DI registration extensions
├── Examples/              # Business process saga implementations
└── Database/              # SQL Server schema and setup scripts