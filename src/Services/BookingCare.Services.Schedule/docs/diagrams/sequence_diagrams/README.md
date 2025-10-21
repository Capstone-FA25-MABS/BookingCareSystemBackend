# Sequence Diagrams - Schedule Service

Tài liệu này chứa các sequence diagram mô tả luồng xử lý của các API endpoints quan trọng trong Schedule Service.

## Cấu trúc Diagram

Tất cả các diagram đều tuân theo cấu trúc chuẩn với các thành phần sau:

- **User** (actor): Người dùng cuối - được biểu diễn bằng hình người
- **UI** (participant): Giao diện người dùng (Frontend)
- **Controller** (participant): API Controller xử lý HTTP requests
- **ScheduleService** (participant): Business logic layer
- **Repository** (participant): Data access layer
- **Database** (database): PostgreSQL database - được biểu diễn bằng hình khối trụ
- **CacheService** (participant): Redis cache service
- **External gRPC Services**: Doctor Service, ServiceMedical Service, Appointment Service

## Danh sách Diagrams

### 1. Get Doctor Daily Schedule (01_get_doctor_daily_schedule.puml)
**Endpoint**: `GET /api/v1/doctor-schedules/{doctorId}/daily/{date}`

**Mô tả**: Lấy thông tin lịch khám của bác sĩ theo ngày cụ thể.

**Luồng chính**:
1. User yêu cầu xem lịch khám của bác sĩ theo ngày
2. UI gọi hàm GetDoctorDailySchedule với doctorId và date
3. Controller chuyển tiếp request đến Service
4. Service kiểm tra cache trước
5. Nếu cache miss, query từ database thông qua Repository
6. Map entity sang DTO và lưu vào cache
7. Trả về kết quả hoặc NotFound

**Đặc điểm**:
- Sử dụng cache để tối ưu performance
- Cache expiration: Short (vài phút)

---

### 2. Get Doctor Schedule Range (02_get_doctor_schedule_range.puml)
**Endpoint**: `GET /api/v1/doctor-schedules/{doctorId}/range?startDate=...&endDate=...`

**Mô tả**: Lấy danh sách lịch khám của bác sĩ trong một khoảng thời gian.

**Luồng chính**:
1. User yêu cầu xem lịch khám theo khoảng thời gian
2. UI gọi hàm GetDoctorScheduleRange với doctorId, startDate, endDate
3. Controller tạo request object và chuyển đến Service
4. Service kiểm tra cache
5. Nếu cache miss, query từ database với điều kiện date range
6. Sắp xếp theo ScheduleDate và map sang DTOs
7. Lưu cache và trả về danh sách

**Đặc điểm**:
- Query với range condition và order by
- Cache theo combination của doctorId + startDate + endDate

---

### 3. Create Or Update Doctor Daily Schedule (03_create_or_update_doctor_daily_schedule.puml)
**Endpoint**: `POST /api/v1/doctor-schedules`

**Mô tả**: Tạo mới hoặc cập nhật lịch khám hàng ngày của bác sĩ.

**Luồng chính**:
1. User tạo/cập nhật lịch khám
2. UI gọi hàm CreateOrUpdateDoctorDailySchedule với request body
3. Controller chuyển request đến Service
4. **Validation**: Service gọi gRPC đến Doctor Service để validate doctor tồn tại và active
5. Nếu doctor không valid, throw DoctorNotFoundException
6. Map request sang entity
7. Repository kiểm tra schedule đã tồn tại chưa:
   - Nếu tồn tại: UPDATE
   - Nếu chưa: INSERT
8. **Invalidate cache**: Xóa cache của daily schedule và available slots
9. Map entity sang DTO và trả về

**Đặc điểm**:
- Có validation với external service (Doctor Service) qua gRPC
- Upsert pattern (Create or Update)
- Cache invalidation để đảm bảo data consistency

---

### 4. Delete Doctor Daily Schedule (04_delete_doctor_daily_schedule.puml)
**Endpoint**: `DELETE /api/v1/doctor-schedules/{doctorId}/daily/{date}`

