# Saga Integration Guide for BookingCare Microservices

This guide explains how to integrate saga orchestration into each BookingCare microservice.

## Service Integration Overview

Each service can participate in sagas in three ways:
1. **Saga Orchestrator**: Manages saga execution (typically API Gateway or dedicated service)
2. **Saga Participant**: Implements saga steps for service-specific operations
3. **Event Handler**: Responds to saga events and manages local state

## Step-by-Step Integration

### 1. User Service Integration

```csharp
// Program.cs
services.AddSagaOrchestration(configuration);
services.AddSaga<UserRegistrationSaga>();
services.AddSagaStep<CreateUserAccountStep>();
services.AddSagaStep<GenerateAuthCredentialsStep>();
services.AddSagaEventHandler<UserRegistrationSagaEventHandler>();

// UserRegistrationSaga.cs
public class UserRegistrationSaga : SagaDefinitionBase
{
    public override string SagaName => "UserRegistration";
    
    public UserRegistrationSaga()
    {
        AddStep<ValidateUserDataStep>();
        AddStep<CreateUserAccountStep>();
        AddStep<GenerateAuthCredentialsStep>();
        AddStep<SendWelcomeNotificationStep>();
    }
}

// CreateUserAccountStep.cs
public class CreateUserAccountStep : CompensatableSagaStepBase
{
    private readonly IUserRepository _userRepository;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userData = context.GetData<UserRegistrationData>("UserData");
        
        try
        {
            var user = await _userRepository.CreateUserAsync(userData);
            context.SetData("UserId", user.Id);
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"User creation failed: {ex.Message}", ex);
        }
    }
    
    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var userId = context.GetData<string>("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            await _userRepository.DeleteUserAsync(userId);
        }
        return Success();
    }
}
```

### 2. Doctor Service Integration

```csharp
// DoctorAvailabilitySaga.cs
public class DoctorAvailabilitySaga : SagaDefinitionBase
{
    public override string SagaName => "DoctorAvailability";
    
    public DoctorAvailabilitySaga()
    {
        AddStep<ValidateDoctorStep>();
        AddStep<CheckScheduleConflictsStep>();
        AddStep<UpdateAvailabilityStep>();
        AddStep<NotifyRelatedAppointmentsStep>();
    }
}

// CheckScheduleConflictsStep.cs
public class CheckScheduleConflictsStep : CompensatableSagaStepBase
{
    private readonly IDoctorScheduleService _scheduleService;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var timeSlot = context.GetData<TimeSlot>("RequestedTimeSlot");
        
        var conflicts = await _scheduleService.CheckConflictsAsync(doctorId, timeSlot);
        
        if (conflicts.Any())
        {
            return Failure($"Schedule conflicts found: {string.Join(", ", conflicts.Select(c => c.AppointmentId))}");
        }
        
        context.SetData("ConflictsChecked", true);
        return Success();
    }
    
    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for conflict check
        return Success();
    }
}
```

### 3. Payment Service Integration

```csharp
// PaymentProcessingSaga.cs
public class PaymentProcessingSaga : SagaDefinitionBase
{
    public override string SagaName => "PaymentProcessing";
    
    public PaymentProcessingSaga()
    {
        AddStep<ValidatePaymentMethodStep>();
        AddStep<AuthorizePaymentStep>();
        AddStep<CapturePaymentStep>();
        AddStep<UpdateAccountingStep>();
        AddStep<SendPaymentReceiptStep>();
    }
}

// CapturePaymentStep.cs
public class CapturePaymentStep : CompensatableSagaStepBase
{
    private readonly IPaymentGateway _paymentGateway;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var authorizationId = context.GetData<string>("AuthorizationId");
        var amount = context.GetData<decimal>("Amount");
        
        try
        {
            var captureResult = await _paymentGateway.CapturePaymentAsync(authorizationId, amount);
            
            if (!captureResult.IsSuccess)
            {
                return Failure($"Payment capture failed: {captureResult.ErrorMessage}", 
                    shouldRetry: captureResult.IsRetryable);
            }
            
            context.SetData("TransactionId", captureResult.TransactionId);
            context.SetData("PaymentCaptured", true);
            
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Payment capture error: {ex.Message}", ex, shouldRetry: true);
        }
    }
    
    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var transactionId = context.GetData<string>("TransactionId");
        
        if (!string.IsNullOrEmpty(transactionId))
        {
            try
            {
                await _paymentGateway.RefundPaymentAsync(transactionId);
                context.SetData("PaymentRefunded", true);
            }
            catch (Exception ex)
            {
                return Failure($"Refund failed: {ex.Message}", ex, shouldRetry: true);
            }
        }
        
        return Success();
    }
}
```

