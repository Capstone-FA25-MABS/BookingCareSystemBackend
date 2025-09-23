# gRPC Saga Integration Guide

## Overview

This guide demonstrates how to implement distributed transactions using the Saga pattern with gRPC communication between microservices. The example shows a user registration process that coordinates between Auth Service and User Service.

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │    │   Auth Service  │    │   User Service  │
│                 │    │                 │    │                 │
│ • Saga Manager  │◄──►│ • Account CRUD  │    │ • Profile CRUD  │
│ • Orchestrator  │    │ • gRPC Server   │    │ • gRPC Server   │
│ • REST API      │    │ • HTTP API      │    │ • HTTP API      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       ▲                       ▲
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                              gRPC
```

## Saga Flow: User Registration

```
1. CreateUserAccountGrpcStep
   ├── gRPC call to Auth Service
   ├── Creates user account + credentials
   └── Returns: AccountId, UserId, VerificationToken

2. CreateUserProfileGrpcStep  
   ├── gRPC call to User Service
   ├── Creates user profile + personal info
   └── Returns: ProfileId

3. SendVerificationEmailGrpcStep
   ├── gRPC call to Notification Service
   ├── Sends welcome + verification email
   └── Returns: EmailId, Status

On Failure: Automatic compensation in reverse order
```

## Implementation Steps

### 1. Service Setup

#### Auth Service (Port 6003 HTTP, 6013 gRPC)
```csharp
// Program.cs
builder.Services.AddGrpc();
builder.Services.AddSagaOrchestration(builder.Configuration);
builder.Services.AddGrpcSagaSteps();
builder.Services.AddSaga<UserRegistrationGrpcSaga>();

app.MapGrpcService<AuthGrpcService>();
```

#### User Service (Port 6014 HTTP, 6024 gRPC)
```csharp
// Program.cs
builder.Services.AddGrpc();

app.MapGrpcService<UserGrpcService>();
```

### 2. gRPC Proto Definitions

#### auth.proto
```protobuf
service AuthService {
  rpc CreateUserAccount(CreateUserAccountRequest) returns (CreateUserAccountResponse);
  rpc DeleteUserAccount(DeleteUserAccountRequest) returns (DeleteUserAccountResponse);
}

message CreateUserAccountRequest {
  string user_id = 1;
  string email = 2;
  string password = 3;
  string role = 4;
}

message CreateUserAccountResponse {
  bool success = 1;
  string message = 2;
  string account_id = 3;
  string verification_token = 4;
}
```

#### user.proto
```protobuf
service UserService {
  rpc CreateUserProfile(CreateUserProfileRequest) returns (CreateUserProfileResponse);
  rpc DeleteUserProfile(DeleteUserProfileRequest) returns (DeleteUserProfileResponse);
}

message CreateUserProfileRequest {
  string user_id = 1;
  string first_name = 2;
  string last_name = 3;
  string email = 4;
  string phone_number = 5;
}

message CreateUserProfileResponse {
  bool success = 1;
  string message = 2;
  string profile_id = 3;
}
```

### 3. Saga Steps Implementation

#### CreateUserAccountGrpcStep
```csharp
public class CreateUserAccountGrpcStep : CompensatableSagaStepBase
{
    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var authServiceUrl = _configuration["Services:Auth:GrpcUrl"];
        using var channel = GrpcChannel.ForAddress(authServiceUrl);
        var client = new AuthService.AuthServiceClient(channel);

        var request = new CreateUserAccountRequest
        {
            UserId = context.GetData<string>("UserId"),
            Email = context.GetData<string>("Email"),
            Password = context.GetData<string>("Password"),
            Role = context.GetData<string>("Role") ?? "patient"
        };

        var response = await client.CreateUserAccountAsync(request, cancellationToken: cancellationToken);

        if (!response.Success)
            return Failure(response.Message, shouldRetry: true);

        context.SetData("AccountId", response.AccountId);
        context.SetData("VerificationToken", response.VerificationToken);
        
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Delete the created account
        var authServiceUrl = _configuration["Services:Auth:GrpcUrl"];
        using var channel = GrpcChannel.ForAddress(authServiceUrl);
        var client = new AuthService.AuthServiceClient(channel);

        var request = new DeleteUserAccountRequest
        {
            UserId = context.GetData<string>("UserId"),
            AccountId = context.GetData<string>("AccountId")
        };

        var response = await client.DeleteUserAccountAsync(request, cancellationToken: cancellationToken);
        return response.Success ? Success() : Failure(response.Message);
    }
}
```

### 4. Saga Definition

```csharp
public class UserRegistrationGrpcSaga : SagaDefinitionBase
{
    public override string SagaName => "UserRegistrationGrpc";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(10);

