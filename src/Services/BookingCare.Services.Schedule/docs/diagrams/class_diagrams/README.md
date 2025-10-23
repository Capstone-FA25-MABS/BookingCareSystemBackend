# Class Diagrams - Schedule Service

Tài liệu này chứa các class diagram mô tả cấu trúc và quan hệ giữa các class trong Schedule Service.

## Tổng quan

Schedule Service được thiết kế theo kiến trúc phân lớp (Layered Architecture) với các lớp chính:

1. **Presentation Layer** - Controllers và gRPC Services
2. **Business Layer** - Service layer với business logic
3. **Data Access Layer** - Repository và DbContext
4. **Domain Models** - Entities, DTOs, Requests, Enums
5. **Infrastructure** - Mappings, Utilities, Extensions

## Danh sách Class Diagrams

### 1. Domain Entities (01_entities.puml)
**Mô tả**: Các entity classes đại diện cho domain model trong database.

**Các class chính**:
- `DoctorDailyScheduleEntity` - Lịch làm việc hàng ngày của bác sĩ
- `DoctorScheduleExceptionEntity` - Ngoại lệ lịch làm việc (nghỉ, khóa/mở slot)
- `ClinicExceptionEntity` - Ngày nghỉ của phòng khám
- `ServiceScheduleEntity` - Lịch hoạt động của dịch vụ y tế

**Enums**:
- `SchedulePatterns` - Các pattern thời gian (MORNING, AFTERNOON, EVENING, FULL_DAY)
- `ExceptionType` - Loại ngoại lệ (BLOCK_SLOT, UNBLOCK_SLOT, DAY_OFF, CAPACITY_CHANGE)
- `AppointmentTime` - Các khung giờ cụ thể (AT_08_00_08_30, AT_08_30_09_00, ...)

**Quan hệ**:
- Composition (◆): Entity "chứa" list của Enum
- Association (→): Entity "tham chiếu" đến Enum
- Aggregation (◇): Optional reference

---

### 2. Data Transfer Objects (02_dtos.puml)
**Mô tả**: Các DTO classes dùng để transfer data giữa các layer và expose qua API.

**Các class chính**:
- `AppointmentTimeDto` - DTO cho khung giờ với startTime và endTime
- `DoctorDailyScheduleDto` - DTO cho lịch làm việc
- `DoctorScheduleExceptionDto` - DTO cho ngoại lệ (ExceptionType là string)
- `ClinicExceptionDto` - DTO cho ngày nghỉ phòng khám
- `ServiceScheduleDto` - DTO cho lịch dịch vụ

**Đặc điểm**:
- DTOs không chứa business logic
- Mapped từ Entities bằng AutoMapper
- ExceptionType được convert sang string trong DTO
- AppointmentTimeDto có GUID được generate từ enum value (deterministic)

---

### 3. Request Models (03_requests.puml)
**Mô tả**: Các request classes nhận data từ API endpoints.

**Các class chính**:
- `CreateDoctorDailyScheduleRequest` - Tạo/update lịch làm việc
- `CreateDoctorScheduleExceptionRequest` - Tạo ngoại lệ
  - AppointmentTimes có thể null/empty cho nghỉ cả ngày
- `CreateClinicExceptionRequest` - Tạo ngày nghỉ phòng khám
- `CreateServiceScheduleRequest` - Tạo lịch dịch vụ
- `GetAvailableSlotsRequest` - Lấy khung giờ trống (ServiceId optional)
- `GetDoctorScheduleRequest` - Lấy lịch theo range

**Validation**:
- Các field có attribute `[Required]`, `[StringLength]`, `[RegularExpression]`
- Input validation được thực hiện ở API layer

---

### 4. Service & Repository Layers (04_service_repository_layers.puml)
**Mô tả**: Các service và repository classes với business logic và data access.

**Controllers**:
- `DoctorSchedulesController` - REST API cho doctor schedules
- `DoctorScheduleExceptionsController` - REST API cho exceptions

**Service Layer**:
- `IScheduleService` (interface) - Contract cho business operations
- `ScheduleService` (implementation) - Business logic layer
  - Cache management (Redis)
  - gRPC client calls for validation (Doctor, ServiceMedical, Appointment services)
  - Entity-DTO mapping (AutoMapper)
  - Cache invalidation strategy
- `ScheduleGrpcService` - gRPC service implementation

**Repository Layer**:
- `IScheduleRepository` (interface) - Contract cho data access
- `ScheduleRepository` (implementation) - Data access layer
  - Database operations (EF Core)
  - Complex slot calculations
  - Exception handling logic
  - Pattern-based slot generation (Morning/Afternoon/Evening slots)

**Quan hệ**:
- Controllers → IScheduleService (Dependency)
- ScheduleService → IScheduleRepository (Dependency)
- Interface ← Implementation (Realization)

---

### 5. Infrastructure (05_infrastructure.puml)
**Mô tả**: Infrastructure components và supporting classes.

**Data Layer**:
- `ScheduleDbContext` - EF Core DbContext
  - Manages 4 DbSets (DoctorDailySchedules, DoctorScheduleExceptions, ClinicExceptions, ServiceSchedules)
  - Auto-updates timestamps (CreatedAt, UpdatedAt)
  - Configures entity relationships

**Mappings**:
- `ScheduleMappingProfile` - AutoMapper profile
  - Maps Entities ↔ DTOs
  - Maps Requests → Entities

