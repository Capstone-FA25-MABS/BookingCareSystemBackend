# BookingCare Doctor Service

## Overview

The Doctor Service manages healthcare professionals and their schedules within the BookingCare healthcare booking system. It provides comprehensive doctor management capabilities including profile management, schedule handling, availability tracking, and appointment coordination.

## Features

### Core Functionality
- **Doctor Management**: Create, update, delete, and query doctor profiles
- **Specialty Management**: Handle medical specialties and qualifications
- **Profile Management**: Comprehensive doctor information and credentials
- **Status Management**: Manage doctor active/inactive status

### Doctor Profile Features
- **Personal Information**: Name, contact details, and identification
- **Professional Details**: Medical license, qualifications, and experience
- **Specialty Information**: Medical specialties and subspecialties
- **Clinic Affiliations**: Associated clinics and practice locations
- **Availability Status**: Active, inactive, or on leave status



### Business Rules
- Unique doctor identification per system
- License validation and uniqueness
- Status management (ACTIVE/INACTIVE)

### Deployment Options
- **Standalone Application**: Traditional .NET deployment
- **Docker Container**: Containerized deployment with Docker
- **Docker Compose**: Full stack with database included
- **Production Ready**: Optimized builds with health checks and monitoring

## Architecture

### Technology Stack
- **.NET 8.0**: Latest .NET framework
- **Entity Framework Core**: Database ORM with SQL Server
- **gRPC**: Internal service communication
- **REST API**: External HTTP endpoints
- **AutoMapper**: Object mapping
- **Swagger/OpenAPI**: API documentation

### Communication Patterns
- **REST API**: External client communication (port 6008)
- **gRPC**: Internal microservice communication (port 6018)
- **Saga Orchestration**: Distributed transaction support

### Database Schema
Based on the provided DDL:
```sql
CREATE TABLE doctors (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    license_number VARCHAR(50) NOT NULL UNIQUE,
    specialty_id BIGINT NOT NULL,
    clinic_id BIGINT NOT NULL,
    experience_years INT NOT NULL,
    education NVARCHAR(MAX),
    certifications NVARCHAR(MAX),
    status VARCHAR(20) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


```

## API Endpoints

### REST API (Port 6008)

#### Doctor Management
- `GET /api/doctors/{id}` - Get doctor by ID
- `GET /api/doctors/by-license/{licenseNumber}` - Get doctor by license
- `GET /api/doctors` - Get doctors with filtering and pagination
- `POST /api/doctors` - Create new doctor
- `PUT /api/doctors/{id}` - Update existing doctor
- `DELETE /api/doctors/{id}` - Delete doctor



#### Administrative Operations
- `PATCH /api/doctors/{id}/activate` - Activate doctor
- `PATCH /api/doctors/{id}/deactivate` - Deactivate doctor
- `POST /api/doctors/{id}/leave` - Set doctor on leave

#### Query Operations
- `GET /api/doctors/clinic/{clinicId}` - Get doctors by clinic
- `GET /api/doctors/specialty/{specialtyId}` - Get doctors by specialty
- `GET /api/doctors/search` - Search doctors by criteria

### gRPC API (Port 6018)

#### Service Definition
```protobuf
service DoctorService {
  rpc GetDoctor (GetDoctorRequest) returns (GetDoctorResponse);
  rpc GetDoctorsByClinic (GetDoctorsByClinicRequest) returns (GetDoctorsByClinicResponse);
  rpc GetDoctorsBySpecialty (GetDoctorsBySpecialtyRequest) returns (GetDoctorsBySpecialtyResponse);
  rpc GetDoctorsByStatus (GetDoctorsByStatusRequest) returns (GetDoctorsByStatusResponse);
}
```

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BookingCareDoctorDb;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### Service Ports
- **REST API**: 6008 (HTTP/1.1 and HTTP/2)
- **gRPC**: 6018 (HTTP/2)

## Usage Examples

### Creating a Doctor
```json
POST /api/doctors
{
  "userId": 123,
  "licenseNumber": "MD123456",
  "specialtyId": 1,
  "clinicId": 1,
  "experienceYears": 5,
  "education": "Medical School, Residency",
  "certifications": "Board Certified",
  "status": "ACTIVE"
}
```



## Error Handling

The service implements comprehensive error handling with custom exceptions:
- `DoctorNotFoundException`: Doctor not found
- `DoctorValidationException`: Validation failures
- `DoctorBusinessException`: Business rule violations
- `DoctorConflictException`: Conflict errors (e.g., duplicate license numbers)

