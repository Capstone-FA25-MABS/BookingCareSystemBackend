# Unit Test Case Report - GetPositions Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | GetPositions |
| **Function Name** | Get List Positions |
| **Class Name** | PositionsController |
| **Service** | BookingCare.Services.Doctor |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 30 |
| **Test Coverage** | 3 test cases |

## Tóm tắt Test Requirements

| Type | Count |
|------|-------|
| **Normal (N)** | 2 |
| **Abnormal (A)** | 1 |
| **Boundary (B)** | 0 |
| **Total** | 3 |

## Test Case Details

| No | Precondition | Page Number | Page Size | Return | Exception | Log Message | Type | Passed/Failed | Executed Date | Defect ID |
|----|-------------|-------------|-----------|--------|-----------|-------------|------|---------------|---------------|-----------|
| TC_GETPOSITIONS_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Positions exist in database. | 1 | 10 | 200: Success | - | "Positions retrieved successfully" | N | P | 06/12/2025 | - |
| TC_GETPOSITIONS_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>No positions in database. | 1 | 10 | 200: Success | - | "Positions retrieved successfully" | N | P | 06/12/2025 | - |
| TC_GETPOSITIONS_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues. | 1 | 10 | 500: Internal Server Error | INTERNAL_SERVER_ERROR | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `GET /api/v1/positions` - Get all positions with pagination (line 82-87)

### Error Handling
- Sử dụng try-catch blocks cho exception handling
- Logging được thực hiện qua ILogger
- Exception được re-throw sau khi log

### Response Format
- Success: StatusCode 200 với message "Positions retrieved successfully"
- Error: StatusCode 500 với message "Internal server error"
- Empty result: StatusCode 200 (vẫn là success case)

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 3
- **Passed**: 3
- **Failed**: 0
- **Pass Rate**: 100%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 2 | 66.67% |
| **Abnormal (A)** | 1 | 33.33% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Tất cả 3 test cases đều passed
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ✅ Phân biệt rõ ràng giữa success case (empty result) và error case
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với danh sách positions có số lượng lớn
- Test với input parameters ở giới hạn
- Test với timeout scenarios

### 2. Cải thiện Test Coverage
- Thêm test cases cho các trường hợp lỗi network
- Test với các loại authentication khác nhau
- Test với database connection issues

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_GETPOSITIONS_001, TC_GETPOSITIONS_002 |
| 500 | Internal Server Error | TC_GETPOSITIONS_003 |

## Conclusion

Báo cáo test case cho function `GetPositions` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Tất cả test cases đều passed với pass rate 100%.

### Key Points:
- ✅ Empty result vẫn là success case (StatusCode 200) - đúng theo REST API standards
- ✅ Internal server error được handle đúng với StatusCode 500
- ✅ Log messages phù hợp cho từng trường hợp
- ✅ Test coverage tốt cho các trường hợp chính của function
