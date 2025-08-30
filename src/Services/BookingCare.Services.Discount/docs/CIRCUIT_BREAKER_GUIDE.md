# Circuit Breaker Implementation Guide

## Overview

The BookingCare Discount Service now implements the Circuit Breaker pattern using Polly v8 for enhanced resilience and fault tolerance. This implementation provides protection against database failures, external service outages, and other transient errors.

## Architecture

### Components

1. **Circuit Breaker Services**
   - `ICircuitBreakerService` - Base interface
   - `IDatabaseCircuitBreakerService` - Database operations
   - `IHttpClientCircuitBreakerService` - HTTP client operations  
   - `IExternalApiCircuitBreakerService` - External API operations

2. **Implementation Classes**
   - `CircuitBreakerService` - Base implementation
   - `DatabaseCircuitBreakerService` - Database-specific
   - `HttpClientCircuitBreakerService` - HTTP-specific
   - `ExternalApiCircuitBreakerService` - External API-specific

3. **Configuration**
   - `CircuitBreakerOptions` - Individual circuit breaker settings
   - `ServiceCircuitBreakerOptions` - Service-wide settings

## Configuration

### appsettings.json

```json
{
  "CircuitBreaker": {
    "Database": {
      "HandledEventsAllowedBeforeBreaking": 5,
      "DurationOfBreak": "00:00:30",
      "SamplingDuration": 60,
      "MinimumThroughput": 5,
      "FailureThreshold": 0.5,
      "Timeout": "00:00:30",
      "RetryCount": 3,
      "BaseDelay": "00:00:00.500"
    },
    "HttpClient": {
      "HandledEventsAllowedBeforeBreaking": 3,
      "DurationOfBreak": "00:00:15",
      "SamplingDuration": 30,
      "MinimumThroughput": 3,
      "FailureThreshold": 0.3,
      "Timeout": "00:00:10",
      "RetryCount": 2,
      "BaseDelay": "00:00:00.250"
    },
    "ExternalApi": {
      "HandledEventsAllowedBeforeBreaking": 3,
      "DurationOfBreak": "00:00:20",
      "SamplingDuration": 45,
      "MinimumThroughput": 3,
      "FailureThreshold": 0.4,
      "Timeout": "00:00:15",
      "RetryCount": 2,
      "BaseDelay": "00:00:00.300"
    }
  }
}
```

### Configuration Parameters

- **HandledEventsAllowedBeforeBreaking**: Number of failures before circuit opens
- **DurationOfBreak**: How long circuit stays open
- **SamplingDuration**: Time window for failure rate calculation (seconds)
- **MinimumThroughput**: Minimum requests needed before circuit can open
- **FailureThreshold**: Failure rate threshold (0.0 to 1.0)
- **Timeout**: Operation timeout duration
- **RetryCount**: Number of retry attempts
- **BaseDelay**: Base delay for exponential backoff

## Service Integration

### Registration in Program.cs

```csharp
// Add circuit breaker services
builder.Services.AddCircuitBreaker(builder.Configuration);
```

### Usage in Services

```csharp
public class DiscountService : BaseService, IDiscountService
{
    private readonly IDatabaseCircuitBreakerService _databaseCircuitBreaker;

    public DiscountService(
        IDiscountRepository discountRepository,
        IDatabaseCircuitBreakerService databaseCircuitBreaker,
        // ... other dependencies
    ) : base(logger)
    {
        _databaseCircuitBreaker = databaseCircuitBreaker;
        // ... other assignments
    }

    public async Task<DiscountResponse> CreateDiscountAsync(CreateDiscountRequest request)
    {
        // Database operation with circuit breaker protection
        var createdDiscount = await _databaseCircuitBreaker.ExecuteAsync(
            () => _discountRepository.CreateAsync(discountEntity),
            "CreateDiscount");
        
        return _mapper.Map<DiscountResponse>(createdDiscount);
    }
}
```

## Polly Pipeline Features

### 1. Timeout Protection
- Prevents operations from hanging indefinitely
- Configurable per circuit breaker type
- Throws `TimeoutRejectedException` when exceeded

### 2. Retry with Exponential Backoff
- Automatic retry on transient failures
- Exponential backoff prevents overwhelming failing services
- Configurable retry count and base delay

### 3. Circuit Breaker
- Opens when failure threshold exceeded
- Half-open state for testing recovery
- Automatic recovery when service stabilizes

## Circuit Breaker States

### Closed (Normal Operation)
- All requests pass through
- Monitors failure rate
- Opens when threshold exceeded

