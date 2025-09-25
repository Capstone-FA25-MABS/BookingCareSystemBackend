# Schedule Service API Documentation

## Overview

The Schedule Service provides comprehensive APIs for managing healthcare appointment scheduling. This document covers both REST API and gRPC service interfaces.

## Base URLs

- **REST API**: `http://localhost:6015/api/v1`
- **gRPC Service**: `http://localhost:6025`

## Authentication

All API endpoints require authentication. Include the JWT bearer token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## REST API Endpoints

### 1. Appointment Times Management

Appointment times represent available time slots for appointments.

#### Get All Appointment Times
```http
GET /api/v1/appointment-times
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "startTime": "09:00",
      "endTime": "09:30",
      "createdAt": "2024-01-15T10:00:00Z",
      "updatedAt": "2024-01-15T10:00:00Z"
    }
  ],
  "message": "Appointment times retrieved successfully"
}
```

#### Get Appointment Time by ID
```http
GET /api/v1/appointment-times/1
```

#### Create Appointment Time
```http
POST /api/v1/appointment-times
Content-Type: application/json

{
  "startTime": "14:00",
  "endTime": "14:30"
}
```

#### Update Appointment Time
```http
PUT /api/v1/appointment-times/1
Content-Type: application/json

{
  "startTime": "14:00",
  "endTime": "14:30"
}
```

#### Delete Appointment Time
```http
DELETE /api/v1/appointment-times/1
```

### 2. Schedule Patterns Management

Schedule patterns are templates that define common schedule types.

#### Get All Schedule Patterns
```http
GET /api/v1/schedule-patterns
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "FULL_DAY",
      "description": "Full day schedule from morning to evening",
      "slots": [
        {
          "id": 1,
          "startTime": "09:00",
          "endTime": "09:30"
        }
      ],
      "createdAt": "2024-01-15T10:00:00Z",
      "updatedAt": "2024-01-15T10:00:00Z"
    }
  ]
}
```

#### Create Schedule Pattern
```http
POST /api/v1/schedule-patterns
Content-Type: application/json

{
  "name": "MORNING_ONLY",
  "description": "Morning schedule only",
  "slotIds": [1, 2, 3]
}
```

**Valid Pattern Names:**
- `FULL_DAY`
- `MORNING_ONLY`
- `AFTERNOON_ONLY`
- `EVENING_ONLY`

### 3. Doctor Daily Schedules Management

Manage doctor availability by specific dates.

#### Get Doctor Schedules
```http
GET /api/v1/doctor-schedules?doctorId=123&startDate=2024-01-15&endDate=2024-01-30
```

**Query Parameters:**
- `doctorId` (optional): Filter by doctor ID
- `startDate` (optional): Start date range (YYYY-MM-DD)
- `endDate` (optional): End date range (YYYY-MM-DD)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "doctorId": 123,
      "scheduleDate": "2024-01-15",
      "patternId": 1,
      "pattern": {
        "id": 1,
        "name": "FULL_DAY",
        "description": "Full day schedule"
      },
      "createdAt": "2024-01-15T10:00:00Z",
      "updatedAt": "2024-01-15T10:00:00Z"
    }
  ]
}
```

#### Create Doctor Schedule
```http
POST /api/v1/doctor-schedules
Content-Type: application/json

{
  "doctorId": 123,
  "scheduleDate": "2024-01-15",
  "patternId": 1
}
```

### 4. Doctor Service Schedules Management

Manage doctor schedules for specific services.

#### Get Doctor Service Schedules
```http
GET /api/v1/doctor-service-schedules?doctorId=123&serviceId=456
```

#### Create Doctor Service Schedule
```http
POST /api/v1/doctor-service-schedules
Content-Type: application/json

{
  "doctorId": 123,
  "serviceId": 456,
  "dayOfWeek": 1,
  "patternId": 1
}
```

**Day of Week Values:**
- 0 = Sunday
- 1 = Monday
- 2 = Tuesday
- 3 = Wednesday
- 4 = Thursday
- 5 = Friday
- 6 = Saturday

### 5. Schedule Exceptions Management

Handle special cases and availability overrides.

#### Get Doctor Schedule Exceptions
```http
GET /api/v1/doctor-schedule-exceptions?doctorId=123&date=2024-01-15
```

#### Create Schedule Exception
```http
POST /api/v1/doctor-schedule-exceptions
Content-Type: application/json

{
  "doctorId": 123,
  "exceptionDate": "2024-01-15",
  "appointmentTimeId": 1,
  "exceptionType": "UNAVAILABLE",
  "isAvailable": false,
  "reason": "Emergency surgery"
}
```

**Exception Types:**
- `AVAILABLE` - Add availability outside normal schedule
- `UNAVAILABLE` - Remove availability from normal schedule
- `MODIFIED` - Change normal schedule timing

## gRPC Service Methods

### 1. Get Available Slots

Retrieve available time slots for a doctor on a specific date.

**Proto Definition:**
```protobuf
rpc GetAvailableSlots(GetAvailableSlotsRequest) returns (GetAvailableSlotsResponse);

message GetAvailableSlotsRequest {
  int32 doctor_id = 1;
  string date = 2; // Format: YYYY-MM-DD
  int32 service_id = 3; // Optional
}

