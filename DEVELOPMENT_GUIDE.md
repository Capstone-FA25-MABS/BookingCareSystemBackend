# BookingCare Microservices Development Guide

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Visual Studio 2022 / VS Code / JetBrains Rider
- SQL Server Management Studio (optional)
- MongoDB Compass (optional)

### Local Development Setup

1. **Clone the repository and navigate to the backend folder**
   ```bash
   cd BookingCareSystemBackend
   ```

2. **Run the setup script**
   ```bash
   ./scripts/setup-local.sh
   ```

3. **Build all services**
   ```bash
   ./scripts/build.sh
   ```

4. **Start services individually or using Docker Compose**
   ```bash
   # Option 1: Start all services with Docker
   docker-compose up
   
   # Option 2: Start services individually
   cd src/Infrastructure/BookingCare.Gateway
   dotnet run
   ```

## Architecture Overview

### Microservices Communication

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web Client    │────│   API Gateway    │────│   Auth Service  │
│  (React/Vue)    │    │    (Ocelot)      │    │   (JWT/OAuth)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
            ┌───────▼───┐  ┌────▼────┐  ┌──▼──────┐
            │User Service│  │Dr Service│  │ Clinic  │
            │ (SQL Svr)  │  │(SQL Svr)│  │ Service │
            └───────────┘  └─────────┘  └─────────┘
                    │           │           │
        ┌───────────┼───────────┼───────────┼───────────┐
        │           │           │           │           │
    ┌───▼───┐  ┌────▼────┐  ┌──▼──────┐ ┌─▼─────┐ ┌───▼───┐
    │Payment│  │Appointment│ │Review   │ │Content│ │  AI   │
    │Service│  │Service    │ │Service  │ │Service│ │Service│
    │       │  │(SQL+Redis)│ │(MongoDB)│ │(Mongo)│ │       │
    └───────┘  └─────────┘  └─────────┘ └───────┘ └───────┘
```

### Database Strategy

#### SQL Server Services (Strong Consistency)
- **AuthService**: User accounts, roles, permissions
- **UserService**: User profiles and management
- **DoctorService**: Doctor information, specialties, schedules
- **ClinicService**: Hospital/clinic data, subscriptions
- **AppointmentService**: Appointment bookings (main data)
- **PaymentService**: Payment transactions, invoices
- **PromotionService**: Discounts and promotional campaigns

#### MongoDB Services (Flexible Schema)
- **ReviewService**: User reviews and ratings
- **ContentService**: Blogs, FAQs, articles
- **ServiceMedicalService**: Medical service catalog
- **CommunicationService**: Chat messages, call logs
- **NotificationService**: Notification templates and logs
- **AnalyticsService**: Reports and analytics data

#### Redis Services (High-Speed Operations)
- **FavoriteService**: User favorites and preferences
- **Caching Layer**: Session data, frequently accessed data
- **Real-time Data**: Doctor availability, appointment slots

## Service Development Guidelines

### 1. Clean Architecture Structure

Each service follows this structure:
```
BookingCare.ServiceName/
├── Controllers/           # API Controllers
├── Services/             # Business Logic
│   ├── Interfaces/
│   └── Implementations/
├── Models/
│   ├── DTOs/            # Data Transfer Objects
│   └── Entities/        # Database Entities
├── Data/
│   ├── DbContext.cs     # Entity Framework Context
│   └── Configurations/ # Entity Configurations
├── Protos/              # gRPC Proto Files
├── Middleware/          # Custom Middleware
├── Extensions/          # Service Extensions
└── appsettings.json     # Configuration
```

### 2. Communication Patterns

#### HTTP/REST APIs
```csharp
[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetDoctor(Guid id)
    {
        // Implementation
    }
}
```

#### gRPC Services
```csharp
public class DoctorGrpcService : DoctorService.DoctorServiceBase
{
    public override async Task<GetDoctorResponse> GetDoctor(
        GetDoctorRequest request, 
        ServerCallContext context)
    {
        // Implementation
    }
}
```

#### Message Queue Events
```csharp
public class AppointmentCreatedEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime AppointmentDate { get; set; }
}
```

### 3. Database Configuration

#### SQL Server (Entity Framework)
```csharp
public class AuthDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
```

#### MongoDB
```csharp
public class ReviewService
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewService(IMongoDatabase database)
    {
        _reviews = database.GetCollection<Review>("reviews");
    }
}
```

#### Redis
```csharp
public class FavoriteService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public FavoriteService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
    }
}
```

### 4. Service Registration and Discovery

Each service registers itself with Consul:
```csharp
public static class ConsulExtensions
{
    public static IServiceCollection AddConsulServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSingleton<IConsulClient>(provider =>
        {
            var consulConfig = new ConsulClientConfiguration
            {
                Address = new Uri(configuration["Consul:Host"])
            };
            return new ConsulClient(consulConfig);
        });

        return services;
    }
}
```

### 5. Health Checks

Every service implements health checks:
```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AuthDbContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
```

## Testing Strategy

### Unit Tests
```csharp
[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private AuthenticationService _authService;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _authService = new AuthenticationService(_userRepositoryMock.Object);
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange, Act, Assert
    }
}
```

### Integration Tests
```csharp
[TestFixture]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        // Test against real database and services
    }
}
```

## Deployment

### Docker Deployment
Each service has its own Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookingCare.AuthService.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingCare.AuthService.dll"]
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: auth-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: auth-service
  template:
    metadata:
      labels:
        app: auth-service
    spec:
      containers:
      - name: auth-service
        image: bookingcare/auth-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## Monitoring and Logging

### Serilog Configuration
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("ServiceName", "AuthService")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
    .CreateLogger();
```

### Application Insights
```csharp
services.AddApplicationInsightsTelemetry(configuration["ApplicationInsights:ConnectionString"]);
```

## Security Considerations

### JWT Authentication
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });
```

### API Rate Limiting
```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", configure =>
    {
        configure.PermitLimit = 100;
        configure.Window = TimeSpan.FromMinutes(1);
    });
});
```

## Performance Optimization

### Caching Strategy
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});

// Usage
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetDoctors()
{
    // Implementation with caching
}
```

### Database Optimization
```csharp
// Use pagination
public async Task<PagedResult<Doctor>> GetDoctorsAsync(int page, int pageSize)
{
    var doctors = await _context.Doctors
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<Doctor>(doctors, totalCount, page, pageSize);
}

// Use projections
var doctorSummaries = await _context.Doctors
    .Select(d => new DoctorSummaryDto
    {
        Id = d.Id,
        Name = d.FirstName + " " + d.LastName,
        Specialty = d.Specialty.Name
    })
    .ToListAsync();
```

This architecture provides a solid foundation for building a scalable, maintainable healthcare booking system using modern .NET microservices practices.