### Open (Failing Fast)
- All requests immediately fail
- No calls to underlying service
- Transitions to half-open after break duration

### Half-Open (Testing Recovery)
- Limited requests allowed through
- Closes if requests succeed
- Opens again if requests fail

## Error Handling

### Exception Types

1. **ServiceUnavailableException** (503)
   - Circuit breaker is open
   - Service temporarily unavailable

2. **TimeoutException** (408)
   - Operation exceeded timeout
   - Request processing too slow

### Middleware Integration

The `CircuitBreakerMonitoringMiddleware` provides:
- Automatic error response formatting
- Request timing monitoring
- Slow request detection
- Comprehensive logging

## Monitoring and Health Checks

### Health Check Endpoints

```http
GET /api/health
GET /api/health/circuit-breaker
POST /api/health/circuit-breaker/reset
POST /api/health/circuit-breaker/reset/{type}
```

### Example Health Check Response

```json
{
  "CircuitBreakers": {
    "Database": {
      "State": "Closed",
      "Type": "Database Operations"
    },
    "HttpClient": {
      "State": "Closed", 
      "Type": "HTTP Client Operations"
    },
    "ExternalApi": {
      "State": "Closed",
      "Type": "External API Operations"
    }
  },
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

## Best Practices

### 1. Circuit Breaker Selection
- Use `IDatabaseCircuitBreakerService` for database operations
- Use `IHttpClientCircuitBreakerService` for HTTP requests
- Use `IExternalApiCircuitBreakerService` for third-party APIs

### 2. Operation Naming
```csharp
// Good: Descriptive operation names
await _databaseCircuitBreaker.ExecuteAsync(
    () => _repository.CreateAsync(entity),
    "CreateDiscount");

// Bad: Generic or missing names
await _databaseCircuitBreaker.ExecuteAsync(
    () => _repository.CreateAsync(entity));
```

### 3. Error Context
- Include meaningful error messages
- Log circuit breaker events
- Monitor circuit breaker states

### 4. Configuration Tuning
- Start with conservative settings
- Monitor failure rates and adjust
- Different settings for different operation types

## Testing Circuit Breakers

### Manual Testing

1. **Test Circuit Opening**
   - Stop database service
   - Make 5+ requests to trigger circuit
   - Verify 503 responses

2. **Test Recovery**
   - Restart database service
   - Wait for break duration
   - Verify circuit closes automatically

3. **Test Reset**
   - Use health endpoint to reset manually
   - Verify immediate recovery

### Load Testing

```bash
# Generate load to test circuit breaker behavior
for i in {1..10}; do
  curl -X POST http://localhost:6007/api/discounts \
    -H "Content-Type: application/json" \
    -d '{"code":"TEST'$i'","amount":10,"discountType":"FIXED"}' &
done
```

## Production Considerations

### 1. Monitoring
- Set up alerts for circuit breaker state changes
- Monitor failure rates and response times
- Track circuit breaker metrics

### 2. Configuration
- Tune settings based on production traffic
- Different settings for peak vs off-peak
- Consider service dependencies

### 3. Fallback Strategies
- Implement fallback responses
- Cache previous results where appropriate
- Graceful degradation of functionality

### 4. Coordination
- Coordinate circuit breaker settings across services
- Consider downstream impact
- Implement proper backpressure handling

## Troubleshooting

### Common Issues

1. **Circuit Opens Too Frequently**
   - Increase failure threshold
   - Increase minimum throughput
   - Check for real service issues

2. **Circuit Doesn't Open**
   - Verify failure threshold settings
   - Check minimum throughput requirements
   - Confirm error types are handled

3. **Slow Recovery**
   - Reduce break duration for faster recovery
   - Implement manual reset capabilities
   - Check half-open behavior

### Debugging

```csharp
// Enable detailed logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Check circuit breaker state
var state = _circuitBreaker.GetCircuitBreakerState();
_logger.LogInformation("Circuit breaker state: {State}", state);
```

## Integration with Shared Common

This implementation is part of the `BookingCare.Shared.Common` package and can be reused across all microservices in the BookingCare system. The pattern provides:

- Consistent error handling
- Standardized configuration
- Reusable components
- Centralized monitoring

## Future Enhancements

1. **Metrics Collection**
   - Integrate with Prometheus/Grafana
   - Custom metrics for circuit breaker events
   - Performance dashboards

2. **Advanced Policies**
   - Bulkhead isolation
   - Rate limiting
   - Adaptive timeout

3. **Service Mesh Integration**
   - Istio circuit breaker coordination
   - Distributed tracing
   - Service discovery integration
