# BookingCare Microservices Architecture

## Overview
This is a comprehensive healthcare booking system built using .NET microservices architecture with gRPC, HTTP APIs, Message Queue, API Gateway, and Service Discovery.

## Architecture Components

### Infrastructure Services
- **API Gateway**: Entry point for all client requests, handles routing and authentication
- **Service Discovery**: Service registry for dynamic service discovery
- **Message Bus**: Event-driven communication between services
- **Monitoring Stack**: 
  - **Prometheus**: Metrics collection and storage
  - **Grafana**: Visualization and dashboards
  - **Jaeger**: Distributed tracing
  - **Node Exporter**: System metrics

### Core Services (15 Microservices)
1. **AuthService** - Authentication and authorization
2. **UserService** - User account management
3. **DoctorService** - Doctor management and specialties
4. **ClinicService** - Hospital/clinic management
5. **AppointmentService** - Appointment booking and management
6. **PaymentService** - Payment processing and invoicing
7. **ReviewService** - Reviews and feedback
8. **ContentService** - Content management (blogs, FAQs)
9. **ServiceMedicalService** - Medical services catalog
10. **CommunicationService** - Chat and video consultation
11. **NotificationService** - Email/SMS notifications
12. **PromotionService** - Promotions and discounts
13. **FavoriteService** - User favorites management
14. **AnalyticsService** - Reports and analytics
15. **AIService** - AI-powered features

### Technology Stack
- **.NET 8.0**: Core framework
- **gRPC**: Inter-service communication
- **HTTP/REST**: External APIs
- **RabbitMQ/Azure Service Bus**: Message queuing
- **Consul**: Service discovery
- **Ocelot**: API Gateway
- **SQL Server**: Relational database
- **MongoDB**: Document database
- **Redis**: Caching and session management

### Database Strategy
- **SQL Server**: Auth, User, Doctor, Clinic, Appointment, Payment, Promotion services
- **MongoDB**: Review, Content, ServiceMedical, Communication, Notification, Analytics services
- **Redis**: Caching, session management, real-time data

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- SQL Server
- MongoDB
- Redis
- RabbitMQ

### Quick Start with Monitoring

#### Option 1: Start Full Stack with Monitoring
```bash
# Start all services including monitoring
./scripts/monitoring.sh start-full

# Access monitoring dashboards
# Grafana:    http://localhost:3000 (admin/admin123)
# Prometheus: http://localhost:9090
# Jaeger:     http://localhost:16686
```

#### Option 2: Start Only Monitoring Stack
```bash
# Start just monitoring services
./scripts/monitoring.sh start-monitoring

# Then start your application services separately
docker-compose up -d auth-service discount-service clinic-service
```

#### Option 3: Traditional Docker Compose
```bash
# Start all services
docker-compose up -d

# Or start specific services
docker-compose up -d sqlserver rabbitmq redis
```

### Monitoring and Observability

The system includes comprehensive monitoring and observability features:

#### Metrics (Prometheus + Grafana)
- **System Metrics**: CPU, memory, disk usage
- **Application Metrics**: Request rates, response times, error rates
- **Business Metrics**: Service-specific KPIs
- **Database Metrics**: Query performance, connection pools

#### Distributed Tracing (Jaeger)
- **Request Tracing**: End-to-end request tracking across services
- **Performance Analysis**: Identify bottlenecks and slow operations
- **Error Debugging**: Trace error propagation through the system

#### Dashboards
- **System Overview**: General health and performance metrics
- **Service Details**: Per-service performance and health
- **Infrastructure**: Database, message queue, and system metrics
- **Custom Dashboards**: Business-specific metrics and KPIs

#### Alerts
- High error rates (>5%)
- High response times (>2s)
- Service downtime
- Resource usage (CPU >80%, Memory >80%)
- Database connection issues

### Service Management

```bash
# Check service status
./scripts/monitoring.sh status

# View service logs
./scripts/monitoring.sh logs [service-name]

# Health check
./scripts/monitoring.sh health

# Stop services
./scripts/monitoring.sh stop-all
```

### Running the Application
1. Start infrastructure services (databases, message queue)
2. Run Service Discovery
3. Start individual microservices
4. Start API Gateway

### Development Guidelines

For detailed documentation on specific components:
- [Exception Handling](./src/Shared/BookingCare.Shared.Common/README.md)
- [Monitoring Setup](./monitoring/README.md)
- [API Versioning](./docs/api/API_VERSIONING_GUIDE.md)
- [Development Guide](./docs/development/DEVELOPMENT_GUIDE.md)

### Monitoring Integration for Services

To add monitoring to any service, follow these steps:

1. **Add monitoring to Program.cs**:
```csharp
using BookingCare.Shared.Common.Extensions;

// Add monitoring services
builder.Services.AddBookingCareMonitoring("YourServiceName", "1.0.0");
builder.Services.AddGlobalExceptionHandling();

// Configure middleware
app.UseBookingCareMonitoring();
app.UseGlobalExceptionHandling();
```

2. **Set environment variables**:
```bash
JAEGER_AGENT_HOST=jaeger
JAEGER_AGENT_PORT=6831
SERVICE_NAME=YourServiceName
SERVICE_VERSION=1.0.0
```

3. **Custom metrics in your code**:
```csharp
using BookingCare.Shared.Common.Extensions;

// Start custom trace
using var activity = MonitoringExtensions.StartActivity("BusinessOperation");
activity?.SetTag("user.id", userId);

// Record metrics
MonitoringExtensions.RecordRequest("/api/endpoint", "POST", 200);
MonitoringExtensions.RecordError("service-name", "error-type");
```

### Production Considerations

#### Security
- Change default Grafana credentials
- Configure proper authentication for monitoring endpoints
- Use HTTPS in production
- Secure Prometheus and Jaeger interfaces

#### Performance
- Configure appropriate retention policies
- Monitor monitoring system resource usage
- Use proper scrape intervals for metrics

#### Scalability
- Consider Prometheus federation for multiple clusters
- Use appropriate Jaeger storage backend (Elasticsearch, Cassandra)
- Configure proper resource limits for monitoring services
- Each service follows Clean Architecture principles
- Use dependency injection for all services
- Implement proper logging and monitoring
- Follow SOLID principles
- Use async/await for all I/O operations