# 🏥 BookingCare Hệ thống Backend Microservices

## 📋 Mục Lục

- [Giới Thiệu](#giới-thiệu)
- [Mô Tả Dự Án](#mô-tả-dự-án)
- [Kiến Trúc Hệ Thống](#kiến-trúc-hệ-thống)
- [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
- [Hướng Dẫn Cài Đặt](#hướng-dẫn-cài-đặt)
- [Hướng Dẫn Sử Dụng](#hướng-dẫn-sử-dụng)
- [Cấu Trúc Dự Án](#cấu-trúc-dự-án)
- [API Documentation](#api-documentation)
- [Troubleshooting](#troubleshooting)

## 🎯 Giới Thiệu

**BookingCare** là một hệ thống đặt lịch khám bệnh trực tuyến hiện đại, được xây dựng với kiến trúc microservices. Hệ thống này cho phép bệnh nhân:

- 👨‍⚕️ Tìm kiếm và đặt lịch khám với các bác sĩ
- 🏥 Tìm hiểu thông tin các bệnh viện, phòng khám
- 💳 Thanh toán trực tuyến an toàn
- 📝 Xem lịch sử khám bệnh
- ⭐ Đánh giá và bình luận về bác sĩ
- 🔔 Nhận thông báo về lịch khám sắp tới

## 📖 Mô Tả Dự Án

### Tổng Quan

Backend của BookingCare là một nền tảng microservices mạnh mẽ, được thiết kế để hỗ trợ một hệ thống đặt lịch khám bệnh phức tạp. Hệ thống này bao gồm:

- **19 Microservices** độc lập, mỗi service có chuyên môn riêng
- **API Gateway** để quản lý và định tuyến yêu cầu
- **Message Queue** cho giao tiếp không đồng bộ giữa các services
- **Multiple Databases** tối ưu hóa cho từng loại dữ liệu
- **Comprehensive Monitoring** để theo dõi sức khỏe hệ thống

### Các Tính Năng Chính

#### 🔐 Xác Thực & Phân Quyền (AuthService)
- Đăng nhập, đăng ký người dùng
- JWT token-based authentication
- Role-based access control (RBAC)
- OAuth2 integration

#### 👥 Quản Lý Người Dùng (UserService)
- Quản lý hồ sơ bệnh nhân
- Cập nhật thông tin cá nhân
- Quản lý địa chỉ và liên hệ
- Xác minh email/số điện thoại

#### 👨‍⚕️ Quản Lý Bác Sĩ (DoctorService)
- Thông tin bác sĩ chi tiết
- Chuyên khoa và kỹ năng
- Đánh giá và rating
- Kinh nghiệm làm việc

#### 🏥 Quản Lý Bệnh Viện (HospitalService)
- Thông tin bệnh viện/phòng khám
- Giờ mở cửa
- Liên hệ và địa chỉ
- Ảnh và mô tả chi tiết

#### 📅 Quản Lý Lịch Khám (ScheduleService)
- Lịch trình bác sĩ
- Các khung giờ khám
- Quản lý ngày lễ/nghỉ
- Đặt chỗ tự động

#### 🎫 Đặt Lịch Khám (AppointmentService)
- Tạo và quản lý appointment
- Xác nhận và hủy lịch
- Lịch sử appointment
- Status tracking

#### 💳 Xử Lý Thanh Toán (PaymentService)
- Ghi nhận thanh toán
- Hóa đơn điện tử
- Hoàn tiền
- Tích hợp payment gateway

#### ⭐ Đánh Giá & Nhận Xét (ReviewService)
- Thêm đánh giá bác sĩ
- Xem các review
- Rating trung bình
- Xóa review

#### 📢 Thông Báo (NotificationService)
- Email notifications
- SMS alerts
- In-app notifications
- Push notifications

#### 🎁 Quản Lý Khuyến Mại (DiscountService)
- Mã giảm giá
- Chương trình khuyến mại
- Kiểm tra hợp lệ voucher
- Lịch sử sử dụng

#### 📊 Phân Tích Dữ Liệu (AnalyticsService)
- Báo cáo doanh thu
- Số liệu appointment
- Phân tích bác sĩ
- Dashboard thống kê

#### 🤖 AI Features (AIService)
- Gợi ý bác sĩ
- Phân loại triệu chứng
- Dự đoán lịch khám
- Chatbot hỗ trợ

#### 📱 Nội Dung & Truyền Thông (ContentService, CommunicationService)
- Blog và bài viết
- Chat trực tuyến
- Video consultation
- FAQs

## 🏗 Kiến Trúc Hệ Thống

```
┌──────────────────────────────────────────────────────────────┐
│                    FRONTEND LAYER                            │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │   User Portal    │         │  Admin Portal    │          │
│  │   (Port: 3000)   │         │  (Port: 3001)    │          │
│  └──────────────────┘         └──────────────────┘          │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                    API GATEWAY LAYER                         │
│          (Port: 5001 - Ocelot API Gateway)                  │
│  - Request routing                                           │
│  - Authentication                                            │
│  - Rate limiting                                             │
│  - Load balancing                                            │
└──────────────────────────────────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
         ▼                  ▼                  ▼
    ┌─────────┐        ┌─────────┐        ┌─────────┐
    │ gRPC    │        │  HTTP   │        │ Message │
    │ Protocol│        │ REST    │        │ Queue   │
    └─────────┘        └─────────┘        └─────────┘
         │                  │                  │
         ▼                  ▼                  ▼
┌──────────────────────────────────────────────────────────────┐
│                 MICROSERVICES LAYER                          │
│ Auth │ User │ Doctor │ Hospital │ Appointment │ Schedule    │
│ Payment │ Review │ Notification │ Analytics │ AI │ Content  │
│ Discount │ Communication │ ServiceMedical │ Favorite │ Blog  │
└──────────────────────────────────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
         ▼                  ▼                  ▼
    ┌──────────┐        ┌──────────┐      ┌──────────┐
    │SQL Server│        │ MongoDB  │      │  Redis   │
    │(11 DB)   │        │(Documents)     │(Cache)   │
    └──────────┘        └──────────┘      └──────────┘
         │
    ┌────────────────────────┐
    │  Databases:            │
    │  • Auth DB             │
    │  • User DB             │
    │  • Doctor DB           │
    │  • Hospital DB         │
    │  • Appointment DB      │
    │  • Schedule DB         │
    │  • Payment DB          │
    │  • Discount DB         │
    │  • ServiceMedical DB   │
    │  • AI DB               │
    │  • Saga DB             │
    └────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│              INFRASTRUCTURE & MONITORING                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  RabbitMQ    │  │   Consul     │  │    Jaeger    │      │
│  │ (Message Bus)│  │  (Discovery) │  │  (Tracing)   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │  Prometheus  │  │   Grafana    │                        │
│  │  (Metrics)   │  │ (Dashboards) │                        │
│  └──────────────┘  └──────────────┘                        │
└──────────────────────────────────────────────────────────────┘
```

## 🛠 Công Nghệ Sử Dụng

### Framework & Runtime
- **.NET 8.0** - Framework chính
- **C#** - Ngôn ngữ lập trình
- **ASP.NET Core** - Web framework

### Communication Protocols
- **gRPC** - Inter-service communication (high performance)
- **HTTP/REST** - External API endpoints
- **Protobuf** - Data serialization for gRPC

### Message Queue & Event Bus
- **RabbitMQ** - Asynchronous messaging
- **MassTransit** - .NET message bus abstraction
- **Event Sourcing** - Domain events

### Service Discovery & API Gateway
- **Consul** - Service discovery & health checking
- **Ocelot** - API Gateway
- **Dynamic Service Resolution**

### Data & Caching
- **SQL Server** - Relational databases (11 instances)
- **MongoDB** - Document database
- **Redis** - Distributed caching
- **Entity Framework Core** - ORM

### Monitoring & Observability
- **Prometheus** - Metrics collection
- **Grafana** - Visualization & dashboards
- **Jaeger** - Distributed tracing
- **Serilog** - Structured logging
- **OpenTelemetry** - Observability standard

### Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library
- **TestContainers** - Integration testing

### DevOps & Deployment
- **Docker** - Containerization
- **Docker Compose** - Orchestration
- **Kubernetes** - Production orchestration (optional)

## ✅ Yêu Cầu Hệ Thống

### Tối Thiểu
- **OS**: Windows 10+, macOS 10.14+, hoặc Linux
- **RAM**: 16GB
- **CPU**: 4 cores
- **Disk**: 50GB free space

### Khuyến Nghị
- **OS**: Windows 11, macOS 12+, hoặc Ubuntu 20.04+
- **RAM**: 32GB
- **CPU**: 8 cores
- **Disk**: 100GB SSD

### Phần Mềm Cần Thiết
- **Docker Desktop** (v20.10+)
  ```bash
  # macOS
  brew install --cask docker
  
  # Windows/Linux
  # Download from https://www.docker.com/products/docker-desktop
  ```

- **.NET 8.0 SDK**
  ```bash
  # macOS
  brew install dotnet
  
  # Windows
  # Download from https://dotnet.microsoft.com/download
  
  # Linux
  # Follow https://learn.microsoft.com/en-us/dotnet/core/install/linux
  ```

- **Git**
  ```bash
  # macOS
  brew install git
  ```

- **Docker Compose** (usually included with Docker Desktop)
  ```bash
  docker-compose --version
  ```

## 🚀 Hướng Dẫn Cài Đặt

### 1. Clone Repository

```bash
# Clone backend repository
git clone https://github.com/Capstone-FA25-MABS/BookingCareSystemBackend.git
cd BookingCareSystemBackend

# Hoặc nếu đã clone
git checkout develop
git pull origin develop
```

### 2. Thiết Lập Environment

```bash
# Copy environment template
cp .env.example .env

# Edit .env với credentials của bạn
# Windows
notepad .env

# macOS/Linux
nano .env
```

**Cấu hình .env:**
```env
# Docker Configuration
DOCKER_USERNAME=your_username
VERSION=latest

# Database Passwords (CHANGE THESE!)
SQLSERVER_AUTH_PASSWORD=YourSecureAuthPassword123!
SQLSERVER_USER_PASSWORD=YourSecureUserPassword123!
SQLSERVER_DOCTOR_PASSWORD=YourSecureDoctorPassword123!
SQLSERVER_HOSPITAL_PASSWORD=YourSecureHospitalPassword123!
SQLSERVER_APPOINTMENT_PASSWORD=YourSecureAppointmentPassword123!
SQLSERVER_SCHEDULE_PASSWORD=YourSecureSchedulePassword123!
SQLSERVER_PAYMENT_PASSWORD=YourSecurePaymentPassword123!
SQLSERVER_DISCOUNT_PASSWORD=YourSecureDiscountPassword123!
SQLSERVER_CONTENT_PASSWORD=YourSecureContentPassword123!
SQLSERVER_AI_PASSWORD=YourSecureAIPassword123!
SQLSERVER_SAGA_PASSWORD=YourSecureSagaPassword123!

# RabbitMQ
RABBITMQ_DEFAULT_USER=bookingcare
RABBITMQ_DEFAULT_PASS=YourSecureRabbitMQPassword123!

# MongoDB
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=YourSecureMongoPassword123!

# Redis
REDIS_PASSWORD=YourSecureRedisPassword123!

# API Gateway
GATEWAY_PORT=5001

# Service Ports
AUTH_SERVICE_PORT=6003
USER_SERVICE_PORT=6016
DOCTOR_SERVICE_PORT=6004
# ... more services
```

### 3. Xây Dựng Docker Images

#### Option A: Pull Images từ Docker Hub (nếu công khai)
```bash
docker-compose pull
```

#### Option B: Build Images Locally
```bash
# Build tất cả images
docker-compose build

# Hoặc build một service cụ thể
docker-compose build api-gateway
```

### 4. Khởi Động Hệ Thống

#### Step 1: Khởi động Infrastructure
```bash
# Khởi động databases, message queue, cache
docker-compose up -d rabbitmq redis mongodb

# Khởi động tất cả SQL Server databases
docker-compose up -d \
  sqlserver-auth \
  sqlserver-user \
  sqlserver-doctor \
  sqlserver-hospital \
  sqlserver-appointment \
  sqlserver-schedule \
  sqlserver-payment \
  sqlserver-discount \
  sqlserver-content \
  sqlserver-ai \
  sqlserver-saga

# Chờ 30-60 giây để databases khởi động
sleep 60

# Kiểm tra health
docker-compose ps
```

#### Step 2: Khởi động Consul (Service Discovery)
```bash
docker-compose up -d consul
```

#### Step 3: Khởi động Backend Services
```bash
# Khởi động API Gateway
docker-compose up -d api-gateway

# Chờ API Gateway sẵn sàng
sleep 30

# Khởi động tất cả microservices
docker-compose up -d \
  auth-service \
  user-service \
  doctor-service \
  hospital-service \
  appointment-service \
  schedule-service \
  payment-service \
  review-service \
  discount-service \
  content-service \
  notification-service \
  analytics-service \
  ai-service \
  communication-service \
  servicemedical-service \
  favorite-service \
  blog-service \
  saga-orchestrator
```

#### Step 4: Khởi động Monitoring (Tuỳ chọn)
```bash
# Khởi động Prometheus, Grafana, Jaeger
docker-compose up -d prometheus grafana jaeger
```

#### Step 5: Khởi động Tất Cả Cùng Lúc
```bash
# Khởi động tất cả services
docker-compose up -d

# Xem logs
docker-compose logs -f

# Chờ tất cả services ready (3-5 phút)
```

### 5. Kiểm Tra Sức Khỏe Hệ Thống

```bash
# Kiểm tra tất cả containers
docker-compose ps

# Xem logs của service cụ thể
docker-compose logs -f api-gateway

# Health check
curl http://localhost:5001/health

# Kiểm tra Consul services
curl http://localhost:8500/v1/catalog/services

# Kiểm tra Prometheus
curl http://localhost:9090/api/v1/targets
```

## 📖 Hướng Dẫn Sử Dụng

### Truy Cập API Gateway

**Base URL**: `http://localhost:5001`

### Điểm Cuối Chính (Main Endpoints)

#### Authentication
```bash
# Đăng ký
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "fullName": "John Doe"
}

# Đăng nhập
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

# Response
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 3600
}
```

#### Users
```bash
# Lấy thông tin profile
GET /api/users/profile
Authorization: Bearer {token}

# Cập nhật profile
PUT /api/users/profile
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "John Doe Updated",
  "phoneNumber": "+84912345678",
  "address": "123 Nguyen Hue, Ho Chi Minh"
}
```

#### Doctors
```bash
# Danh sách bác sĩ
GET /api/doctors
GET /api/doctors?specialty=Cardiology&page=1&pageSize=10

# Chi tiết bác sĩ
GET /api/doctors/{doctorId}

# Danh sách lịch khám
GET /api/doctors/{doctorId}/schedules
```

#### Appointments
```bash
# Tạo appointment
POST /api/appointments
Authorization: Bearer {token}
Content-Type: application/json

{
  "doctorId": "doctor-uuid",
  "scheduleId": "schedule-uuid",
  "notes": "Patient notes here"
}

# Danh sách appointments của bệnh nhân
GET /api/appointments
Authorization: Bearer {token}

# Chi tiết appointment
GET /api/appointments/{appointmentId}
Authorization: Bearer {token}

# Hủy appointment
DELETE /api/appointments/{appointmentId}
Authorization: Bearer {token}
```

#### Payments
```bash
# Tạo payment
POST /api/payments
Authorization: Bearer {token}
Content-Type: application/json

{
  "appointmentId": "appointment-uuid",
  "amount": 500000,
  "paymentMethod": "creditcard"
}

# Kiểm tra status payment
GET /api/payments/{paymentId}
Authorization: Bearer {token}
```

### Sử Dụng Monitoring Dashboards

#### Grafana Dashboard
- **URL**: http://localhost:3000
- **Username**: admin
- **Password**: admin123
- **Dashboards**: System Overview, Service Metrics, Database Metrics

#### Prometheus
- **URL**: http://localhost:9090
- **PromQL queries**: Xem metrics của các services

#### Jaeger Tracing
- **URL**: http://localhost:16686
- **View**: Distributed traces của requests

#### Consul Service Discovery
- **URL**: http://localhost:8500
- **View**: Registered services, health checks, key-value store

## 📁 Cấu Trúc Dự Án

```
BookingCareSystemBackend/
├── src/
│   ├── Shared/
│   │   └── BookingCare.Shared.Common/        # Shared utilities
│   ├── Services/
│   │   ├── AuthService/                      # Authentication service
│   │   ├── UserService/                      # User management
│   │   ├── DoctorService/                    # Doctor profiles
│   │   ├── HospitalService/                  # Hospital management
│   │   ├── AppointmentService/               # Appointment booking
│   │   ├── ScheduleService/                  # Doctor schedules
│   │   ├── PaymentService/                   # Payment processing
│   │   ├── ReviewService/                    # Reviews & ratings
│   │   ├── DiscountService/                  # Promotions & discounts
│   │   ├── ContentService/                   # Blog & content
│   │   ├── NotificationService/              # Notifications
│   │   ├── AnalyticsService/                 # Analytics & reports
│   │   ├── AIService/                        # AI features
│   │   ├── CommunicationService/             # Chat & messaging
│   │   ├── ServiceMedicalService/            # Medical services catalog
│   │   ├── FavoriteService/                  # User favorites
│   │   ├── BlogService/                      # Blog management
│   │   └── SagaOrchestrator/                 # Saga pattern orchestration
│   ├── Gateway/
│   │   └── ApiGateway/                       # Ocelot API Gateway
│   └── Program.cs
├── tests/
│   ├── Unit/                                 # Unit tests
│   ├── Integration/                          # Integration tests
│   └── Performance/                          # Performance tests
├── docker-compose.yml                        # Docker Compose config
├── docker-compose.prd.yml                    # Production config
├── rabbitmq.conf                             # RabbitMQ config
├── sonar-project.properties                  # SonarQube config
├── data/
│   └── *.sql                                 # Database scripts
├── docs/
│   ├── api/                                  # API documentation
│   ├── architecture/                         # Architecture docs
│   ├── deployment/                           # Deployment guides
│   └── development/                          # Development guides
├── infrastructure/                           # IaC files
├── monitoring/                               # Monitoring stack
├── scripts/                                  # Build & deployment scripts
└── README.md                                 # Project README
```

### Cấu Trúc Microservice

```
AuthService/
├── src/
│   ├── BookingCare.AuthService/
│   │   ├── Application/
│   │   │   ├── DTOs/                        # Data Transfer Objects
│   │   │   ├── Services/                    # Business logic
│   │   │   ├── Validators/                  # FluentValidation
│   │   │   └── Mappers/                     # AutoMapper configs
│   │   ├── Domain/
│   │   │   ├── Entities/                    # Domain entities
│   │   │   ├── Events/                      # Domain events
│   │   │   ├── Interfaces/                  # Repository interfaces
│   │   │   └── ValueObjects/                # Value objects
│   │   ├── Infrastructure/
│   │   │   ├── Database/                    # EF Core DbContext
│   │   │   ├── Repositories/                # Repository implementations
│   │   │   ├── Messaging/                   # MassTransit consumers
│   │   │   └── ExternalServices/            # External service calls
│   │   ├── Presentation/
│   │   │   ├── Controllers/                 # API controllers
│   │   │   ├── Middleware/                  # Custom middleware
│   │   │   └── Extensions/                  # Dependency injection
│   │   └── Program.cs                       # Service entry point
│   └── BookingCare.AuthService.Protos/
│       └── *.proto                          # gRPC service definitions
└── tests/
    ├── Unit/
    ├── Integration/
    └── E2E/
```

## 📚 API Documentation

### Swagger/OpenAPI

Mỗi service có Swagger documentation riêng:

```
http://localhost:6003/swagger/index.html      # Auth Service
http://localhost:6016/swagger/index.html      # User Service
http://localhost:6004/swagger/index.html      # Doctor Service
http://localhost:6005/swagger/index.html      # Hospital Service
http://localhost:6002/swagger/index.html      # Appointment Service
```

### API Gateway Documentation

```
http://localhost:5001/swagger/index.html
```

## 🔧 Troubleshooting

### Problem: Containers không khởi động được

```bash
# 1. Kiểm tra docker status
docker ps -a

# 2. Xem logs chi tiết
docker-compose logs [service-name]

# 3. Xóa và rebuild
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

### Problem: Database connection errors

```bash
# 1. Kiểm tra database container
docker exec sqlserver-auth sqlcmd -S localhost -U sa

# 2. Kiểm tra credentials trong .env
cat .env | grep SQLSERVER

# 3. Khởi động lại database
docker-compose restart sqlserver-auth
```

### Problem: RabbitMQ connection refused

```bash
# 1. Kiểm tra RabbitMQ status
docker-compose logs rabbitmq

# 2. Truy cập management UI
http://localhost:15672
# Username: guest
# Password: guest

# 3. Restart RabbitMQ
docker-compose restart rabbitmq
```

### Problem: Services không đăng ký với Consul

```bash
# 1. Kiểm tra Consul UI
http://localhost:8500

# 2. Xem logs service
docker-compose logs api-gateway

# 3. Kiểm tra Consul connectivity
curl http://localhost:8500/v1/health/service/api-gateway
```

### Problem: High memory/CPU usage

```bash
# 1. Kiểm tra resource usage
docker stats

# 2. Giảm replicas hoặc resource limits
# Edit docker-compose.yml

# 3. Monitor active connections
docker exec redis redis-cli INFO stats
```

## 📞 Support & Kontribusi

### Gửi Bug Reports
1. Kiểm tra [Issues](https://github.com/Capstone-FA25-MABS/BookingCareSystemBackend/issues)
2. Tạo issue mới với template
3. Include logs và reproduction steps

### Gửi Pull Requests
1. Fork repository
2. Tạo feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Mở Pull Request

### Development Guidelines
- Tuân thủ [Clean Architecture](./docs/architecture/CLEAN_ARCHITECTURE.md)
- Viết unit tests cho tất cả business logic
- Thêm integration tests cho database interactions
- Follow [Coding Standards](./docs/development/CODING_STANDARDS.md)
- Cập nhật documentation

## 📄 License

Dự án này được cấp phép dưới [MIT License](LICENSE)

## 👥 Team

- **Project Lead**: Team Lead
- **Architecture**: System Architects
- **Development**: Development Team
- **QA**: Quality Assurance Team

---

**Last Updated**: December 2025
**Version**: 1.0.0