## Background Services

### Doctor Status Service
- Runs periodically to check doctor status updates
- Logs status change activities for audit purposes

## Integration with Other Services

### Saga Orchestration Support
The service is designed to participate in distributed transactions using the Saga pattern:
- **Appointment Creation**: Validate doctor status and existence
- **Doctor Updates**: Coordinate with other services for doctor information changes
- **Status Management**: Handle doctor status changes across the system

### gRPC Communication
Other services can integrate using the gRPC client:
```csharp
// Example: Get doctor information from Appointment Service
var client = new DoctorService.DoctorServiceClient(channel);
var response = await client.GetDoctorAsync(new GetDoctorRequest
{
    DoctorId = 1
});
```

## 🐳 Docker Deployment

### Quick Start with Docker

The Doctor Service is fully containerized and can be deployed using Docker in multiple ways:

#### Option 1: Automated Script (Recommended)
```bash
# Build and run with Docker Compose (includes database)
./docker-build.sh latest compose dev

# For production deployment
./docker-build.sh latest compose prod
```

#### Option 2: Docker Compose Manual
```bash
# Development with database
docker-compose up -d

# Production deployment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

#### Option 3: Standalone Container
```bash
# Build image
docker build -t bookingcare/doctor-service:latest -f Dockerfile ../../../

# Run container (requires external database)
docker run -d \
  --name doctor-service \
  -p 6008:6008 -p 6018:6018 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1435;Database=MABS_Doctor;User Id=sa;Password=Doctor123!;TrustServerCertificate=true;" \
  bookingcare/doctor-service:latest
```

### Services and Ports

| Service | HTTP Port | gRPC Port | Database Port |
|---------|-----------|-----------|---------------|
| Doctor Service | 6008 | 6018 | - |
| SQL Server | - | - | 1435 |

### Health Checks

```bash
# Service health
curl http://localhost:6008/health

# Container status
docker ps --filter "name=doctor-service"

# Service logs
docker-compose logs -f doctor-service
```

### Docker Management

```bash
# View service status
./docker-build.sh latest status

# Show logs
./docker-build.sh latest logs

# Run database migrations
./docker-build.sh latest migrate

# Stop all services
./docker-build.sh latest stop

# Clean up everything
./docker-build.sh latest clean

# Show help
./docker-build.sh latest help
```

### Environment Configuration

Copy `env.template` to `.env` and customize:

```bash
# Database configuration
DOCTOR_DB_PASSWORD=YourStrongPassword123!
ASPNETCORE_ENVIRONMENT=Development

# Service ports
DOCTOR_SERVICE_HTTP_PORT=6008
DOCTOR_SERVICE_GRPC_PORT=6018
```

## Development

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Running the Service
1. Clone the repository
2. Update connection strings in `appsettings.json`
3. Run database migrations: `dotnet ef database update`
4. Start the service: `dotnet run`

### API Documentation
- Swagger UI: `http://localhost:6008/swagger`
- Health Check: `http://localhost:6008/health`

## Testing

### Unit Tests
Located in `tests/` directory with comprehensive coverage of:
- Business logic validation
- Schedule management algorithms
- Repository operations
- Exception handling

### Integration Tests
- Database integration tests
- gRPC service tests
- End-to-end API tests

## Monitoring and Logging

### Structured Logging
- Request/response logging
- Business operation logging
- Error and exception logging
- Performance metrics

### Health Checks
- `/health` endpoint for service health
- Database connectivity checks
- Dependencies status monitoring

## Security Considerations

### Data Protection
- Input validation and sanitization
- SQL injection prevention through parameterized queries
- Rate limiting on doctor search endpoints

### Business Logic Security
- License number validation and uniqueness
- Access control for sensitive doctor information

## Performance Optimizations

### Database Optimizations
- Indexed license_number column for fast lookups
- Efficient query patterns for doctor lookups
- Connection pooling

### Caching Strategy
- In-memory caching for frequently accessed doctor profiles
- Cache invalidation on profile updates
- Distributed caching for multi-instance deployments

## Deployment

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6008 6018

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BookingCare.Services.Doctor.dll"]
```

### Environment Configuration
- Development: LocalDB
- Staging: Azure SQL Database
- Production: SQL Server with high availability

## Contributing

1. Follow the established code structure
2. Implement comprehensive tests
3. Update documentation for new features
4. Follow the naming conventions and patterns
5. Ensure all builds pass before submitting PRs

## License

This service is part of the BookingCare system and follows the project's licensing terms.
