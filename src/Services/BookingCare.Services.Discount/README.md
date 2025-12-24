# BookingCare Discount Service

## Overview

The Discount Service manages promotional codes and discount logic for the BookingCare healthcare booking system. It provides comprehensive discount management capabilities including creation, validation, usage tracking, and expiration handling.

## Features

### Core Functionality
- **Discount Management**: Create, update, delete, and query discounts
- **Real-time Validation**: Validate discount codes before application
- **Usage Tracking**: Track and limit discount usage counts
- **Automatic Expiration**: Background service to automatically expire outdated discounts
- **Flexible Applicability**: Discounts can apply to all services, specific specialties, or individual doctors

### Discount Types
- **Fixed Amount**: Deduct a specific monetary value
- **Percentage**: Apply a percentage-based discount (with validation to prevent >100%)

### Applicability Scope
- **ALL**: Applies to any appointment in the clinic
- **SPECIALTY**: Applies only to specific medical specialties
- **DOCTOR**: Applies only to specific doctors

### Business Rules
- Unique discount codes per system
- Date-based validity (start/end dates)
- Optional usage limits with automatic tracking
- Automatic status management (ACTIVE/INACTIVE/EXPIRED)

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
- **REST API**: External client communication (port 6007)
- **gRPC**: Internal microservice communication (port 6017)
- **Saga Orchestration**: Distributed transaction support

### Database Schema
Based on the provided DDL:
```sql
CREATE TABLE discounts (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX),
    clinic_id BIGINT NOT NULL,
    specialty_id BIGINT,
    doctor_id BIGINT,
    applicable_to VARCHAR(20) DEFAULT 'ALL',
    amount DECIMAL(10, 2) NOT NULL,
    discount_type VARCHAR(20) NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    max_uses INT,
    uses_count INT DEFAULT 0,
    status VARCHAR(10) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);
```

## API Endpoints

### REST API (Port 6007)

#### Discount Management
- `GET /api/discounts/{id}` - Get discount by ID
- `GET /api/discounts/by-code/{code}` - Get discount by code
- `GET /api/discounts` - Get discounts with filtering and pagination
- `POST /api/discounts` - Create new discount
- `PUT /api/discounts/{id}` - Update existing discount
- `DELETE /api/discounts/{id}` - Delete discount

#### Discount Operations
- `POST /api/discounts/validate` - Validate discount code
- `POST /api/discounts/use` - Use discount code (increment usage)
- `POST /api/discounts/revert` - Revert discount usage
- `POST /api/discounts/calculate` - Calculate discount amount

#### Administrative Operations
- `PATCH /api/discounts/{id}/activate` - Activate discount
- `PATCH /api/discounts/{id}/deactivate` - Deactivate discount
- `POST /api/discounts/update-expired` - Update expired discounts

#### Query Operations
- `GET /api/discounts/clinic/{clinicId}/active` - Get active discounts for clinic
- `GET /api/discounts/applicable` - Get applicable discounts

### gRPC API (Port 6017)

#### Service Definition
```protobuf
service DiscountService {
  rpc ValidateDiscount (ValidateDiscountRequest) returns (ValidateDiscountResponse);
  rpc UseDiscount (UseDiscountRequest) returns (UseDiscountResponse);
  rpc RevertDiscountUsage (RevertDiscountUsageRequest) returns (RevertDiscountUsageResponse);
  rpc CalculateDiscountAmount (CalculateDiscountAmountRequest) returns (CalculateDiscountAmountResponse);
  rpc GetApplicableDiscounts (GetApplicableDiscountsRequest) returns (GetApplicableDiscountsResponse);
  rpc IsDiscountValid (IsDiscountValidRequest) returns (IsDiscountValidResponse);
}
```

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BookingCareDiscountDb;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### Service Ports
- **REST API**: 6007 (HTTP/1.1 and HTTP/2)
- **gRPC**: 6017 (HTTP/2)

## Usage Examples

