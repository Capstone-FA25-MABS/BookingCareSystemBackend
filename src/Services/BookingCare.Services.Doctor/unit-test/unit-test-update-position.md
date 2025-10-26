# Unit Test Case Report - UpdatePosition Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | UpdatePosition |
| **Function Name** | Update Position |
| **Class Name** | PositionsController |
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

| No | Precondition | Position ID | Request Data | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|---------------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_UPDATEPOSITION_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid position ID exists.<br>Valid update data provided.<br>User has admin/position update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Phó Giáo sư (Updated)<br>Status: ACTIVE | 200: Success | - | "Position updated successfully" | N | P | 06/12/2025 | - |
| TC_UPDATEPOSITION_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid/non-existent position ID.<br>Valid update data provided.<br>User has admin/position update permissions. | 00000000-0000-0000-0000-000000000000 | Name: Test Position<br>Status: ACTIVE | 404: Not Found | POSITION_NOT_FOUND_EXCEPTION | "Position with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_UPDATEPOSITION_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid position ID exists.<br>Invalid/missing required fields.<br>User has admin/position update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: (exceeds max length)<br>Status: (invalid value) | 400: Bad Request | VALIDATION_ERROR_EXCEPTION | "Invalid request data" | A | P | 06/12/2025 | - |
| TC_UPDATEPOSITION_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid position ID exists.<br>Name already exists for another position.<br>User has admin/position update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Giáo sư<br>Status: ACTIVE | 409: Conflict | POSITION_CONFLICT_EXCEPTION | "Position with name already exists" | A | P | 06/12/2025 | - |
| TC_UPDATEPOSITION_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Valid position ID exists.<br>Valid update data provided.<br>User lacks admin/position update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Unauthorized Position<br>Status: INACTIVE | 401: Unauthorized | UNAUTHORIZED_ACCESS_EXCEPTION | "Unauthorized access to update position" | A | F | 06/12/2025 | DFID008 |
| TC_UPDATEPOSITION_006 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues.<br>Valid position ID exists.<br>Valid update data provided.<br>User has admin/position update permissions. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | Name: Error Position<br>Status: ACTIVE | 500: Internal Server Error | INTERNAL_SERVER_ERROR_EXCEPTION | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `PUT /api/v1/positions/{id}` - Update position (line 143-163)

### Error Handling
- ModelState validation cho request data
- Position existence validation
- Duplicate name checking với exclude current position
- Try-catch blocks cho exception handling

### Response Format
- Success: StatusCode 200 với message "Position updated successfully"
- Not Found: StatusCode 404 với message "Position with ID {id} not found"
- Validation Error: StatusCode 400 với message "Invalid request data"
- Conflict: StatusCode 409 với message "Position with name already exists"
- Unauthorized: StatusCode 401 với message "Unauthorized access to update position"
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
- ⚠️ Có 1 test case failed: TC_UPDATEPOSITION_005 (Unauthorized access)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với data ở giới hạn (min/max values)
- Test với special characters trong fields
- Test với very long strings

### 2. Cải thiện Test Coverage
- Fix failed test case TC_UPDATEPOSITION_005
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_UPDATEPOSITION_001 |
| 404 | Not Found | TC_UPDATEPOSITION_002 |
| 400 | Bad Request | TC_UPDATEPOSITION_003 |
| 409 | Conflict | TC_UPDATEPOSITION_004 |
| 401 | Unauthorized | TC_UPDATEPOSITION_005 |
| 500 | Internal Server Error | TC_UPDATEPOSITION_006 |

## Conclusion

Báo cáo test case cho function `UpdatePosition` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 83.33% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Position not found được handle đúng với StatusCode 404
- ✅ Duplicate name được handle đúng với StatusCode 409
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_UPDATEPOSITION_005