**Extensions**:
- `ScheduleModelBuilderExtensions` - EF Core ModelBuilder extensions
  - Configures entity properties
  - Defines indexes, constraints
  - JSON conversion for List<SchedulePatterns>

**Utilities**:
- `AppointmentTimeHelper` - Static helper class
  - Converts AppointmentTime enum → AppointmentTimeDto
  - Generates deterministic GUID from enum value
  - Parses time strings from enum name

**Exceptions**:
- `ScheduleException` (base)
- `ScheduleExceptionNotFoundException`
- `ScheduleExceptionConflictException`
- `ClinicNotFoundException`
- `ClinicExceptionNotFoundException`
- `ServiceNotAvailableException`
- `ServiceScheduleNotFoundException`

**External Dependencies**:
- `ICacheService` (Redis) - từ Shared library
- `IMapper` (AutoMapper) - từ AutoMapper library
- `DoctorServiceClient` (gRPC) - Call Doctor service
- `ServiceMedicalServiceClient` (gRPC) - Call ServiceMedical service
- `AppointmentServiceClient` (gRPC) - Call Appointment service

---

### 6. Complete Architecture (06_complete_architecture.puml)
**Mô tả**: Tổng quan toàn bộ kiến trúc Schedule Service với tất cả các layer và dependencies.

**Layers**:

1. **Presentation Layer** (LightBlue)
   - DoctorSchedulesController
   - DoctorScheduleExceptionsController
   - ScheduleGrpcService

2. **Business Layer** (LightGreen)
   - IScheduleService (interface)
   - ScheduleService (implementation)

3. **Data Access Layer** (LightYellow)
   - IScheduleRepository (interface)
   - ScheduleRepository (implementation)
   - ScheduleDbContext

4. **Domain Models** (LightCoral)
   - Entities (4 classes)
   - DTOs (5 classes)
   - Requests (7 classes)
   - Enums (3 enums)

5. **Infrastructure** (Lavender)
   - ScheduleMappingProfile
   - AppointmentTimeHelper
   - ScheduleModelBuilderExtensions

6. **External Dependencies** (LightGray - Cloud)
   - ICacheService (Redis)
   - IMapper (AutoMapper)
   - DoctorServiceClient (gRPC)
   - ServiceMedicalServiceClient (gRPC)
   - AppointmentServiceClient (gRPC)

7. **Database** (SQL Server)
   - doctor_daily_schedules
   - doctor_schedule_exceptions
   - clinic_exceptions
   - service_schedules

**Design Patterns**:
- **Repository Pattern**: Tách biệt data access logic
- **Dependency Injection**: Loose coupling giữa các components
- **DTO Pattern**: Transfer data giữa layers
- **Cache-Aside Pattern**: Caching strategy với Redis
- **Factory Pattern**: AutoMapper mappings
- **Service Layer Pattern**: Business logic encapsulation

---

## Quan hệ giữa các Class

### Composition (Chứa đựng) - ◆
- Entity chứa list của Enum (ví dụ: DoctorDailyScheduleEntity chứa List<SchedulePatterns>)
- Owner object quản lý lifecycle của contained objects

### Aggregation (Tập hợp) - ◇
- Weak ownership, optional reference
- Ví dụ: DoctorScheduleExceptionEntity có optional AppointmentTime

### Association (Liên kết) - →
- Entity tham chiếu đến Enum
- Ví dụ: DoctorScheduleExceptionEntity has ExceptionType

### Dependency (Phụ thuộc) - - - >
- Controller depends on Service
- Service depends on Repository
- Được inject qua constructor (DI)

### Realization (Thực thi) - - - |>
- Class implements Interface
- Ví dụ: ScheduleService implements IScheduleService

---

## Key Design Decisions

### 1. Entity Design
- Sử dụng `Guid` cho tất cả IDs (universally unique)
- `DateOnly` cho dates (không cần time component)
- List<Enum> stored as JSON trong database
- Timestamp fields (CreatedAt, UpdatedAt) tự động update

### 2. DTO Design
- Tách biệt DTOs khỏi Entities để control API contract
- ExceptionType convert sang string trong DTO
- AppointmentTimeDto với deterministic GUID generation

### 3. Service Layer
- Cache-aside pattern với Redis
- Cache invalidation khi CREATE/UPDATE/DELETE
- Validation với external services qua gRPC
- AutoMapper cho entity-DTO mapping

### 4. Repository Layer
- Complex business logic cho slot calculations
- Pattern-based slot generation (Morning: 8-12, Afternoon: 13-17, Evening: 17-21)
- Exception handling logic (Day off, Block/Unblock slots)
- Service schedule filtering

### 5. Database Design
- Unique index: (DoctorId, ScheduleDate) cho DoctorDailySchedules
- Index trên foreign keys và date fields
- JSON storage cho List<SchedulePatterns>

---

## Xem Diagrams

Các file `.puml` có thể được xem bằng:
1. **PlantUML Plugin** trong VS Code
2. **PlantUML Online Server**: http://www.plantuml.com/plantuml/
3. **IntelliJ IDEA** với PlantUML integration plugin

## Lưu ý

- Tất cả class diagrams sử dụng PlantUML syntax
- Sử dụng màu sắc để phân biệt các layers
- Relationships được mô tả rõ ràng với UML notation
- Có notes giải thích cho các concepts phức tạp
- Legend/chú thích cho complete architecture diagram
