# BookingCare.Services.Schedule

## Overview

The Schedule Service is a core microservice in the BookingCare System responsible for managing appointment schedules, time slots, doctor availability, and schedule patterns. It provides both REST API and gRPC interfaces for managing healthcare appointment scheduling.

## Features

- **Appointment Time Management**: Create and manage time slots for appointments
- **Schedule Patterns**: Define reusable schedule templates (FULL_DAY, MORNING_ONLY, etc.)
- **Doctor Daily Schedules**: Manage doctor availability by date and pattern
- **Doctor Service Schedules**: Configure service-specific schedules for doctors
- **Schedule Exceptions**: Handle special cases and availability overrides
- **Redis Caching**: High-performance caching for frequently accessed data
- **gRPC Communication**: Inter-service communication with other microservices
- **REST API**: External API for client applications

## Architecture

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   REST Controllers  │    │   gRPC Services     │    │   Service Layer     │
├─────────────────────┤    ├─────────────────────┤    ├─────────────────────┤
│ • AppointmentTimes  │    │ • ScheduleGrpcSvc   │    │ • ScheduleService   │
│ • SchedulePatterns  │    │ • Available Slots   │    │ • Business Logic    │
│ • DoctorSchedules   │    │ • Schedule Mgmt     │    │ • Cache Integration │
│ • ServiceSchedules  │    │ • Exception Mgmt    │    │ • Data Mapping      │
│ • Exceptions        │    │                     │    │                     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
           │                           │                           │
           └───────────────────────────┼───────────────────────────┘
                                       │
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│ Repository Layer    │    │   Entity Framework  │    │   External Systems  │
├─────────────────────┤    ├─────────────────────┤    ├─────────────────────┤
│ • ScheduleRepo      │    │ • ScheduleDbContext │    │ • Redis Cache       │
│ • CRUD Operations   │    │ • Entity Models     │    │ • SQL Server        │
│ • Async Methods     │    │ • Relationships     │    │ • Other Services    │
│ • Query Optimization│    │ • Migrations        │    │   (via gRPC)        │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
```

## Database Schema

### Core Tables

1. **appointment_times**: Time slots with start/end times
2. **schedule_patterns**: Reusable schedule templates
3. **schedule_pattern_slots**: Time slots for each pattern
4. **doctor_daily_schedules**: Doctor availability by date
5. **doctor_service_schedules**: Service-specific doctor schedules
6. **doctor_schedule_exceptions**: Override default schedules
7. **service_schedule_exceptions**: Service-level schedule exceptions

### Key Relationships

- Schedule patterns contain multiple time slots
- Doctor daily schedules reference schedule patterns
- Exceptions can override default schedules
- Service schedules link doctors to specific services

## Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1446;Database=MABS_Schedule;User Id=sa;Password=Schedule@1234!"
  }
}
```

### Redis Cache Configuration

```json
{
  "Cache": {
    "ConnectionString": "localhost:6379",
    "DatabaseNumber": 0,
    "KeyPrefix": "BCSF:",
    "DefaultExpiration": "01:00:00"
  }
}
```

### Service Ports

- **HTTP/REST API**: Port 6015 (HTTP/1.1 and HTTP/2)
- **gRPC**: Port 6025 (HTTP/2 only)

## API Endpoints

### REST API (v1)

#### Appointment Times
- `GET /api/v1/appointment-times` - Get all appointment times
- `GET /api/v1/appointment-times/{id}` - Get specific appointment time
- `POST /api/v1/appointment-times` - Create new appointment time
- `PUT /api/v1/appointment-times/{id}` - Update appointment time
- `DELETE /api/v1/appointment-times/{id}` - Delete appointment time

#### Schedule Patterns
- `GET /api/v1/schedule-patterns` - Get all schedule patterns
- `GET /api/v1/schedule-patterns/{id}` - Get specific pattern
- `POST /api/v1/schedule-patterns` - Create new pattern
- `PUT /api/v1/schedule-patterns/{id}` - Update pattern
- `DELETE /api/v1/schedule-patterns/{id}` - Delete pattern

#### Doctor Schedules
- `GET /api/v1/doctor-schedules` - Get doctor schedules (with filters)
- `GET /api/v1/doctor-schedules/{id}` - Get specific schedule
- `POST /api/v1/doctor-schedules` - Create doctor schedule
- `PUT /api/v1/doctor-schedules/{id}` - Update doctor schedule
- `DELETE /api/v1/doctor-schedules/{id}` - Delete doctor schedule