message GetAvailableSlotsResponse {
  repeated AvailableSlot slots = 1;
}

message AvailableSlot {
  int32 appointment_time_id = 1;
  string start_time = 2;
  string end_time = 3;
  bool is_available = 4;
}
```

**Usage Example (C#):**
```csharp
var request = new GetAvailableSlotsRequest
{
    DoctorId = 123,
    Date = "2024-01-15",
    ServiceId = 456
};

var response = await grpcClient.GetAvailableSlotsAsync(request);
```

### 2. Create Doctor Schedule Exception

Create a schedule exception via gRPC.

**Proto Definition:**
```protobuf
rpc CreateDoctorScheduleException(CreateDoctorScheduleExceptionRequest) 
    returns (DoctorScheduleExceptionResponse);

message CreateDoctorScheduleExceptionRequest {
  int32 doctor_id = 1;
  string exception_date = 2;
  int32 appointment_time_id = 3;
  string exception_type = 4;
  bool is_available = 5;
  string reason = 6;
}
```

### 3. Get Doctor Schedule Exceptions

Retrieve doctor's schedule exceptions.

**Proto Definition:**
```protobuf
rpc GetDoctorScheduleExceptions(GetDoctorScheduleExceptionsRequest) 
    returns (GetDoctorScheduleExceptionsResponse);

message GetDoctorScheduleExceptionsRequest {
  int32 doctor_id = 1;
  string start_date = 2; // Optional
  string end_date = 3;   // Optional
}
```

## Error Responses

### REST API Error Format
```json
{
  "success": false,
  "data": null,
  "message": "Error description",
  "errors": [
    {
      "field": "fieldName",
      "message": "Field-specific error message"
    }
  ]
}
```

### Common HTTP Status Codes
- `200 OK` - Success
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict (e.g., duplicate schedule)
- `500 Internal Server Error` - Server error

### gRPC Error Codes
- `OK` - Success
- `INVALID_ARGUMENT` - Invalid request parameters
- `NOT_FOUND` - Resource not found
- `PERMISSION_DENIED` - Insufficient permissions
- `ALREADY_EXISTS` - Resource already exists
- `INTERNAL` - Server error

## Rate Limiting

API endpoints have rate limiting applied:
- **Standard endpoints**: 100 requests per minute
- **Bulk operations**: 10 requests per minute
- **gRPC calls**: 200 requests per minute

Rate limit headers are included in responses:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1642291200
```

## Caching

The service implements intelligent caching:

### Cache Headers
```
Cache-Control: public, max-age=300
ETag: "abc123"
Last-Modified: Mon, 15 Jan 2024 10:00:00 GMT
```

### Cache Invalidation
- Automatic invalidation on data updates
- Manual cache refresh via special endpoints
- Time-based expiration for dynamic data

## Data Validation

### Date Formats
- All dates must be in `YYYY-MM-DD` format
- Times must be in `HH:MM` format (24-hour)
- DateTime fields are in ISO 8601 format

### Field Constraints
- **StartTime/EndTime**: Must be valid time format
- **DoctorId**: Must be positive integer
- **ServiceId**: Must be positive integer
- **ScheduleDate**: Cannot be in the past
- **PatternName**: Must be one of predefined values

## Integration Examples

### JavaScript/TypeScript
```typescript
const scheduleApi = {
  baseUrl: 'http://localhost:6015/api/v1',
  
  async getDoctorSchedules(doctorId: number, startDate: string, endDate: string) {
    const params = new URLSearchParams({
      doctorId: doctorId.toString(),
      startDate,
      endDate
    });
    
    const response = await fetch(`${this.baseUrl}/doctor-schedules?${params}`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    return response.json();
  },
  
  async createDoctorSchedule(schedule: CreateScheduleRequest) {
    const response = await fetch(`${this.baseUrl}/doctor-schedules`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(schedule)
    });
    
    return response.json();
  }
};
```

### Python
```python
import requests
from datetime import datetime

class ScheduleApiClient:
    def __init__(self, base_url: str, token: str):
        self.base_url = base_url
        self.headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json'
        }
    
    def get_available_slots(self, doctor_id: int, date: str, service_id: int = None):
        params = {
            'doctorId': doctor_id,
            'date': date
        }
        if service_id:
            params['serviceId'] = service_id
            
        response = requests.get(
            f'{self.base_url}/doctor-schedules',
            params=params,
            headers=self.headers
        )
        return response.json()
    
    def create_schedule_exception(self, exception_data: dict):
        response = requests.post(
            f'{self.base_url}/doctor-schedule-exceptions',
            json=exception_data,
            headers=self.headers
        )
        return response.json()
```

## Performance Considerations

### Best Practices
1. **Use appropriate date ranges** when querying schedules
2. **Leverage caching** by including cache headers
3. **Batch operations** when possible
4. **Use gRPC** for high-frequency service-to-service calls
5. **Implement retry logic** with exponential backoff

### Query Optimization
- Always specify doctor_id when possible
- Limit date ranges to reasonable periods
- Use pagination for large result sets
- Consider using service_id filters

## Support

For API support and questions:
- Email: api-support@bookingcare.com
- Documentation: https://docs.bookingcare.com/schedule-service
- Status Page: https://status.bookingcare.com