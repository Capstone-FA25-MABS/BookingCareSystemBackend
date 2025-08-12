# BookingCare Microservices Architecture

## Overview
This is a comprehensive healthcare booking system built using .NET microservices architecture with gRPC, HTTP APIs, Message Queue, API Gateway, and Service Discovery.

## Architecture Components

### Infrastructure Services
- **API Gateway**: Entry point for all client requests, handles routing and authentication
- **Service Discovery**: Service registry for dynamic service discovery
- **Message Bus**: Event-driven communication between services

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

### Running the Application
1. Start infrastructure services (databases, message queue)
2. Run Service Discovery
3. Start individual microservices
4. Start API Gateway

### Development Guidelines
- Each service follows Clean Architecture principles
- Use dependency injection for all services
- Implement proper logging and monitoring
- Follow SOLID principles
- Use async/await for all I/O operations