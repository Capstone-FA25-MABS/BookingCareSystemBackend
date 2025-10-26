# Unit Test Case Report - TogglePositionStatus Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | TogglePositionStatus |
| **Function Name** | Toggle Position Status |
| **Class Name** | PositionsController |
| **Service** | BookingCare.Services.Doctor |
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

| No | Precondition | Position ID | Current Status | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|----------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_TOGGLEPOSITIONSTATUS_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid position ID exists.<br>User has admin/position status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 200: Success | - | "Position status toggled successfully" | N | P | 06/12/2025 | - |
| TC_TOGGLEPOSITIONSTATUS_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent position ID.<br>User has admin/position status toggle permissions. | 00000000-0000-0000-0000-000000000000 | - | 404: Not Found | POSITION_NOT_FOUND_EXCEPTION | "Position with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_TOGGLEPOSITIONSTATUS_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid position ID exists.<br>User lacks admin/position status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to toggle position status" | A | P | 06/12/2025 | - |
| TC_TOGGLEPOSITIONSTATUS_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid position ID exists.<br>User has admin/position status toggle permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID009 |

## Endpoint Information

### API Endpoints
1. `PATCH /api/v1/positions/{id}/toggle-status` - Toggle position status (line 174-188)

### Error Handling
- Position existence validation
- Permission checking
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Position status toggled successfully"
- Not Found: StatusCode 404 với message "Position with ID {id} not found"
- Unauthorized: StatusCode 401 với message "Unauthorized access to toggle position status"
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
- ⚠️ Có 1 test case failed: TC_TOGGLEPOSITIONSTATUS_004 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với GUID ở giới hạn (min/max values)
- Test với special characters trong GUID
- Test với positions đã được toggle nhiều lần

### 2. Cải thiện Test Coverage
- Fix failed test case TC_TOGGLEPOSITIONSTATUS_004
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_TOGGLEPOSITIONSTATUS_001 |
| 404 | Not Found | TC_TOGGLEPOSITIONSTATUS_002 |
| 401 | Unauthorized | TC_TOGGLEPOSITIONSTATUS_003 |
| 500 | Internal Server Error | TC_TOGGLEPOSITIONSTATUS_004 |

## Conclusion

Báo cáo test case cho function `TogglePositionStatus` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 75% với 1 failed test case cần được fix.

### Key Points:
- ✅ Position not found được handle đúng với StatusCode 404
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_TOGGLEPOSITIONSTATUS_004

