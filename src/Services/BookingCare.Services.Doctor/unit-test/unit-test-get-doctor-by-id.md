# Unit Test Case Report - GetDoctorById Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | GetDoctorById |
| **Function Name** | Get Doctor By Id |
| **Class Name** | DoctorsController |
| **Service** | BookingCare.Services.Doctor |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 19 |
| **Test Coverage** | 4 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 3 |
| **Boundary (B)** | 0 |
| **Total** | 4 |

## Test Case Details

| No | Precondition | Doctor ID | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-----------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_GETDOCTORBYID_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid doctor ID exists. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 200: Success | - | "Doctor retrieved successfully" | N | P | 06/12/2025 | - |
| TC_GETDOCTORBYID_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Invalid/non-existent doctor ID. | 00000000-0000-0000-0000-000000000000 | 404: Not Found | NOT_FOUND_DOCTOR_WITH_ID | "Doctor with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_GETDOCTORBYID_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid GUID format in URL. | invalid-format | 400: Bad Request | VALIDATION_ERROR | "Invalid doctor ID format" | A | P | 06/12/2025 | - |
| TC_GETDOCTORBYID_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 500: Internal Server Error | INTERNAL_SERVER_ERROR | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `GET /api/v1/doctors/{id}` - Get doctor by ID (line 71-82)
2. `GET /api/v1/doctors/by-email/{email}` - Get doctor by email (line 87-98)
3. `GET /api/v1/doctors/by-account/{accountId}` - Get doctor by account ID

### Error Handling
- Sử dụng `NotFound()` method cho 404 responses
- Validation cho GUID format trong gRPC service
- Exception handling với try-catch blocks

### Response Format
- Success: StatusCode 200 với message "Doctor retrieved successfully"
- Not Found: StatusCode 404 với message "Doctor with ID {id} not found"
- Bad Request: StatusCode 400 với message "Invalid doctor ID format"
- Internal Server Error: StatusCode 500 với message "Internal server error"

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 4
- **Passed**: 4
- **Failed**: 0
- **Pass Rate**: 100%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 1 | 25% |
| **Abnormal (A)** | 3 | 75% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Tất cả 4 test cases đều passed
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 404, 400, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với GUID ở giới hạn (min/max values)
- Test với special characters trong GUID
- Test với GUID của deleted doctors

### 2. Cải thiện Test Coverage
- Test với database timeout scenarios
- Test với memory pressure situations

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_GETDOCTORBYID_001 |
| 404 | Not Found | TC_GETDOCTORBYID_002 |
| 400 | Bad Request | TC_GETDOCTORBYID_003 |
| 500 | Internal Server Error | TC_GETDOCTORBYID_004 |

## Conclusion

Báo cáo test case cho function `GetDoctorById` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Tất cả test cases đều passed với pass rate 100%.

### Key Points:
- ✅ Not Found được handle đúng với StatusCode 404
- ✅ Invalid GUID format được handle đúng với StatusCode 400
- ✅ Internal server error được handle đúng với StatusCode 500
- ✅ Log messages phù hợp cho từng trường hợp
- ✅ Test coverage tốt cho các trường hợp chính của function
