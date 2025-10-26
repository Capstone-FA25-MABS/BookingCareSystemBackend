# Unit Test Case Report - CreatePosition Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | CreatePosition |
| **Function Name** | Create Position |
| **Class Name** | PositionsController |
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
| TC_CREATEPOSITION_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid position data provided.<br>User has admin/position creation permissions. | Name: Phó Giáo sư<br>Status: ACTIVE | 201: Created | - | "Position created successfully" | N | P | 06/12/2025 | - |
| TC_CREATEPOSITION_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/missing required fields.<br>User has admin/position creation permissions. | Name: (missing)<br>Status: (missing) | 400: Bad Request | VALIDATION_ERROR | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_CREATEPOSITION_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Position name already exists.<br>User has admin/position creation permissions. | Name: Giáo sư<br>Status: ACTIVE | 409: Conflict | POSITION_CONFLICT_EXCEPTION | "Position with name already exists" | A | P | 06/12/2025 | - |
| TC_CREATEPOSITION_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid position data provided.<br>User lacks admin/position creation permissions. | Name: Tiến sĩ<br>Status: ACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to create position" | A | P | 06/12/2025 | - |
| TC_CREATEPOSITION_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid position data provided.<br>User has admin/position creation permissions. | Name: Thạc sĩ<br>Status: ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | F | 06/12/2025 | DFID007 |

## Endpoint Information

### API Endpoints
1. `POST /api/v1/positions` - Create position (line 123-141)

### Error Handling
- ModelState validation cho request data
- Position name uniqueness validation
- Try-catch blocks cho exception handling
- Specific error responses với status codes
- Logging được thực hiện qua ILogger

### Response Format
- Success: StatusCode 201 với message "Position created successfully"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Position with name already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to create position"
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
- ⚠️ Có 1 test case failed: TC_CREATEPOSITION_005 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Test với database timeout scenarios
- Fix failed test case TC_CREATEPOSITION_005

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 201 | Created | TC_CREATEPOSITION_001 |
| 400 | Bad Request | TC_CREATEPOSITION_002 |
| 409 | Conflict | TC_CREATEPOSITION_003 |
| 401 | Unauthorized | TC_CREATEPOSITION_004 |
| 500 | Internal Server Error | TC_CREATEPOSITION_005 |

## Conclusion

Báo cáo test case cho function `CreatePosition` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 80% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Duplicate name được handle đúng với StatusCode 409
- ✅ Unauthorized access được handle đúng với StatusCode 401
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_CREATEPOSITION_005

