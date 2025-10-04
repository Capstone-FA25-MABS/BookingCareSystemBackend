# gRPC Saga Pattern Demo

## Demo: User Registration with Distributed Transactions

This demo shows how the Saga pattern coordinates user registration across multiple microservices using gRPC communication.

### Architecture Overview

```
┌─────────────────┐    gRPC    ┌─────────────────┐    gRPC    ┌─────────────────┐
│   API Gateway   │ ────────► │   Auth Service  │ ────────► │   User Service  │
│                 │           │                 │           │                 │
│ • Saga Manager  │           │ • Create Account│           │ • Create Profile│
│ • Orchestrator  │           │ • Store Creds   │           │ • Store Details │
│ • Compensation  │           │ • Delete Account│           │ • Delete Profile│
└─────────────────┘           └─────────────────┘           └─────────────────┘
```

### User Registration Saga Flow

1. **Input**: User provides registration data
   ```json
   {
     "email": "john.doe@example.com",
     "password": "SecurePass123",
     "firstName": "John",
     "lastName": "Doe",
     "phoneNumber": "+1234567890"
   }
   ```

2. **Step 1: Create User Account** (Auth Service)
   - gRPC call: `CreateUserAccount()`
   - Creates login credentials
   - Generates verification token
   - **Compensation**: `DeleteUserAccount()`

3. **Step 2: Create User Profile** (User Service)
   - gRPC call: `CreateUserProfile()`
   - Stores personal information
   - Links to user account
   - **Compensation**: `DeleteUserProfile()`

4. **Step 3: Send Welcome Email** (Notification Service)
   - gRPC call: `SendVerificationEmail()`
   - Sends verification link
   - Records email status
   - **Compensation**: `SendCancellationEmail()`

### Success Scenario

```
POST /api/userregistration/register
{
  "email": "john@example.com",
  "password": "password123",
  "firstName": "John",
  "lastName": "Doe"
}

Response:
{
  "sagaId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "User registration started successfully",
  "userId": "123e4567-e89b-12d3-a456-426614174000"
}

Saga Execution:
✅ Step 1: Auth Service - Account created (AccountId: acc_001)
✅ Step 2: User Service - Profile created (ProfileId: prof_001)  
✅ Step 3: Notification Service - Email sent (EmailId: email_001)

Final State: SUCCESS - User fully registered
```

### Failure Scenario with Compensation

```
POST /api/userregistration/register
{
  "email": "jane@example.com",
  "password": "password456",
  "firstName": "Jane",
  "lastName": "Smith"
}

Saga Execution:
✅ Step 1: Auth Service - Account created (AccountId: acc_002)
✅ Step 2: User Service - Profile created (ProfileId: prof_002)
❌ Step 3: Notification Service - Email failed (Service unavailable)

Compensation Flow:
🔄 Compensate Step 2: User Service - Profile deleted (prof_002)
🔄 Compensate Step 1: Auth Service - Account deleted (acc_002)

Final State: COMPENSATED - No partial data left in system
```

### Key Benefits Demonstrated

#### 1. **Data Consistency**
- All services succeed together, or all changes are reverted
- No orphaned accounts without profiles
- No profiles without accounts

#### 2. **Fault Tolerance**
- Service failures automatically trigger compensation
- Network timeouts handled gracefully
- Partial failures don't corrupt data

#### 3. **Distributed Transaction Management**
- Coordinates across multiple databases
- Maintains ACID properties across services
- Long-running transaction support

#### 4. **High Performance**
- gRPC binary protocol for fast communication
- HTTP/2 multiplexing for efficient connections
- Async processing for better throughput

### Monitoring & Observability

```bash
# Check saga status
GET /api/userregistration/status/{sagaId}

Response:
{
  "sagaId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "totalDuration": "00:00:02.450",
  "steps": [
    {
      "stepName": "CreateUserAccount",
      "status": "Completed",
      "duration": "00:00:00.850",
      "startedAt": "2025-09-08T10:00:00Z",
      "completedAt": "2025-09-08T10:00:00.850Z"
    },
    {
      "stepName": "CreateUserProfile", 
      "status": "Completed",
      "duration": "00:00:00.750",
      "startedAt": "2025-09-08T10:00:01Z",
      "completedAt": "2025-09-08T10:00:01.750Z"
    },
    {
      "stepName": "SendVerificationEmail",
      "status": "Completed",
      "duration": "00:00:00.500",
      "startedAt": "2025-09-08T10:00:02Z",
      "completedAt": "2025-09-08T10:00:02.500Z"
    }
  ]
}
```

### Running the Demo

1. **Start Services**
   ```bash
   # Terminal 1: Auth Service
   dotnet run --project src/Services/BookingCare.Services.Auth
   # Listening on: HTTP 6003, gRPC 6013
   
   # Terminal 2: User Service  
   dotnet run --project src/Services/BookingCare.Services.User
   # Listening on: HTTP 6014, gRPC 6024
   ```

2. **Test Registration**
   ```bash
   curl -X POST http://localhost:6003/api/userregistration/register \
        -H "Content-Type: application/json" \
        -d '{
          "email": "demo@example.com",
          "password": "DemoPass123",
          "firstName": "Demo",
          "lastName": "User",
          "phoneNumber": "+1555000123"
        }'
   ```

3. **Monitor Execution**
   ```bash
   # Check status (replace with actual sagaId from response)
   curl http://localhost:6003/api/userregistration/status/{sagaId}
   ```

### Configuration for Demo

```json
{
  "Saga": {
    "StateStoreType": "InMemory",
    "ProcessingInterval": "00:00:30",
    "DefaultTimeout": "00:05:00"
  },
  "Services": {
    "Auth": {
      "GrpcUrl": "https://localhost:6013"
    },
    "User": {
      "GrpcUrl": "https://localhost:6024"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BookingCare.Shared.Saga": "Debug"
    }
  }
}
```

### Expected Log Output

```
[INFO] Starting user registration saga for email: demo@example.com
[INFO] Saga started: UserRegistrationGrpc, SagaId: 550e8400-e29b-41d4-a716-446655440000
[INFO] Executing step: CreateUserAccount (1/3)
[INFO] gRPC call to Auth Service: CreateUserAccount
[INFO] Account created successfully: AccountId=acc_003
[INFO] Executing step: CreateUserProfile (2/3)
[INFO] gRPC call to User Service: CreateUserProfile  
[INFO] Profile created successfully: ProfileId=prof_003
[INFO] Executing step: SendVerificationEmail (3/3)
[INFO] Email sent successfully: EmailId=email_003
[INFO] Saga completed successfully: SagaId=550e8400-e29b-41d4-a716-446655440000
```

This demo showcases the power of the Saga pattern with gRPC for building reliable, distributed transaction management in microservices architectures.