### 4. Notification Service Integration

```csharp
// NotificationDeliverySaga.cs
public class NotificationDeliverySaga : SagaDefinitionBase
{
    public override string SagaName => "NotificationDelivery";
    
    public NotificationDeliverySaga()
    {
        AddStep<ValidateRecipientStep>();
        AddStep<PrepareNotificationContentStep>();
        AddStep<SendEmailNotificationStep>();
        AddStep<SendSMSNotificationStep>();
        AddStep<SendPushNotificationStep>();
        AddStep<LogDeliveryStatusStep>();
    }
}

// SendEmailNotificationStep.cs
public class SendEmailNotificationStep : CompensatableSagaStepBase
{
    private readonly IEmailService _emailService;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var recipientEmail = context.GetData<string>("RecipientEmail");
        var emailContent = context.GetData<EmailContent>("EmailContent");
        
        try
        {
            var messageId = await _emailService.SendEmailAsync(recipientEmail, emailContent);
            context.SetData("EmailMessageId", messageId);
            context.SetData("EmailSent", true);
            
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Email sending failed: {ex.Message}", ex, shouldRetry: true);
        }
    }
    
    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Email cannot be unsent, but we can log the compensation attempt
        var messageId = context.GetData<string>("EmailMessageId");
        if (!string.IsNullOrEmpty(messageId))
        {
            await _emailService.LogCompensationAttemptAsync(messageId);
        }
        return Success();
    }
}
```

### 5. Schedule Service Integration

```csharp
// ScheduleManagementSaga.cs
public class ScheduleManagementSaga : SagaDefinitionBase
{
    public override string SagaName => "ScheduleManagement";
    
    public ScheduleManagementSaga()
    {
        AddStep<ValidateScheduleRequestStep>();
        AddStep<CheckResourceAvailabilityStep>();
        AddStep<ReserveTimeSlotStep>();
        AddStep<UpdateCalendarStep>();
        AddStep<NotifyAffectedPartiesStep>();
    }
}

// ReserveTimeSlotStep.cs
public class ReserveTimeSlotStep : CompensatableSagaStepBase
{
    private readonly IScheduleRepository _scheduleRepository;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var timeSlot = context.GetData<TimeSlot>("TimeSlot");
        var appointmentId = context.GetData<string>("AppointmentId");
        
        try
        {
            var reservation = await _scheduleRepository.ReserveSlotAsync(doctorId, timeSlot, appointmentId);
            
            if (reservation == null)
            {
                return Failure("Time slot is no longer available");
            }
            
            context.SetData("ReservationId", reservation.Id);
            context.SetData("SlotReserved", true);
            
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Slot reservation failed: {ex.Message}", ex);
        }
    }
    
    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var reservationId = context.GetData<string>("ReservationId");
        
        if (!string.IsNullOrEmpty(reservationId))
        {
            await _scheduleRepository.ReleaseSlotAsync(reservationId);
            context.SetData("SlotReleased", true);
        }
        
        return Success();
    }
}
```

## Event Handler Implementation

### Cross-Service Event Handlers

```csharp
// In Appointment Service
public class AppointmentEventHandler : 
    ISagaEventHandler<PaymentCompletedEvent>,
    ISagaEventHandler<DoctorAvailabilityChangedEvent>
{
    private readonly ISagaManager _sagaManager;
    private readonly IAppointmentService _appointmentService;
    
    public async Task HandleAsync(PaymentCompletedEvent @event, SagaContext context, CancellationToken cancellationToken = default)
    {
        // Update appointment status when payment is completed
        var appointmentId = context.GetData<string>("AppointmentId");
        await _appointmentService.ConfirmAppointmentAsync(appointmentId);
        
        // Start follow-up saga if needed
        context.SetData("PaymentTransactionId", @event.TransactionId);
        await _sagaManager.StartSagaAsync<AppointmentConfirmationSaga>(context, cancellationToken);
    }
    
    public async Task HandleAsync(DoctorAvailabilityChangedEvent @event, SagaContext context, CancellationToken cancellationToken = default)
    {
        // Check if any appointments need to be rescheduled
        context.SetData("DoctorId", @event.DoctorId);
        context.SetData("AffectedTimeSlots", @event.AffectedTimeSlots);
        
        await _sagaManager.StartSagaAsync<AppointmentReschedulingSaga>(context, cancellationToken);
    }
}
```

## Configuration per Service

### appsettings.json for each service

