# Unit Test Case Report - DeleteHospital Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | DeleteHospital |
| **Function Name** | Delete Hospital |
| **Class Name** | HospitalsController |
| **Service** | BookingCare.Services.Hospital |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 20 |
| **Test Coverage** | 4 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 3 |
| **Boundary (B)** | 0 |
| **Total** | 4 |

## Test Case Details

| No | Precondition | Hospital ID | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_DELETEHOSPITAL_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid hospital ID exists.<br>User has admin/hospital delete permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 204: No Content | - | "Hospital deleted successfully" | N | P | 06/12/2025 | - |
| TC_DELETEHOSPITAL_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent hospital ID.<br>User has admin/hospital delete permissions. | 00000000-0000-0000-0000-000000000000 | 404: Not Found | HOSPITAL_NOT_FOUND_EXCEPTION | "Hospital with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_DELETEHOSPITAL_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid hospital ID exists.<br>User lacks admin/hospital delete permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to delete hospital" | A | P | 06/12/2025 | - |
| TC_DELETEHOSPITAL_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid hospital ID exists.<br>User has admin/hospital delete permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID006 |

## Endpoint Information

### API Endpoints
1. `DELETE /api/v1/hospitals/{id}` - Delete hospital (line 214-232)

### Error Handling
- Hospital existence validation
- Permission checking
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 204 với message "Hospital deleted successfully"
- Not Found: StatusCode 404 với message "Hospital with ID {id} not found"
- Unauthorized: StatusCode 401 với message "Unauthorized access to delete hospital"
- Internal Server Error: StatusCode 500 với message "Internal server error"

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 4
- **Passed**: 3
- **Failed**: 1
- **Pass Rate**: 75%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 1 | 25% |
| **Abnormal (A)** | 3 | 75% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (204, 404, 401, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Có 1 test case failed: TC_DELETEHOSPITAL_004 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với GUID ở giới hạn (min/max values)
- Test với special characters trong GUID
- Test với GUID của already deleted hospitals

### 2. Cải thiện Test Coverage
- Fix failed test case TC_DELETEHOSPITAL_004
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 204 | No Content | TC_DELETEHOSPITAL_001 |
| 404 | Not Found | TC_DELETEHOSPITAL_002 |
| 401 | Unauthorized | TC_DELETEHOSPITAL_003 |
| 500 | Internal Server Error | TC_DELETEHOSPITAL_004 |

## Conclusion

Báo cáo test case cho function `DeleteHospital` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 75% với 1 failed test case cần được fix.

### Key Points:
- ✅ Hospital not found được handle đúng với StatusCode 404
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_DELETEHOSPITAL_004

