# Unit Test Case Report - UpdateHospital Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | UpdateHospital |
| **Function Name** | Update Hospital |
| **Class Name** | HospitalsController |
| **Service** | BookingCare.Services.Hospital |
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

| No | Precondition | Hospital ID | Request Data | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|---------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_UPDATEHOSPITAL_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid hospital ID exists.<br>Valid update data provided.<br>User has admin/hospital update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Bệnh viện Updated<br>Phone: 028-3822-9999<br>Email: updated@hospital.com<br>Description: Updated description<br>Address: 999 New Street<br>SpecialtyIds: [e5f6g7h8-c9d0-8e9f-2g3h-5i6j7k8l9m0n, f6g7h8i9-d0e1-9f0g-3h4i-6j7k8l9m0n1o, g7h8i9j0-e1f2-0g1h-4i5j-7k8l9m0n1o2p, h8i9j0k1-f2g3-1h2i-5j6k-8l9m0n1o2p3q] | 200: Success | - | "Hospital updated successfully" | N | P | 06/12/2025 | - |
| TC_UPDATEHOSPITAL_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent hospital ID.<br>Valid update data provided.<br>User has admin/hospital update permissions. | 00000000-0000-0000-0000-000000000000 | Name: Test Hospital<br>Email: test@hospital.com<br>Address: Test Address | 404: Not Found | HOSPITAL_NOT_FOUND_EXCEPTION | "Hospital with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_UPDATEHOSPITAL_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid hospital ID exists.<br>Invalid/missing required fields.<br>User has admin/hospital update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: invalid_email<br>Name: (exceeds max length)<br>Phone: (invalid format) | 400: Bad Request | VALIDATION_ERROR_EXCEPTION | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_UPDATEHOSPITAL_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid hospital ID exists.<br>Email already exists for another hospital.<br>User has admin/hospital update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: existing@hospital.com<br>Name: Updated Hospital<br>Address: New Address | 409: Conflict | HOSPITAL_ALREADY_EXISTS_EXCEPTION | "Hospital with email already exists" | A | P | 06/12/2025 | - |
| TC_UPDATEHOSPITAL_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid hospital ID exists.<br>Valid update data provided.<br>User lacks admin/hospital update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Unauthorized Hospital<br>Email: unauthorized@hospital.com<br>Phone: 028-1111-2222 | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to update hospital" | A | F | 06/12/2025 | DFID005 |
| TC_UPDATEHOSPITAL_006 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid hospital ID exists.<br>Valid update data provided.<br>User has admin/hospital update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Error Hospital<br>Email: error@hospital.com<br>Description: Test update | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `PUT /api/v1/hospitals/{id}` - Update hospital (line 178-212)

### Error Handling
- ModelState validation cho request data
- Hospital existence validation
- Duplicate email checking với exclude current hospital
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Hospital updated successfully"
- Not Found: StatusCode 404 với message "Hospital with ID {id} not found"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Hospital with email already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to update hospital"
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
- ⚠️ Có 1 test case failed: TC_UPDATEHOSPITAL_005 (Unauthorized access)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Fix failed test case TC_UPDATEHOSPITAL_005
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_UPDATEHOSPITAL_001 |
| 404 | Not Found | TC_UPDATEHOSPITAL_002 |
| 400 | Bad Request | TC_UPDATEHOSPITAL_003 |
| 409 | Conflict | TC_UPDATEHOSPITAL_004 |
| 401 | Unauthorized | TC_UPDATEHOSPITAL_005 |
| 500 | Internal Server Error | TC_UPDATEHOSPITAL_006 |

## Conclusion

Báo cáo test case cho function `UpdateHospital` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 83.33% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Hospital not found được handle đúng với StatusCode 404
- ✅ Duplicate email được handle đúng với StatusCode 409
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_UPDATEHOSPITAL_005