**Mô tả**: Xóa lịch khám của bác sĩ theo ngày.

**Luồng chính**:
1. User yêu cầu xóa lịch khám
2. UI gọi hàm DeleteDoctorDailySchedule với doctorId và date
3. Controller chuyển request đến Service
4. Service gọi Repository để xóa
5. Repository query để tìm schedule
6. Nếu tìm thấy, thực hiện DELETE
7. **Invalidate cache**: Xóa cache của daily schedule và available slots
8. Trả về success message

**Đặc điểm**:
- Soft delete không được áp dụng (hard delete)
- Cache invalidation pattern matching cho available slots

---

### 5. Get Available Slots (05_get_available_slots.puml)
**Endpoint**: `GET /api/v1/doctor-schedules/{doctorId}/available-slots?date=...&serviceId=...`

**Mô tả**: Lấy danh sách khung giờ trống của bác sĩ (slots chưa được đặt).

**Luồng chính**:
1. User xem khung giờ trống
2. UI gọi hàm GetAvailableSlots với doctorId, date, và serviceId (optional)
3. Controller tạo request và chuyển đến Service
4. **Validation Doctor**: Service gọi gRPC đến Doctor Service
   - Nếu không valid, throw DoctorNotAvailableException
5. **Validation Service** (nếu có serviceId): Gọi gRPC đến ServiceMedical Service
   - Nếu không valid, throw ServiceNotAvailableException
6. Service kiểm tra cache
7. Nếu cache miss:
   - Repository lấy doctor schedule từ database
   - Convert schedule patterns sang appointment times
   - Lấy exceptions của doctor từ database
   - Apply exceptions để filter slots (day off, block slot, unblock slot)
   - Nếu có serviceId: Filter theo service schedule
8. Service convert enums sang DTOs
9. **Check booked slots**: Gọi gRPC đến Appointment Service để lấy danh sách slots đã được book
10. Filter out các slots đã được book
11. Lưu cache và trả về danh sách available slots

**Đặc điểm**:
- Luồng phức tạp nhất với nhiều validation và external service calls
- Kết hợp data từ nhiều nguồn: Schedule, Exceptions, Service Schedule, Booked Appointments
- Business logic phức tạp: Pattern conversion, exception handling, booking status
- Cache key bao gồm doctorId, date, và serviceId

---

### 6. Get Doctor Exceptions (06_get_doctor_exceptions.puml)
**Endpoint**: `GET /api/v1/doctor-schedule-exceptions/{doctorId}/{date}`

**Mô tả**: Lấy danh sách ngoại lệ lịch khám của bác sĩ theo ngày (nghỉ phép, khóa slot, mở slot).

**Luồng chính**:
1. User xem danh sách ngoại lệ
2. UI gọi hàm GetDoctorExceptions với doctorId và date
3. Controller chuyển request đến Service
4. Service kiểm tra cache
5. Nếu cache miss, query từ database
6. Map entities sang DTOs và lưu cache
7. Trả về danh sách exceptions

**Đặc điểm**:
- Tương tự pattern Get Doctor Daily Schedule
- Exceptions bao gồm: DAY_OFF, BLOCK_SLOT, UNBLOCK_SLOT

---

### 7. Create Doctor Schedule Exception (07_create_doctor_schedule_exception.puml)
**Endpoint**: `POST /api/v1/doctor-schedule-exceptions`

**Mô tả**: Tạo ngoại lệ lịch khám (nghỉ cả ngày hoặc khóa/mở các slot cụ thể).

**Luồng chính**:
1. User tạo ngoại lệ lịch khám
2. UI gọi hàm CreateDoctorScheduleException với request body
3. Controller chuyển request đến Service
4. Service xử lý theo 2 trường hợp:
   - **No Appointment Times** (nghỉ cả ngày): Tạo 1 exception với appointmentTime = null
   - **With Appointment Times**: Loop qua từng appointment time và tạo exception riêng
