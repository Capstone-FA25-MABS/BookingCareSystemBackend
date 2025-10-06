# Schedule Service Exception Handling

## Overview
This document describes the custom exception handling implementation for the Schedule Service, which provides consistent API responses and proper error codes for all schedule-related operations.

## Problem Solved
Previously, the Schedule Service was using:
- `DoctorExceptions.DoctorNotFoundException` from the shared domain exceptions
- Generic `ArgumentException` for validation errors

This resulted in inconsistent error responses and made it difficult to distinguish between different types of schedule-related errors.

## Solution
Created schedule-specific exceptions in `BookingCare.Services.Schedule.Exceptions.ScheduleExceptions` that follow the project's exception handling pattern.

## Exception Categories

### 1. Doctor Schedule Exceptions
- **`DoctorNotFoundException`** - When doctor is not found in schedule context
- **`DoctorNotAvailableException`** - When doctor is inactive or unavailable for scheduling
- **`DoctorScheduleConflictException`** - When doctor schedule already exists for a date
- **`DoctorDailyScheduleNotFoundException`** - When daily schedule is not found

### 2. Schedule Exception Management
- **`ScheduleExceptionNotFoundException`** - When schedule exception is not found
- **`ScheduleExceptionConflictException`** - When schedule exception conflicts with existing ones

### 3. Clinic Schedule Exceptions
- **`ClinicNotFoundException`** - When clinic is not found in schedule context
- **`ClinicExceptionNotFoundException`** - When clinic exception is not found

### 4. Service Schedule Exceptions
- **`ServiceNotFoundException`** - When medical service is not found in schedule context
- **`ServiceNotAvailableException`** - When medical service is inactive or unavailable
- **`ServiceScheduleNotFoundException`** - When service schedule is not found

### 5. Schedule Pattern Exceptions
- **`InvalidSchedulePatternException`** - When schedule pattern is invalid
- **`InvalidAppointmentTimeException`** - When appointment time is invalid
- **`InvalidScheduleDateException`** - When schedule date is invalid

### 6. Schedule Validation Exceptions
- **`ScheduleValidationException`** - General schedule validation errors
- **`ScheduleBusinessException`** - Business rule violations

## API Response Format

When these exceptions are thrown, the global exception handler will return consistent API responses:

### Before (Using DoctorExceptions.DoctorNotFoundException)
```json
{
  "isSuccessful": false,
  "message": "Doctor with identifier 'a38c0460-25c2-41f4-82b9-4233251ca0d8' was not found.",
  "data": null,
  "errors": null,
  "statusCode": 404,
  "timestamp": "2025-10-01T10:30:00Z"
}
```

### After (Using Schedule-Specific DoctorNotFoundException)
```json
{
  "isSuccessful": false,
  "message": "Doctor with identifier 'a38c0460-25c2-41f4-82b9-4233251ca0d8' was not found.",
  "data": null,
  "errors": ["SCHEDULE_DOCTOR_NOT_FOUND"],
  "statusCode": 404,
  "timestamp": "2025-10-01T10:30:00Z"
}
```

## Usage Examples

### Doctor Not Found
```csharp
throw DoctorNotFoundException.WithId(doctorId);
```

### Doctor Not Available
```csharp
throw DoctorNotAvailableException.WithId(doctorId);
```

### Service Not Available
```csharp
throw ServiceNotAvailableException.WithId(serviceId);
```

### Invalid Schedule Pattern
```csharp
throw InvalidSchedulePatternException.WithPattern(pattern, "Pattern contains overlapping time slots");
```

### Schedule Validation Error
```csharp
var errors = new List<ValidationError>
{
    new("DoctorId", "Doctor ID is required", null),
    new("ScheduleDate", "Schedule date cannot be in the past", scheduleDate)
};
throw new ScheduleValidationException(errors);
```

## Error Codes

All schedule-specific exceptions use descriptive error codes:

| Exception | Error Code | HTTP Status |
|-----------|------------|-------------|
| DoctorNotFoundException | SCHEDULE_DOCTOR_NOT_FOUND | 404 |
| DoctorNotAvailableException | SCHEDULE_DOCTOR_NOT_AVAILABLE | 400 |
| DoctorScheduleConflictException | SCHEDULE_DOCTOR_CONFLICT | 409 |
| ServiceNotFoundException | SCHEDULE_SERVICE_NOT_FOUND | 404 |
| ServiceNotAvailableException | SCHEDULE_SERVICE_NOT_AVAILABLE | 400 |
| InvalidSchedulePatternException | INVALID_SCHEDULE_PATTERN | 400 |
| ScheduleValidationException | SCHEDULE_VALIDATION_ERROR | 400 |

## Benefits

1. **Consistent Error Responses**: All exceptions follow the same pattern and return structured API responses
2. **Better Error Identification**: Specific error codes make it easier for clients to handle different error scenarios
3. **Improved Debugging**: Detailed error information with context (DoctorId, ServiceId, etc.)
4. **Type Safety**: Compile-time checking ensures proper exception usage
5. **Documentation**: Clear separation of concerns with well-documented exception types

## Integration with Global Exception Handler

The exceptions automatically integrate with the project's global exception handling system:
- `GlobalExceptionFilter` catches and formats exceptions in controllers
- `GlobalExceptionMiddleware` provides additional middleware-level handling
- Consistent logging with correlation IDs and context information

## Testing

To test the new exception handling:

1. **Doctor Not Found Scenario**:
   ```bash
   curl -X POST "https://localhost:6015/api/v1/doctor-schedules" \
   -H "Content-Type: application/json" \
   -d '{
     "doctorId": "00000000-0000-0000-0000-000000000000",
     "scheduleDate": "2025-10-01",
     "schedulePatterns": ["PATTERN_1"]
   }'
   ```

2. **Invalid Service Scenario**:
   ```bash
   curl -X GET "https://localhost:6015/api/v1/doctor-schedules/{doctorId}/available-slots?date=2025-10-01&serviceId=00000000-0000-0000-0000-000000000000"
   ```

Both should now return properly formatted API responses with schedule-specific error codes.