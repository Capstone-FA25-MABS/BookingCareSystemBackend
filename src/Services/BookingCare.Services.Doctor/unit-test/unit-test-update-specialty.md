# Unit Test Case Report - UpdateSpecialty Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | UpdateSpecialty |
| **Function Name** | Update Specialty |
| **Class Name** | SpecialtiesController |
| **Service** | BookingCare.Services.Doctor |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 25 |
| **Test Coverage** | 6 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 5 |
| **Boundary (B)** | 0 |
| **Total** | 6 |

## Test Case Details

| No | Precondition | Specialty ID | Request Data | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|---------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_UPDATESPECIALTY_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid specialty ID exists.<br>Valid update data provided.<br>User has admin/specialty update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Nội khoa (Updated)<br>ImageUrl: https://example.com/images/noi-khoa-updated.jpg<br>Status: ACTIVE | 200: Success | - | "Specialty updated successfully" | N | P | 06/12/2025 | - |
| TC_UPDATESPECIALTY_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent specialty ID.<br>Valid update data provided.<br>User has admin/specialty update permissions. | 00000000-0000-0000-0000-000000000000 | Name: Test Specialty<br>ImageUrl: https://example.com/images/test.jpg<br>Status: ACTIVE | 404: Not Found | SPECIALTY_NOT_FOUND_EXCEPTION | "Specialty with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_UPDATESPECIALTY_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid specialty ID exists.<br>Invalid/missing required fields.<br>User has admin/specialty update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: (exceeds max length)<br>ImageUrl: invalid_url<br>Status: (invalid value) | 400: Bad Request | VALIDATION_ERROR_EXCEPTION | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_UPDATESPECIALTY_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid specialty ID exists.<br>Name already exists for another specialty.<br>User has admin/specialty update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Ngoại khoa<br>ImageUrl: https://example.com/images/ngoai-khoa.jpg<br>Status: ACTIVE | 409: Conflict | SPECIALTY_CONFLICT_EXCEPTION | "Specialty with name already exists" | A | P | 06/12/2025 | - |
| TC_UPDATESPECIALTY_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid specialty ID exists.<br>Valid update data provided.<br>User lacks admin/specialty update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Unauthorized Specialty<br>ImageUrl: https://example.com/images/unauthorized.jpg<br>Status: INACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to update specialty" | A | F | 06/12/2025 | DFID011 |
| TC_UPDATESPECIALTY_006 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid specialty ID exists.<br>Valid update data provided.<br>User has admin/specialty update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Error Specialty<br>ImageUrl: https://example.com/images/error.jpg<br>Status: ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `PUT /api/v1/specialties/{id}` - Update specialty (line 208-224)

### Error Handling
- ModelState validation cho request data
- Specialty existence validation
- Duplicate name checking với exclude current specialty
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Specialty updated successfully"
- Not Found: StatusCode 404 với message "Specialty with ID {id} not found"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Specialty with name already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to update specialty"
- Internal Server Error: StatusCode 500 với message "Internal server error"

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 6
- **Passed**: 5
- **Failed**: 1
- **Pass Rate**: 83.33%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 1 | 16.67% |
| **Abnormal (A)** | 5 | 83.33% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 404, 400, 409, 401, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Có 1 test case failed: TC_UPDATESPECIALTY_005 (Unauthorized access)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Fix failed test case TC_UPDATESPECIALTY_005
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_UPDATESPECIALTY_001 |
| 404 | Not Found | TC_UPDATESPECIALTY_002 |
| 400 | Bad Request | TC_UPDATESPECIALTY_003 |
| 409 | Conflict | TC_UPDATESPECIALTY_004 |
| 401 | Unauthorized | TC_UPDATESPECIALTY_005 |
| 500 | Internal Server Error | TC_UPDATESPECIALTY_006 |

## Conclusion

Báo cáo test case cho function `UpdateSpecialty` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 83.33% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Specialty not found được handle đúng với StatusCode 404
- ✅ Duplicate name được handle đúng với StatusCode 409
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_UPDATESPECIALTY_005