5. Repository INSERT từng exception vào database
6. **Invalidate cache**: Xóa cache của exceptions và available slots
7. Map entities sang DTOs và trả về danh sách

**Đặc điểm**:
- Hỗ trợ bulk creation (tạo nhiều exceptions cùng lúc)
- Xử lý 2 patterns: Day off vs Specific time slots
- Cache invalidation cho cả exceptions và available slots

---

### 8. Delete Doctor Schedule Exception (08_delete_doctor_schedule_exception.puml)
**Endpoint**: `DELETE /api/v1/doctor-schedule-exceptions/{id}`

**Mô tả**: Xóa một ngoại lệ lịch khám.

**Luồng chính**:
1. User xóa ngoại lệ
2. UI gọi hàm DeleteDoctorScheduleException với exception id
3. Controller chuyển request đến Service
4. Service gọi Repository để xóa
5. Repository query để tìm exception
6. Nếu tìm thấy, thực hiện DELETE
7. **Invalidate cache**: Xóa cache theo pattern (vì không biết doctorId và date)
8. Trả về success message

**Đặc điểm**:
- Hard delete
- Cache invalidation sử dụng pattern matching vì thiếu thông tin

---

## Quy ước trong Diagram

### Đánh số thứ tự
Mỗi bước trong luồng được đánh số thứ tự kèm dấu chấm (ví dụ: `1.`, `2.`, `3.`...) để dễ dàng theo dõi flow.

### Mô tả trên arrow
- Tên hàm được gọi (không phải URL endpoint)
- Tham số quan trọng (nếu cần)
- Mô tả ngắn gọn bằng tiếng Việt

### Các khối điều kiện
- `alt`: Alternative paths (if-else)
- `opt`: Optional (if)
- `loop`: Lặp

### Activation bars
- Hiển thị thời gian một participant đang xử lý (activated)
- Giúp hiểu rõ flow đồng bộ/bất đồng bộ

## Cache Strategy

Schedule Service sử dụng Redis cache với các cache keys:

- `doctor_daily_schedule:{doctorId}:{date}` - Short expiration (vài phút)
- `doctor_schedule_range:{doctorId}:{startDate}:{endDate}` - Short expiration
- `available_slots:{doctorId}:{date}:{serviceId}` - Short expiration
- `doctor_exceptions:{doctorId}:{date}` - Short expiration
- `clinic_exceptions:{clinicId}:{date}` - Short expiration
- `service_schedules:{serviceId}` - Medium expiration

### Cache Invalidation
Khi có thao tác CREATE/UPDATE/DELETE, cache liên quan sẽ được xóa:
- `RemoveAsync(key)`: Xóa cache key cụ thể
- `RemoveByPatternAsync(pattern)`: Xóa tất cả cache keys match pattern

## External Dependencies

### gRPC Services
1. **Doctor Service**: Validate doctor tồn tại và active
2. **ServiceMedical Service**: Validate medical service tồn tại và active
3. **Appointment Service**: Check booked slots

### Database Tables
1. **DoctorDailySchedules**: Lịch khám hàng ngày của bác sĩ
2. **DoctorScheduleExceptions**: Ngoại lệ lịch khám (nghỉ, khóa slot, mở slot)
3. **ClinicExceptions**: Ngoại lệ của phòng khám
4. **ServiceSchedules**: Lịch hoạt động của dịch vụ y tế

## Xem Diagram

Các file `.puml` có thể được xem bằng:
1. **PlantUML Plugin** trong VS Code
2. **PlantUML Online Server**: http://www.plantuml.com/plantuml/
3. **IntelliJ IDEA** với PlantUML integration plugin

## Lưu ý

- Tất cả các diagram đều được đánh số thứ tự rõ ràng
- User được biểu diễn bằng `actor` (hình người)
- Database được biểu diễn bằng `database` (hình khối trụ)
- UI không phơi ra API endpoint mà gọi function name
- Repository không phơi ra SQL query mà mô tả operation
- Mỗi diagram tách biệt theo từng luồng nghiệp vụ
