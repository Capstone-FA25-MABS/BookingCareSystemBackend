# BookingCare Microservices Project Structure

## Complete Project Structure

```
BookingCareSystemBackend/
├── BookingCareSystem.sln                          # Main solution file
├── README.md                                       # Project documentation
├── docker-compose.yml                             # Docker orchestration
├── .gitignore                                      # Git ignore rules
│
├── src/                                           # Source code
│   ├── Infrastructure/                            # Infrastructure services
│   │   ├── BookingCare.Gateway/                   # API Gateway (Ocelot)
│   │   │   ├── BookingCare.Gateway.csproj
│   │   │   ├── Program.cs
│   │   │   ├── ocelot.json
│   │   │   ├── appsettings.json
│   │   │   └── Dockerfile
│   │   │
│   │   ├── BookingCare.ServiceDiscovery/          # Service Discovery (Consul)
│   │   │   ├── BookingCare.ServiceDiscovery.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Services/
│   │   │   │   ├── ServiceRegistrationService.cs
│   │   │   │   └── HealthCheckService.cs
│   │   │   ├── Controllers/
│   │   │   │   └── ServiceDiscoveryController.cs
│   │   │   ├── appsettings.json
│   │   │   └── Dockerfile
│   │   │
│   │   └── BookingCare.MessageBus/               # Message Bus (RabbitMQ)
│   │       ├── BookingCare.MessageBus.csproj
│   │       ├── Abstractions/
│   │       │   └── IMessageBus.cs
│   │       ├── RabbitMQ/
│   │       │   └── RabbitMQMessageBus.cs
│   │       └── Extensions/
│   │           └── MessageBusExtensions.cs
│   │
│   ├── Services/                                  # Microservices
│   │   ├── AuthService/                          # Authentication Service
│   │   │   └── BookingCare.AuthService/
│   │   │       ├── BookingCare.AuthService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── AuthController.cs
│   │   │       ├── Services/
│   │   │       │   ├── IAuthenticationService.cs
│   │   │       │   ├── AuthenticationService.cs
│   │   │       │   └── GrpcAuthService.cs
│   │   │       ├── Models/
│   │   │       │   ├── DTOs/
│   │   │       │   │   └── AuthDtos.cs
│   │   │       │   └── Entities/
│   │   │       │       ├── Account.cs
│   │   │       │       ├── Role.cs
│   │   │       │       └── Permission.cs
│   │   │       ├── Data/
│   │   │       │   ├── AuthDbContext.cs
│   │   │       │   └── Configurations/
│   │   │       ├── Protos/
│   │   │       │   └── auth.proto
│   │   │       ├── appsettings.json
│   │   │       └── Dockerfile
│   │   │
│   │   ├── UserService/                          # User Management Service
│   │   │   └── BookingCare.UserService/
│   │   │       ├── BookingCare.UserService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── UsersController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       │   └── user.proto
│   │   │       └── Dockerfile
│   │   │
│   │   ├── DoctorService/                        # Doctor Management Service
│   │   │   └── BookingCare.DoctorService/
│   │   │       ├── BookingCare.DoctorService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   ├── DoctorsController.cs
│   │   │       │   └── SpecialtiesController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       │   └── doctor.proto
│   │   │       └── Dockerfile
│   │   │
│   │   ├── ClinicService/                        # Clinic Management Service
│   │   │   └── BookingCare.ClinicService/
│   │   │       ├── BookingCare.ClinicService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   ├── ClinicsController.cs
│   │   │       │   └── SubscriptionController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── AppointmentService/                   # Appointment Management Service
│   │   │   └── BookingCare.AppointmentService/
│   │   │       ├── BookingCare.AppointmentService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── AppointmentsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── PaymentService/                       # Payment Processing Service
│   │   │   └── BookingCare.PaymentService/
│   │   │       ├── BookingCare.PaymentService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── PaymentsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── ReviewService/                        # Review & Feedback Service
│   │   │   └── BookingCare.ReviewService/
│   │   │       ├── BookingCare.ReviewService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── ReviewsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── ContentService/                       # Content Management Service
│   │   │   └── BookingCare.ContentService/
│   │   │       ├── BookingCare.ContentService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   ├── BlogsController.cs
│   │   │       │   └── FAQsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── ServiceMedicalService/                # Medical Services Management
│   │   │   └── BookingCare.ServiceMedicalService/
│   │   │       ├── BookingCare.ServiceMedicalService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── MedicalServicesController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── CommunicationService/                 # Chat & Video Consultation Service
│   │   │   └── BookingCare.CommunicationService/
│   │   │       ├── BookingCare.CommunicationService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   ├── ChatController.cs
│   │   │       │   └── VideoCallController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       ├── Hubs/
│   │   │       │   └── ChatHub.cs
│   │   │       └── Dockerfile
│   │   │
│   │   ├── NotificationService/                  # Notification Service
│   │   │   └── BookingCare.NotificationService/
│   │   │       ├── BookingCare.NotificationService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── NotificationsController.cs
│   │   │       ├── Services/
│   │   │       │   ├── EmailService.cs
│   │   │       │   └── SMSService.cs
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── PromotionService/                     # Promotion & Discount Service
│   │   │   └── BookingCare.PromotionService/
│   │   │       ├── BookingCare.PromotionService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── PromotionsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── FavoriteService/                      # Favorites Service
│   │   │   └── BookingCare.FavoriteService/
│   │   │       ├── BookingCare.FavoriteService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── FavoritesController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   ├── AnalyticsService/                     # Analytics & Reports Service
│   │   │   └── BookingCare.AnalyticsService/
│   │   │       ├── BookingCare.AnalyticsService.csproj
│   │   │       ├── Program.cs
│   │   │       ├── Controllers/
│   │   │       │   └── AnalyticsController.cs
│   │   │       ├── Services/
│   │   │       ├── Models/
│   │   │       ├── Data/
│   │   │       ├── Protos/
│   │   │       └── Dockerfile
│   │   │
│   │   └── AIService/                           # AI & Smart Assistance Service
│   │       └── BookingCare.AIService/
│   │           ├── BookingCare.AIService.csproj
│   │           ├── Program.cs
│   │           ├── Controllers/
│   │           │   ├── AIController.cs
│   │           │   └── ChatbotController.cs
│   │           ├── Services/
│   │           │   ├── SymptomCheckerService.cs
│   │           │   └── RecommendationService.cs
│   │           ├── Models/
│   │           ├── Data/
│   │           ├── Protos/
│   │           └── Dockerfile
│   │
│   └── Shared/                                   # Shared libraries
│       ├── BookingCare.Shared.Common/             # Common models and utilities
│       │   ├── BookingCare.Shared.Common.csproj
│       │   ├── Models/
│       │   │   ├── CommonModels.cs
│       │   │   ├── ApiResponse.cs
│       │   │   └── BaseEntity.cs
│       │   ├── Extensions/
│       │   ├── Middleware/
│       │   └── Utilities/
│       │
│       ├── BookingCare.Shared.Contracts/          # Service contracts and DTOs
│       │   ├── BookingCare.Shared.Contracts.csproj
│       │   ├── Auth/
│       │   ├── Users/
│       │   ├── Doctors/
│       │   ├── Appointments/
│       │   └── Events/
│       │
│       └── BookingCare.Shared.EventBus/           # Event bus abstractions
│           ├── BookingCare.Shared.EventBus.csproj
│           ├── Abstractions/
│           ├── Events/
│           └── Handlers/
│
├── tests/                                        # Test projects
│   ├── Unit/
│   │   ├── BookingCare.AuthService.Tests/
│   │   ├── BookingCare.UserService.Tests/
│   │   └── BookingCare.DoctorService.Tests/
│   ├── Integration/
│   │   ├── BookingCare.Gateway.IntegrationTests/
│   │   └── BookingCare.Services.IntegrationTests/
│   └── Load/
│       └── BookingCare.LoadTests/
│
├── docs/                                         # Documentation
│   ├── api/
│   ├── deployment/
│   ├── development/
│   └── architecture/
│
├── scripts/                                      # Build and deployment scripts
│   ├── build.sh
│   ├── deploy.sh
│   ├── setup-local.sh
│   └── docker/
│
└── infrastructure/                               # Infrastructure as Code
    ├── terraform/
    ├── kubernetes/
    └── helm-charts/
```

## Key Features

### Communication Patterns
- **HTTP/REST**: External APIs and client-service communication
- **gRPC**: High-performance inter-service communication
- **Message Queue**: Asynchronous event-driven communication
- **SignalR**: Real-time communication (chat, notifications)

### Infrastructure Components
- **API Gateway**: Ocelot for request routing and authentication
- **Service Discovery**: Consul for service registration and discovery
- **Message Bus**: RabbitMQ for asynchronous messaging
- **Databases**: SQL Server, MongoDB, Redis for different data needs
- **Caching**: Redis for session management and high-speed operations

### Development Standards
- **Clean Architecture**: Each service follows Clean Architecture principles
- **CQRS + MediatR**: Command Query Responsibility Segregation
- **Auto Mapping**: AutoMapper for object mapping
- **Validation**: FluentValidation for request validation
- **Logging**: Serilog for structured logging
- **Health Checks**: Built-in health monitoring
- **API Documentation**: Swagger/OpenAPI documentation

### Security Features
- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: Fine-grained access control
- **HTTPS**: Secure communication
- **CORS**: Cross-origin resource sharing configuration

This structure provides a solid foundation for a scalable, maintainable microservices architecture following best practices and modern .NET development standards.
