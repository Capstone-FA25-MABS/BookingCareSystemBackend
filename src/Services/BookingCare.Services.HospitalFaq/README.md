# BookingCare Hospital FAQ Service

Service quản lý FAQs (Frequently Asked Questions) cho các bệnh viện trong hệ thống BookingCare.

## Tổng quan

Service này cho phép:
- Staff tạo và quản lý FAQs cho từng bệnh viện
- Hiển thị FAQs trên trang profile của bệnh viện
- Sắp xếp FAQs theo thứ tự hiển thị (display_order)

## Cấu trúc Database

### Bảng `hospital_faqs`

| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `id` | uniqueidentifier | Primary key |
| `hospital_id` | uniqueidentifier | ID của bệnh viện |
| `question` | nvarchar(max) | Câu hỏi |
| `answer` | nvarchar(max) | Câu trả lời |
| `created_by` | uniqueidentifier | Account ID của staff tạo FAQ |
| `display_order` | int | Thứ tự hiển thị (mặc định: 0) |
| `created_at` | datetime2 | Thời gian tạo |
| `updated_at` | datetime2 | Thời gian cập nhật |

**Indexes:**
- `IX_hospital_faqs_hospital_id` trên `hospital_id`
- `IX_hospital_faqs_hospital_id_display_order` trên `hospital_id, display_order`

## API Endpoints

### GET `/api/v1/hospital-faqs`
Lấy danh sách FAQs với filter tùy chọn.

**Query Parameters:**
- `hospitalId` (Guid, optional): Lọc theo hospital ID
- `pageNumber` (int, default: 1): Số trang
- `pageSize` (int, default: 10): Số items mỗi trang

**Response:** `HospitalFaqListResponse`

### GET `/api/v1/hospital-faqs/hospital/{hospitalId}`
Lấy tất cả FAQs của một bệnh viện cụ thể.

**Response:** `List<HospitalFaqResponse>`

### GET `/api/v1/hospital-faqs/{id}`
Lấy một FAQ cụ thể theo ID.

**Response:** `HospitalFaqResponse`

### POST `/api/v1/hospital-faqs`
Tạo FAQ mới (Yêu cầu: Staff hoặc Admin role).

**Request Body:** `CreateHospitalFaqRequest`
```json
{
  "hospitalId": "guid",
  "question": "Câu hỏi?",
  "answer": "Câu trả lời",
  "displayOrder": 0
}
```

**Response:** `HospitalFaqResponse` (201 Created)

### PUT `/api/v1/hospital-faqs/{id}`
Cập nhật FAQ (Yêu cầu: Staff hoặc Admin role).

**Request Body:** `UpdateHospitalFaqRequest`
```json
{
  "question": "Câu hỏi đã cập nhật?",
  "answer": "Câu trả lời đã cập nhật",
  "displayOrder": 1
}
```

**Response:** `HospitalFaqResponse`

### DELETE `/api/v1/hospital-faqs/{id}`
Xóa FAQ (Yêu cầu: Staff hoặc Admin role).

**Response:** 204 No Content

## Cấu hình

### Database Connection String
Cấu hình trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MABS_HospitalFaq;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### Port
Mặc định chạy trên port **6060** (Development).

## Migration

Để tạo migration mới:
```bash
dotnet ef migrations add MigrationName --project BookingCare.Services.HospitalFaq
```

Để apply migrations:
```bash
dotnet ef database update --project BookingCare.Services.HospitalFaq
```

## Sử dụng trong Frontend

Để hiển thị FAQs trên Hospital Profile:

1. Gọi API: `GET /api/v1/hospital-faqs/hospital/{hospitalId}`
2. Hiển thị danh sách FAQs đã được sắp xếp theo `display_order`
3. Sử dụng component tương tự như trong `HospitalProfile.tsx` (section FAQ)

## Lưu ý

- FAQs được sắp xếp theo `display_order` (tăng dần), sau đó theo `created_at`
- Chỉ Staff và Admin mới có quyền tạo/sửa/xóa FAQs
- FAQs được liên kết với hospital thông qua `hospital_id`

