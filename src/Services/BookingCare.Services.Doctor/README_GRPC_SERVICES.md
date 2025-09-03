# gRPC Services Architecture

## Overview
The Doctor service has been refactored to use separate gRPC services for better separation of concerns and maintainability.

## Service Structure

### 1. DoctorGrpcService.cs
**Purpose**: Handles all Doctor-related gRPC operations
**Methods**:
- `CreateDoctor` - Create a new doctor
- `GetDoctorById` - Get doctor by ID
- `GetDoctorByEmail` - Get doctor by email
- `UpdateDoctor` - Update doctor information
- `DeleteDoctor` - Delete doctor
- `GetDoctors` - Get doctors with filtering and pagination
- `AssignPriceToDoctor` - Assign price to doctor
- `RemovePriceFromDoctor` - Remove price from doctor
- `GetDoctorPrices` - Get doctor's prices
- `ValidateDoctor` - Validate doctor existence

### 2. PositionGrpcService.cs
**Purpose**: Handles all Position-related gRPC operations
**Methods**:
- `CreatePosition` - Create a new position
- `GetPositionById` - Get position by ID
- `GetPositions` - Get positions with filtering and pagination

### 3. PriceGrpcService.cs
**Purpose**: Handles all Price-related gRPC operations
**Methods**:
- `CreatePrice` - Create a new price
- `GetPriceById` - Get price by ID
- `GetPrices` - Get prices with filtering and pagination

## Benefits of Separation

### 1. **Single Responsibility Principle**
- Each service handles only its domain-specific operations
- Easier to understand and maintain

### 2. **Independent Scaling**
- Can scale each service independently based on load
- Better resource utilization

### 3. **Cleaner Code Organization**
- Related functionality grouped together
- Easier to locate and modify specific features

### 4. **Better Testing**
- Can test each service independently
- More focused unit tests

### 5. **Easier Maintenance**
- Changes to one domain don't affect others
- Reduced risk of breaking changes

## Service Registration

All services are registered in `Program.cs`:

```csharp
// Map gRPC services
app.MapGrpcService<DoctorGrpcService>();
app.MapGrpcService<PositionGrpcService>();
app.MapGrpcService<PriceGrpcService>();
```

## Dependencies

Each service has its own dependencies:

### DoctorGrpcService
- `IDoctorService` - For doctor business logic
- `IMapper` - For object mapping
- `ILogger<DoctorGrpcService>` - For logging

### PositionGrpcService
- `IPositionService` - For position business logic
- `IMapper` - For object mapping
- `ILogger<PositionGrpcService>` - For logging

### PriceGrpcService
- `IPriceService` - For price business logic
- `IMapper` - For object mapping
- `ILogger<PriceGrpcService>` - For logging

## Protocol Buffer Definition

All services use the same `doctor.proto` file but implement different methods from the `DoctorService` interface. This allows for:

- Consistent API structure
- Shared message types
- Easy client generation

## Error Handling

Each service implements consistent error handling:
- Try-catch blocks around all operations
- Structured logging with context
- Consistent error response format
- Graceful error messages

## Performance Considerations

- Each service can be optimized independently
- Reduced memory footprint per service
- Better garbage collection patterns
- Independent caching strategies

## Future Enhancements

This architecture allows for:
- Microservice extraction (if needed)
- Independent deployment
- Service-specific middleware
- Domain-specific optimizations
- Easier integration with service mesh
