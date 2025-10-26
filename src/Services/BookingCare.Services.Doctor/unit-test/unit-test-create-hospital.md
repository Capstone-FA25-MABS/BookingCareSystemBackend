# Unit Test Case Report - CreateHospital Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | CreateHospital |
| **Function Name** | Create Hospital |
| **Class Name** | HospitalsController |
| **Service** | BookingCare.Services.Hospital |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 20 |
| **Test Coverage** | 5 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 4 |
| **Boundary (B)** | 0 |
| **Total** | 5 |

## Test Case Details

| No | Precondition | Request Data | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|---------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_CREATEHOSPITAL_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid hospital data provided.<br>User has admin/hospital creation permissions. | Account ID: 7c8b9a4d-2f3e-4a5b-8c9d-1e2f3a4b5c6d<br>Name: Bệnh viện Đa khoa ABC<br>Address: 123 Đường ABC, Quận 1<br>Phone: 028-3822-1234<br>Email: info@hospital.com<br>Description: Bệnh viện đa khoa<br>SpecialtyIds: [a1b2c3d4-e5f6-4a5b-8c9d-1e2f3a4b5c6d, b2c3d4e5-f6a7-5b6c-9d0e-2f3g4h5i6j7k, c3d4e5f6-a7b8-6c7d-0e1f-3g4h5i6j7k8l, d4e5f6g7-b8c9-7d8e-1f2g-4h5i6j7k8l9m] | 201: Created | - | "Hospital created successfully" | N | P | 06/12/2025 | - |
| TC_CREATEHOSPITAL_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/missing required fields.<br>User has admin/hospital creation permissions. | Account ID: 7c8b9a4d-2f3e-4a5b-8c9d-1e2f3a4b5c6d<br>Name: (missing)<br>Email: invalid_email<br>Address: (missing)<br>Description: (missing) | 400: Bad Request | VALIDATION_ERROR | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_CREATEHOSPITAL_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Hospital email already exists.<br>User has admin/hospital creation permissions. | Account ID: 8d9c5e6f-3g4h-5i6j-9k0l-2f3g4h5i6j7k<br>Name: Bệnh viện XYZ<br>Email: existing@hospital.com<br>Address: 456 Đường XYZ<br>Description: Bệnh viện chuyên khoa | 409: Conflict | HOSPITAL_ALREADY_EXISTS_EXCEPTION | "Hospital with email already exists" | A | P | 06/12/2025 | - |
| TC_CREATEHOSPITAL_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid hospital data provided.<br>User lacks admin/hospital creation permissions. | Account ID: 9e0d6g7h-4i5j-6k7l-0m1n-3g4h5i6j7k8l<br>Name: Bệnh viện DEF<br>Email: unauthorized@hospital.com<br>Address: 789 Đường DEF<br>Description: Bệnh viện tư nhân | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to create hospital" | A | P | 06/12/2025 | - |
| TC_CREATEHOSPITAL_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid hospital data provided.<br>User has admin/hospital creation permissions. | Account ID: 0f1e7h8i-5j6k-7l8m-1n2o-4h5i6j7k8l9m<br>Name: Bệnh viện Error<br>Email: error@hospital.com<br>Address: 111 Đường Error<br>Description: Test hospital | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID004 |

## Endpoint Information

### API Endpoints
1. `POST /api/v1/hospitals` - Create hospital (line 153-176)

### Error Handling
- ModelState validation cho request data
- Email uniqueness validation
- Try-catch blocks cho exception handling
- Specific error responses với status codes
- Logging được thực hiện qua ILogger

### Response Format
- Success: StatusCode 201 với message "Hospital created successfully"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Hospital with email already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to create hospital"
- Internal Server Error: StatusCode 500 với message "Internal server error"

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 5
- **Passed**: 4
- **Failed**: 1
- **Pass Rate**: 80%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 1 | 20% |
| **Abnormal (A)** | 4 | 80% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (201, 400, 409, 401, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Có 1 test case failed: TC_CREATEHOSPITAL_005 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Test với database timeout scenarios
- Fix failed test case TC_CREATEHOSPITAL_005

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 201 | Created | TC_CREATEHOSPITAL_001 |
| 400 | Bad Request | TC_CREATEHOSPITAL_002 |
| 409 | Conflict | TC_CREATEHOSPITAL_003 |
| 401 | Unauthorized | TC_CREATEHOSPITAL_004 |
| 500 | Internal Server Error | TC_CREATEHOSPITAL_005 |

## Conclusion

Báo cáo test case cho function `CreateHospital` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 80% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Duplicate email được handle đúng với StatusCode 409
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_CREATEHOSPITAL_005

