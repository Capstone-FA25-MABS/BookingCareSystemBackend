# Unit Test Case Report - UpdateDoctor Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | UpdateDoctor |
| **Function Name** | Update Doctor |
| **Class Name** | DoctorsController |
| **Service** | BookingCare.Services.Doctor |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 22 |
| **Test Coverage** | 6 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 5 |
| **Boundary (B)** | 0 |
| **Total** | 6 |

## Test Case Details

| No | Precondition | Doctor ID | Request Data | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-----------|---------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_UPDATEDOCTOR_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid doctor ID exists.<br>Valid update data provided.<br>User has admin/doctor update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: doctor.updated@hospital.com<br>FirstName: John<br>LastName: Updated<br>Bio: Updated bio<br>YearsOfExperience: 12<br>Gender: MALE<br>Address: 123 Main St | 200: Success | - | "Doctor updated successfully" | N | P | 06/12/2025 | - |
| TC_UPDATEDOCTOR_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent doctor ID.<br>Valid update data provided.<br>User has admin/doctor update permissions. | 00000000-0000-0000-0000-000000000000 | Email: doctor.test@hospital.com<br>FirstName: Test<br>LastName: User<br>YearsOfExperience: 5 | 404: Not Found | DOCTOR_NOT_FOUND_EXCEPTION | "Doctor with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_UPDATEDOCTOR_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid doctor ID exists.<br>Invalid/missing required fields.<br>User has admin/doctor update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: invalid_email<br>FirstName: (empty string)<br>LastName: (exceeds max length) | 400: Bad Request | VALIDATION_ERROR_EXCEPTION | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_UPDATEDOCTOR_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid doctor ID exists.<br>Email already exists for another doctor.<br>User has admin/doctor update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: doctor.existing@hospital.com<br>FirstName: Jane<br>LastName: Existing | 409: Conflict | DUPLICATE_EMAIL_EXCEPTION | "Doctor with email already exists" | A | P | 06/12/2025 | - |
| TC_UPDATEDOCTOR_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid doctor ID exists.<br>Valid update data provided.<br>User lacks admin/doctor update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: doctor.unauthorized@hospital.com<br>FirstName: Bob<br>LastName: Unauthorized<br>YearsOfExperience: 3 | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to update doctor" | A | F | 06/12/2025 | DFID002 |
| TC_UPDATEDOCTOR_006 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid doctor ID exists.<br>Valid update data provided.<br>User has admin/doctor update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Email: doctor.error@hospital.com<br>FirstName: Error<br>LastName: Test<br>YearsOfExperience: 20 | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `PUT /api/v1/doctors/{id}` - Update doctor (line 372-387)
2. `PUT /api/v1/doctors/{id}/upload-avatar` - Update doctor with avatar (line 455-537)

### Error Handling
- ModelState validation cho request data
- Doctor existence validation
- Duplicate email checking với exclude current doctor
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Doctor updated successfully"
- Not Found: StatusCode 404 với message "Doctor with ID {id} not found"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Duplicate Email: StatusCode 409 với message "Doctor with email already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to update doctor"
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
- ⚠️ Có 1 test case failed: TC_UPDATEDOCTOR_005 (Unauthorized access)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Fix failed test case TC_UPDATEDOCTOR_005
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_UPDATEDOCTOR_001 |
| 404 | Not Found | TC_UPDATEDOCTOR_002 |
| 400 | Bad Request | TC_UPDATEDOCTOR_003 |
| 409 | Conflict | TC_UPDATEDOCTOR_004 |
| 401 | Unauthorized | TC_UPDATEDOCTOR_005 |
| 500 | Internal Server Error | TC_UPDATEDOCTOR_006 |

## Conclusion

Báo cáo test case cho function `UpdateDoctor` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 83.33% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Doctor not found được handle đúng với StatusCode 404
- ✅ Duplicate email được handle đúng với StatusCode 409
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_UPDATEDOCTOR_005
