# Unit Test Case Report - ToggleStatusDoctor Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | ToggleStatusDoctor |
| **Function Name** | Toggle Doctor Status |
| **Class Name** | DoctorsController |
| **Service** | BookingCare.Services.Doctor |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 12 |
| **Test Coverage** | 4 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 1 |
| **Abnormal (A)** | 3 |
| **Boundary (B)** | 0 |
| **Total** | 4 |

## Test Case Details

| No | Precondition | Doctor ID | Current Status | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-----------|----------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_TOGGLESTATUS_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid doctor ID exists.<br>User has admin/doctor status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 200: Success | - | "Doctor status toggled successfully" | N | P | 06/12/2025 | - |
| TC_TOGGLESTATUS_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent doctor ID.<br>User has admin/doctor status toggle permissions. | 00000000-0000-0000-0000-000000000000 | - | 404: Not Found | DOCTOR_NOT_FOUND_EXCEPTION | "Doctor with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_TOGGLESTATUS_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid doctor ID exists.<br>User lacks admin/doctor status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to toggle doctor status" | A | P | 06/12/2025 | - |
| TC_TOGGLESTATUS_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid doctor ID exists.<br>User has admin/doctor status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID003 |

## Endpoint Information

### API Endpoints
1. `PATCH /api/v1/doctors/{id}/toggle-status` - Toggle doctor status (line 558-569)

### Error Handling
- Doctor existence validation
- Permission checking
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Doctor status toggled successfully"
- Not Found: StatusCode 404 với message "Doctor with ID {id} not found"
- Unauthorized: StatusCode 401 với message "Unauthorized access to toggle doctor status"
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
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 404, 401, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Có 1 test case failed: TC_TOGGLESTATUS_004 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với GUID ở giới hạn (min/max values)
- Test với special characters trong GUID
- Test với GUID của deleted doctors

### 2. Cải thiện Test Coverage
- Fix failed test case TC_TOGGLESTATUS_004
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_TOGGLESTATUS_001 |
| 404 | Not Found | TC_TOGGLESTATUS_002 |
| 401 | Unauthorized | TC_TOGGLESTATUS_003 |
| 500 | Internal Server Error | TC_TOGGLESTATUS_004 |

## Conclusion

Báo cáo test case cho function `ToggleStatusDoctor` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 75% với 1 failed test case cần được fix.

### Key Points:
- ✅ Doctor not found được handle đúng với StatusCode 404
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_TOGGLESTATUS_004