#### Doctor Service Schedules
- `GET /api/v1/doctor-service-schedules` - Get service schedules
- `POST /api/v1/doctor-service-schedules` - Create service schedule
- `PUT /api/v1/doctor-service-schedules/{id}` - Update service schedule
- `DELETE /api/v1/doctor-service-schedules/{id}` - Delete service schedule

#### Schedule Exceptions
- `GET /api/v1/doctor-schedule-exceptions` - Get exceptions
- `POST /api/v1/doctor-schedule-exceptions` - Create exception
- `PUT /api/v1/doctor-schedule-exceptions/{id}` - Update exception
- `DELETE /api/v1/doctor-schedule-exceptions/{id}` - Delete exception

### gRPC Services

#### Available Methods
- `GetAvailableSlots` - Get available time slots for booking
- `CreateDoctorScheduleException` - Create schedule exception
- `GetDoctorScheduleExceptions` - Get doctor's schedule exceptions

## Caching Strategy

### Cache Keys
- `schedule:appointment_times:all` - All appointment times (5 min TTL)
- `schedule:patterns:all` - All schedule patterns (30 min TTL)
- `schedule:doctor:{doctorId}:date:{date}` - Doctor's daily schedule (2 hours TTL)
- `schedule:doctor:{doctorId}:exceptions` - Doctor's exceptions (30 min TTL)
- `schedule:available_slots:{doctorId}:{date}:{serviceId}` - Available slots (5 min TTL)

### Cache Invalidation
- Automatic invalidation on data updates
- Pattern-based invalidation for related data
- TTL-based expiration for time-sensitive data

## Development

### Prerequisites
- .NET 8.0 SDK
- SQL Server (Docker container available)
- Redis Server
- Entity Framework Core Tools

### Running Locally

1. **Start Dependencies**:
   ```bash
   cd BookingCareSystemBackend
   docker-compose up -d
   ```

2. **Apply Database Migrations**:
   ```bash
   cd src/Services/BookingCare.Services.Schedule
   dotnet ef database update
   ```

3. **Run the Service**:
   ```bash
   dotnet run
   ```

### Database Migrations

**Create Migration**:
```bash
dotnet ef migrations add MigrationName
```

**Apply Migration**:
```bash
dotnet ef database update
```

**Remove Last Migration**:
```bash
dotnet ef migrations remove
```

## Testing

### Unit Tests
- Service layer business logic testing
- Repository pattern testing
- Cache integration testing

### Integration Tests
- API endpoint testing
- Database integration testing
- gRPC service testing

### Load Testing
- High-traffic scenario testing
- Cache performance testing
- Database query optimization

## Monitoring

### Health Checks
- Database connectivity
- Redis connectivity
- Service dependencies

### Logging
- Structured logging with Serilog
- Request/response logging
- Error tracking and alerting

### Metrics
- API response times
- Cache hit/miss ratios
- Database query performance
- gRPC call metrics

## Deployment

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6015 6025

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/BookingCare.Services.Schedule/BookingCare.Services.Schedule.csproj"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingCare.Services.Schedule.dll"]
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Application environment
- `ConnectionStrings__DefaultConnection` - Database connection
- `Cache__ConnectionString` - Redis connection
- `Cache__DatabaseNumber` - Redis database number

## Security

### Authentication
- JWT Bearer token validation
- API key authentication for gRPC

### Authorization
- Role-based access control
- Resource-based permissions
- Service-to-service authentication

### Data Protection
- Sensitive data encryption
- Audit logging
- Data retention policies

## Performance

### Optimizations
- Database indexing strategy
- Query optimization
- Connection pooling
- Redis caching layers

### Scalability
- Horizontal scaling support
- Load balancing ready
- Stateless service design
- Database sharding capability

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify SQL Server is running
   - Check connection string configuration
   - Ensure database exists and migrations are applied

2. **Redis Connection Errors**
   - Verify Redis server is running
   - Check Redis connection string
   - Validate Redis database number

3. **gRPC Communication Issues**
   - Verify HTTP/2 support is enabled
   - Check port configuration (6025)
   - Validate service registration

### Debugging
- Enable detailed logging in `appsettings.Development.json`
- Use Visual Studio debugger or VS Code
- Monitor Redis commands with `redis-cli monitor`
- Use SQL Server Profiler for database queries

## Contributing

### Code Standards
- Follow C# coding conventions
- Use async/await patterns consistently
- Implement proper error handling
- Add comprehensive XML documentation

### Pull Request Process
1. Create feature branch from `develop`
2. Implement changes with tests
3. Update documentation
4. Submit pull request for review

## Support

For questions, issues, or contributions, please contact the development team or create an issue in the project repository.