### Creating a Discount
```json
POST /api/discounts
{
  "code": "NEWPATIENT20",
  "name": "New Patient Discount",
  "description": "20% off for new patients",
  "clinicId": 1,
  "applicableTo": "ALL",
  "amount": 20.00,
  "discountType": "PERCENTAGE",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "maxUses": 1000,
  "status": "ACTIVE"
}
```

### Validating a Discount
```json
POST /api/discounts/validate
{
  "code": "NEWPATIENT20",
  "clinicId": 1,
  "totalAmount": 100.00
}
```

### Using a Discount
```json
POST /api/discounts/use
{
  "code": "NEWPATIENT20",
  "clinicId": 1,
  "totalAmount": 100.00
}
```

## Error Handling

The service implements comprehensive error handling with custom exceptions:
- `DiscountNotFoundException`: Discount not found
- `DiscountValidationException`: Validation failures
- `DiscountBusinessException`: Business rule violations
- `DiscountConflictException`: Conflict errors (e.g., duplicate codes)

## Background Services

### Discount Expiration Service
- Runs every hour to check for expired discounts
- Automatically updates status from ACTIVE to EXPIRED
- Logs expiration activities for audit purposes

## Integration with Other Services

### Saga Orchestration Support
The service is designed to participate in distributed transactions using the Saga pattern:
- **Appointment Creation**: Validate and reserve discount usage
- **Payment Processing**: Apply discount and increment usage
- **Cancellation**: Revert discount usage if appointment is cancelled

### gRPC Communication
Other services can integrate using the gRPC client:
```csharp
// Example: Validate discount from Order Service
var client = new DiscountService.DiscountServiceClient(channel);
var response = await client.ValidateDiscountAsync(new ValidateDiscountRequest
{
    Code = "NEWPATIENT20",
    ClinicId = 1,
    TotalAmount = 100.00
});
```

## 🐳 Docker Deployment

### Quick Start with Docker

The Discount Service is fully containerized and can be deployed using Docker in multiple ways:

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
docker build -t bookingcare/discount-service:latest -f Dockerfile ../../../

# Run container (requires external database)
docker run -d \
  --name discount-service \
  -p 6007:6007 -p 6017:6017 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1434;Database=MABS_Discount;User Id=sa;Password=Discount123!;TrustServerCertificate=true;" \
  bookingcare/discount-service:latest
```

### Services and Ports

| Service | HTTP Port | gRPC Port | Database Port |
|---------|-----------|-----------|---------------|
| Discount Service | 6007 | 6017 | - |
| SQL Server | - | - | 1434 |

### Health Checks

```bash
# Service health
curl http://localhost:6007/api/discounts/health

# Container status
docker ps --filter "name=discount-service"

# Service logs
docker-compose logs -f discount-service
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

Copy `.env.template` to `.env` and customize:

```bash
# Database configuration
DISCOUNT_DB_PASSWORD=YourStrongPassword123!
ASPNETCORE_ENVIRONMENT=Development

# Service ports
DISCOUNT_SERVICE_HTTP_PORT=6007
DISCOUNT_SERVICE_GRPC_PORT=6017
```

### Docker Documentation

For comprehensive Docker deployment instructions, see:
- **[Docker Deployment Guide](./docs/DOCKER_DEPLOYMENT_GUIDE.md)** - Complete deployment documentation
- **[API Testing Guide](./docs/API_TESTING_GUIDE.md)** - API testing with containerized service

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
- Swagger UI: `http://localhost:6007/swagger`
- Health Check: `http://localhost:6007/health`

## Testing

### Unit Tests
Located in `tests/` directory with comprehensive coverage of:
- Business logic validation
- Discount calculation algorithms
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
- Rate limiting on discount validation endpoints

### Business Logic Security
- Prevents percentage discounts > 100%
- Validates date ranges and business rules
- Tracks and limits usage to prevent abuse

## Performance Optimizations

### Database Optimizations
- Indexed code column for fast lookups
- Efficient query patterns
- Connection pooling

### Caching Strategy
- In-memory caching for frequently accessed discounts
- Cache invalidation on discount updates
- Distributed caching for multi-instance deployments

## Deployment

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6007 6017

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "BookingCare.Services.Discount.dll"]
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
