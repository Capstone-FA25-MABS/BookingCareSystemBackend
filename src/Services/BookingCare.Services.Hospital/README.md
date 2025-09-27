# BookingCare Hospital Service

## Overview

The Hospital Service manages healthcare institutions and their subscriptions within the BookingCare healthcare booking system. It provides comprehensive hospital management capabilities including profile management, subscription handling, specialty management, and institutional coordination.

## Features

### Core Functionality
- **Hospital Management**: Create, update, delete, and query hospital profiles
- **Subscription Management**: Handle hospital subscription plans and billing
- **Specialty Management**: Manage hospital specialties and services
- **Image Management**: Handle hospital images and media content
- **Status Management**: Manage hospital active/inactive status

### Hospital Profile Features
- **Institution Information**: Name, address, contact details, and identification
- **Service Details**: Available specialties, services, and capabilities
- **Subscription Plans**: Various subscription tiers with different features
- **Media Management**: Hospital images, backgrounds, and avatars
- **Status Tracking**: Active, inactive subscription management

### Subscription Features
- **Multiple Plans**: Basic, Standard, Professional, Enterprise, and Trial plans
- **Billing Cycles**: Monthly, yearly, and trial options
- **Feature Limits**: Doctor limits, appointment limits, storage quotas
- **Status Management**: Active, expired, cancelled, pending, trial statuses
- **Automatic Updates**: Subscription status updates based on dates

### Business Rules
- Unique hospital identification per system
- Email validation and uniqueness
- Subscription plan enforcement
- Status management (ACTIVE/INACTIVE)
- Automatic subscription expiry handling

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
- **REST API**: External client communication (port 6004)
- **gRPC**: Internal microservice communication (port 6014)
- **Service Integration**: Integration with Doctor service and other microservices

### Database Schema
Key entities include:
- **Hospitals**: Main hospital information
- **Subscription Plans**: Available subscription tiers
- **Hospital Subscriptions**: Active hospital subscriptions
- **Hospital Specialties**: Hospital-specialty relationships
- **Hospital Images**: Media content for hospitals

```csharp
public class HospitalEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; }
    public string Description { get; set; }
    public string? BackgroundUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BookingCare_Hospital;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### Service Ports
- **REST API**: 6004 (HTTP/1.1 and HTTP/2)
- **gRPC**: 6014 (HTTP/2)

### gRPC Client Configuration
```json
{
  "GrpcClients": {
    "Auth": {
      "Address": "http://localhost:6013"
    }
  }
}
```

## API Documentation

### REST Endpoints

#### Hospital Management
```http
GET    /api/v1.0/hospitals              # Get all hospitals with pagination
GET    /api/v1.0/hospitals/{id}         # Get hospital by ID
POST   /api/v1.0/hospitals              # Create new hospital
PUT    /api/v1.0/hospitals/{id}         # Update hospital
DELETE /api/v1.0/hospitals/{id}         # Delete hospital
GET    /api/v1.0/hospitals/search       # Search hospitals with filters
```

#### Subscription Management
```http
GET    /api/v1.0/subscription-plans     # Get all subscription plans
GET    /api/v1.0/subscription-plans/{id} # Get subscription plan by ID
POST   /api/v1.0/subscription-plans     # Create new subscription plan
PUT    /api/v1.0/subscription-plans/{id} # Update subscription plan

