# Unit Test Case Report - CreateSpecialty Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | CreateSpecialty |
| **Function Name** | Create Specialty |
| **Class Name** | SpecialtiesController |
| **Service** | BookingCare.Services.Doctor |
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
| TC_CREATESPECIALTY_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid specialty data provided.<br>User has admin/specialty creation permissions. | Name: Nội khoa<br>ImageUrl: https://example.com/images/noi-khoa.jpg<br>Status: ACTIVE | 201: Created | - | "Specialty created successfully" | N | P | 06/12/2025 | - |
| TC_CREATESPECIALTY_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/missing required fields.<br>User has admin/specialty creation permissions. | Name: (missing)<br>ImageUrl: invalid_url<br>Status: (missing) | 400: Bad Request | VALIDATION_ERROR | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_CREATESPECIALTY_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Specialty name already exists.<br>User has admin/specialty creation permissions. | Name: Ngoại khoa<br>ImageUrl: https://example.com/images/ngoai-khoa.jpg<br>Status: ACTIVE | 409: Conflict | SPECIALTY_CONFLICT_EXCEPTION | "Specialty with name already exists" | A | P | 06/12/2025 | - |
| TC_CREATESPECIALTY_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid specialty data provided.<br>User lacks admin/specialty creation permissions. | Name: Nhi khoa<br>ImageUrl: https://example.com/images/nhi-khoa.jpg<br>Status: ACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to create specialty" | A | P | 06/12/2025 | - |
| TC_CREATESPECIALTY_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid specialty data provided.<br>User has admin/specialty creation permissions. | Name: Da liễu<br>ImageUrl: https://example.com/images/da-lieu.jpg<br>Status: ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID010 |

## Endpoint Information

### API Endpoints
1. `POST /api/v1/specialties` - Create specialty (line 147-163)

### Error Handling
- ModelState validation cho request data
- Specialty name uniqueness validation
- Try-catch blocks cho exception handling
- Specific error responses với status codes
- Logging được thực hiện qua ILogger

### Response Format
- Success: StatusCode 201 với message "Specialty created successfully"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Specialty with name already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to create specialty"
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
- ⚠️ Có 1 test case failed: TC_CREATESPECIALTY_005 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Test với database timeout scenarios
- Fix failed test case TC_CREATESPECIALTY_005

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 201 | Created | TC_CREATESPECIALTY_001 |
| 400 | Bad Request | TC_CREATESPECIALTY_002 |
| 409 | Conflict | TC_CREATESPECIALTY_003 |
| 401 | Unauthorized | TC_CREATESPECIALTY_004 |
| 500 | Internal Server Error | TC_CREATESPECIALTY_005 |

## Conclusion

Báo cáo test case cho function `CreateSpecialty` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 80% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Duplicate name được handle đúng với StatusCode 409
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_CREATESPECIALTY_005