    private void ConfigureSteps()
    {
        AddStep(_serviceProvider.GetRequiredService<CreateUserAccountGrpcStep>());
        AddStep(_serviceProvider.GetRequiredService<CreateUserProfileGrpcStep>());
        AddStep(_serviceProvider.GetRequiredService<SendVerificationEmailGrpcStep>());
    }
}
```

### 5. API Usage

#### Start User Registration
```bash
POST /api/userregistration/register
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "role": "patient"
}
```

#### Response
```json
{
  "sagaId": "123e4567-e89b-12d3-a456-426614174000",
  "message": "User registration process started successfully",
  "email": "john.doe@example.com",
  "userId": "456e7890-e89b-12d3-a456-426614174001"
}
```

#### Check Status
```bash
GET /api/userregistration/status/123e4567-e89b-12d3-a456-426614174000
```

```json
{
  "sagaId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Completed",
  "steps": [
    {
      "stepName": "CreateUserAccount",
      "status": "Completed",
      "completedAt": "2025-09-08T10:30:00Z"
    },
    {
      "stepName": "CreateUserProfile", 
      "status": "Completed",
      "completedAt": "2025-09-08T10:30:15Z"
    },
    {
      "stepName": "SendVerificationEmail",
      "status": "Completed", 
      "completedAt": "2025-09-08T10:30:30Z"
    }
  ]
}
```

## Configuration

### appsettings.json
```json
{
  "Saga": {
    "StateStoreType": "SqlServer",
    "ConnectionString": "Server=localhost;Database=BookingCare;Trusted_Connection=true;",
    "ProcessingInterval": "00:01:00",
    "DefaultTimeout": "00:10:00"
  },
  "Services": {
    "Auth": {
      "GrpcUrl": "https://localhost:6013"
    },
    "User": {
      "GrpcUrl": "https://localhost:6024"
    },
    "Notification": {
      "GrpcUrl": "https://localhost:6033"
    }
  }
}
```

## Error Handling & Compensation

### Failure Scenarios

1. **Auth Service Failure**: Saga stops, no compensation needed
2. **User Service Failure**: Compensates by deleting created account
3. **Notification Failure**: Compensates by deleting account and profile
4. **Network Timeout**: Automatic retry with exponential backoff
5. **Service Unavailable**: Retry up to 3 times, then compensate

### Compensation Flow
```
Failed at Step 3 (SendEmail)
├── Compensate Step 2: DeleteUserProfile()
├── Compensate Step 1: DeleteUserAccount()  
└── Saga Status: Compensated
```

## Testing

### Unit Tests
```csharp
[Test]
public async Task CreateUserAccountGrpcStep_Success_ReturnsAccountId()
{
    var step = new CreateUserAccountGrpcStep(_logger, _configuration);
    var context = new SagaContext();
    context.SetData("Email", "test@example.com");
    context.SetData("Password", "password123");
    
    var result = await step.ExecuteAsync(context);
    
    Assert.IsTrue(result.IsSuccess);
    Assert.IsTrue(context.HasData("AccountId"));
}
```

### Integration Tests
```csharp
[Test]
public async Task UserRegistrationSaga_FullFlow_CompletesSuccessfully()
{
    var request = new UserRegistrationRequest
    {
        Email = "integration@test.com",
        Password = "test123",
        FirstName = "Integration",
        LastName = "Test"
    };
    
    var context = UserRegistrationSagaFactory.CreateContext(request);
    var sagaId = await _sagaManager.StartSagaAsync<UserRegistrationGrpcSaga>(context);
    
    await WaitForSagaCompletion(sagaId, TimeSpan.FromMinutes(2));
    
    var status = await _orchestrator.GetSagaStatusAsync(sagaId);
    Assert.AreEqual(SagaStatus.Completed, status);
}
```

## Monitoring & Observability

### Logging
- Structured logging with correlation IDs
- Step-level execution tracking
- Performance metrics (duration, success rate)
- gRPC call tracing

### Metrics
- Saga completion rate
- Average execution time
- Compensation frequency
- Service availability

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<SagaManagerHealthCheck>("saga-manager")
    .AddCheck<GrpcServiceHealthCheck>("grpc-services");
```

## Production Considerations

### Security
- gRPC with TLS in production
- Service-to-service authentication
- Input validation and sanitization
- Secure credential handling

### Scalability
- Multiple saga orchestrator instances
- Load balancing for gRPC services
- Database connection pooling
- Async processing optimization

### Reliability
- Circuit breaker pattern for gRPC calls
- Retry policies with exponential backoff
- Dead letter queue for failed sagas
- Persistent state store (SQL Server/PostgreSQL)

### Deployment
```yaml
# docker-compose.yml
version: '3.8'
services:
  auth-service:
    image: bookingcare/auth-service
    ports:
      - "6003:6003"
      - "6013:6013"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      
  user-service:
    image: bookingcare/user-service  
    ports:
      - "6014:6014"
      - "6024:6024"
```

## Benefits

### ✅ Distributed Transaction Management
- Ensures data consistency across services
- Automatic compensation on failures
- Support for long-running processes

### ✅ High Performance
- gRPC binary protocol
- HTTP/2 multiplexing
- Efficient serialization

### ✅ Type Safety
- Strongly-typed gRPC contracts
- Compile-time validation
- Code generation from proto files

### ✅ Resilience
- Automatic retry mechanisms
- Circuit breaker protection
- Graceful degradation

### ✅ Observability
- Distributed tracing
- Comprehensive logging
- Performance monitoring
- Business process visibility

This implementation provides a robust foundation for distributed transactions in a microservices architecture, ensuring data consistency while maintaining high performance and reliability.