GET    /api/v1.0/hospital-subscriptions # Get hospital subscriptions
POST   /api/v1.0/hospital-subscriptions # Create new hospital subscription
PUT    /api/v1.0/hospital-subscriptions/{id} # Update subscription
```

### gRPC Services
```protobuf
service HospitalService {
  rpc GetHospital (GetHospitalRequest) returns (HospitalReply);
  rpc GetHospitalsList (GetHospitalsListRequest) returns (HospitalBasicListReply);
  rpc CreateHospital (CreateHospitalGrpcRequest) returns (HospitalReply);
}
```

## Usage Examples

### Creating a Hospital
```json
POST /api/v1.0/hospitals
{
  "accountId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Bệnh viện Đa khoa ABC",
  "address": "123 Đường ABC, Quận 1, TP.HCM",
  "phone": "028-3822-1234",
  "email": "info@bvdkabc.vn",
  "description": "Bệnh viện đa khoa với đội ngũ bác sĩ giàu kinh nghiệm",
  "specialtyIds": ["guid1", "guid2"]
}
```

### Getting Hospital with Details
```json
GET /api/v1.0/hospitals/{id}
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "accountId": "456e7890-e89b-12d3-a456-426614174001",
    "name": "Bệnh viện Đa khoa ABC",
    "address": "123 Đường ABC, Quận 1, TP.HCM",
    "phone": "028-3822-1234",
    "email": "info@bvdkabc.vn",
    "description": "Bệnh viện đa khoa với đội ngũ bác sĩ giàu kinh nghiệm",
    "status": "ACTIVE",
    "currentSubscription": {
      "planName": "Gói Tiêu Chuẩn",
      "status": "ACTIVE",
      "endDate": "2024-12-31T23:59:59Z"
    },
    "specialties": [...],
    "images": [...]
  }
}
```

## Error Handling

The service implements comprehensive error handling with custom exceptions:
- `HospitalNotFoundException`: Hospital not found
- `HospitalAlreadyExistsException`: Duplicate hospital
- `HospitalValidationException`: Validation failures
- `HospitalOperationException`: Operation failures
- `SubscriptionNotFoundException`: Subscription not found
- `SubscriptionExpiredException`: Expired subscription

## Database Scripts

The service includes comprehensive database scripts:

### Data Management
- `insert_sample_data.sql`: Insert sample hospitals and subscriptions
- `truncate_db.sql`: Clean database for fresh start
- `update_subscription_statuses.sql`: Update subscription statuses based on dates

### Analytics & Reporting
- `hospital_analytics.sql`: Comprehensive analytics and reports
  - Hospital overview statistics
  - Subscription plan popularity
  - Revenue analysis
  - Registration trends
  - Expiry alerts

### Backup & Maintenance
- `backup_and_restore.sql`: Database backup and restore procedures
  - Full database backup
  - Differential backup
  - Transaction log backup
  - Restore procedures
  - Backup history queries

## Integration with Other Services

### gRPC Communication
The Hospital service provides gRPC endpoints for other services:

```csharp
// Example: Get hospital information from Doctor Service
var client = new HospitalService.HospitalServiceClient(channel);
var response = await client.GetHospitalAsync(new GetHospitalRequest
{
    Id = hospitalId.ToString()
});
```

### Doctor Service Integration
The Hospital service is integrated with the Doctor service to provide hospital information when retrieving doctor details:

```csharp
// Doctor service calls Hospital service to get hospital details
var hospitalInfo = await _hospitalClient.GetHospitalAsync(request);
```

## 🐳 Docker Deployment

### Quick Start with Docker
```bash
# Build and run with Docker Compose
docker-compose up -d

# Build standalone container
docker build -t bookingcare/hospital-service:latest -f Dockerfile ../../../

# Run container
docker run -d \
  --name hospital-service \
  -p 6004:6004 -p 6014:6014 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=BookingCare_Hospital;User Id=sa;Password=Hospital123!;TrustServerCertificate=true;" \
  bookingcare/hospital-service:latest
```

### Services and Ports

| Service | HTTP Port | gRPC Port | Database Port |
|---------|-----------|-----------|---------------|
| Hospital Service | 6004 | 6014 | - |
| SQL Server | - | - | 1433 |

### Health Checks
```bash
# Service health
curl http://localhost:6004/health

# Container status
docker ps --filter "name=hospital-service"
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
- Swagger UI: `http://localhost:6004/swagger`
- Health Check: `http://localhost:6004/health`

## Testing

### Unit Tests
Comprehensive coverage of:
- Business logic validation
- Subscription management algorithms
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
- Rate limiting on hospital search endpoints

### Business Logic Security
- Email validation and uniqueness
- Access control for sensitive hospital information
- Subscription validation and enforcement

## Performance Optimizations

### Database Optimizations
- Indexed email column for fast lookups
- Efficient query patterns for hospital searches
- Connection pooling

### Caching Strategy
- In-memory caching for frequently accessed hospital profiles
- Cache invalidation on profile updates
- Distributed caching for multi-instance deployments

## Subscription Management

### Automatic Status Updates
The service includes automated subscription management:
- Daily status updates based on subscription dates
- Automatic expiry notifications
- Trial period management
- Billing cycle handling

### Subscription Plans
- **Basic Plan**: Small clinics (5 doctors, 100 appointments)
- **Standard Plan**: Medium clinics (15 doctors, 500 appointments)
- **Professional Plan**: Large hospitals (50 doctors, 2000 appointments)
- **Enterprise Plan**: Hospital systems (200 doctors, 10000 appointments)
- **Trial Plan**: 30-day free trial with limited features

## Deployment

### Production Deployment
```bash
# Build for production
dotnet build -c Release

# Publish application
dotnet publish -c Release -o ./publish

# Run in production
dotnet BookingCare.Services.Hospital.dll
```

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Production connection string"
GrpcClients__Auth__Address="http://auth-service:6013"
```

## Maintenance Tasks

### Daily Tasks
- Run subscription status updates
- Monitor expiring subscriptions
- Check system health and performance

### Weekly Tasks
- Review analytics reports
- Database maintenance and optimization
- Backup verification

### Monthly Tasks
- Comprehensive analytics review
- Subscription plan analysis
- Performance optimization review

---

## Support and Documentation

For additional support and documentation:
- API Documentation: Available at `/swagger` endpoint
- Health Monitoring: Available at `/health` endpoint
- Database Scripts: Located in `/Scripts` folder
- Integration Examples: See gRPC service definitions

**Version**: 1.0.0  
**Last Updated**: December 2024