```json
{
  "Saga": {
    "StateStoreType": "SqlServer",
    "ConnectionString": "Server=localhost;Database=BookingCare;Trusted_Connection=true;",
    "ProcessingInterval": "00:01:00",
    "DefaultTimeout": "00:30:00"
  },
  "EventBus": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "booking_care_event_bus"
  }
}
```

## Service-Specific Saga Examples

### Auth Service - Authentication Saga

```csharp
public class AuthenticationSaga : SagaDefinitionBase
{
    public override string SagaName => "Authentication";
    
    public AuthenticationSaga()
    {
        AddStep<ValidateCredentialsStep>();
        AddStep<CheckAccountStatusStep>();
        AddStep<GenerateTokensStep>();
        AddStep<LogAuthenticationEventStep>();
        AddStep<UpdateLastLoginStep>();
    }
}
```

### Analytics Service - Data Processing Saga

```csharp
public class DataProcessingSaga : SagaDefinitionBase
{
    public override string SagaName => "DataProcessing";
    
    public DataProcessingSaga()
    {
        AddStep<ValidateDataSourceStep>();
        AddStep<ExtractDataStep>();
        AddStep<TransformDataStep>();
        AddStep<LoadDataStep>();
        AddStep<GenerateReportsStep>();
        AddStep<NotifyStakeholdersStep>();
    }
}
```

## Best Practices for Service Integration

### 1. Service Boundaries
- Each service should own its domain-specific saga steps
- Avoid cross-service dependencies in saga steps
- Use events for inter-service communication

### 2. Error Handling
```csharp
public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
{
    try
    {
        // Service-specific logic
        var result = await _domainService.ProcessAsync(data);
        return Success();
    }
    catch (DomainException ex)
    {
        // Business logic errors - don't retry
        return Failure(ex.Message, ex, shouldRetry: false);
    }
    catch (TransientException ex)
    {
        // Infrastructure errors - retry with backoff
        return Failure(ex.Message, ex, shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
    }
}
```

### 3. State Management
```csharp
// Store service-specific data in context
context.SetData($"{ServiceName}_ProcessedData", processedData);
context.SetData($"{ServiceName}_TransactionId", transactionId);

// Use consistent naming conventions
context.SetData("User_Id", userId);
context.SetData("Payment_TransactionId", transactionId);
context.SetData("Appointment_Id", appointmentId);
```

### 4. Monitoring and Observability
```csharp
public class MonitoredSagaStep : CompensatableSagaStepBase
{
    private readonly ILogger<MonitoredSagaStep> _logger;
    private readonly IMetrics _metrics;
    
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        using var activity = _metrics.StartActivity($"saga.step.{StepName}");
        
        try
        {
            _logger.LogInformation("Executing saga step {StepName} for saga {SagaId}", 
                StepName, context.SagaId);
            
            var result = await ExecuteStepLogic(context, cancellationToken);
            
            _metrics.Counter($"saga.step.{StepName}.success").Increment();
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga step {StepName} failed for saga {SagaId}", 
                StepName, context.SagaId);
            
            _metrics.Counter($"saga.step.{StepName}.failure").Increment();
            
            throw;
        }
    }
}
```

## Testing Saga Integration

### Unit Testing
```csharp
[Test]
public async Task CreateUserAccountStep_ValidData_ReturnsSuccess()
{
    // Arrange
    var step = new CreateUserAccountStep(_mockUserRepository.Object);
    var context = new SagaContext();
    context.SetData("UserData", new UserRegistrationData { Email = "test@test.com" });
    
    _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<UserRegistrationData>()))
        .ReturnsAsync(new User { Id = "user123" });
    
    // Act
    var result = await step.ExecuteAsync(context);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual("user123", context.GetData<string>("UserId"));
}
```

### Integration Testing
```csharp
[Test]
public async Task UserRegistrationSaga_EndToEnd_CompletesSuccessfully()
{
    // Arrange
    var sagaManager = _serviceProvider.GetRequiredService<ISagaManager>();
    var context = new SagaContext();
    context.SetData("UserData", validUserData);
    
    // Act
    var sagaId = await sagaManager.StartSagaAsync<UserRegistrationSaga>(context);
    
    // Wait for completion
    await WaitForSagaCompletion(sagaId, TimeSpan.FromMinutes(5));
    
    // Assert
    var sagaStatus = await _sagaOrchestrator.GetSagaStatusAsync(sagaId);
    Assert.AreEqual(SagaStatus.Completed, sagaStatus);
}
```

This integration guide provides a comprehensive approach to implementing saga orchestration across all BookingCare microservices while maintaining proper separation of concerns and service boundaries.
