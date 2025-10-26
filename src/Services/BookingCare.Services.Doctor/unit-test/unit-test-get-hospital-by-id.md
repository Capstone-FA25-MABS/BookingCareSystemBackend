# Unit Test Case Report - GetHospitalById Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | GetHospitalById |
| **Function Name** | Get Hospital By Id |
| **Class Name** | HospitalsController |
| **Service** | BookingCare.Services.Hospital |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 25 |
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
| TC_GETHOSPITALBYID_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid hospital ID exists. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 200: Success | - | "Hospital retrieved successfully" | N | P | 06/12/2025 | - |
| TC_GETHOSPITALBYID_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Invalid/non-existent hospital ID. | 00000000-0000-0000-0000-000000000000 | 404: Not Found | HOSPITAL_NOT_FOUND_EXCEPTION | "Hospital with ID {id} not found" | A | P | 06/12/2025 | - |
| TC_GETHOSPITALBYID_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid GUID format in URL. | invalid-format | 400: Bad Request | VALIDATION_ERROR | "Invalid hospital ID format" | A | P | 06/12/2025 | - |
| TC_GETHOSPITALBYID_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database connection issues. | 146819aa-3a9e-4fc3-b53e-01e3292ae215 | 500: Internal Server Error | INTERNAL_SERVER_ERROR | "Internal server error" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `GET /api/v1/hospitals/{id}` - Get hospital by ID (line 86-126)

### Error Handling
- Sử dụng exception handling cho hospital not found
- Validation cho GUID format
- Exception handling với try-catch blocks

### Response Format
- Success: StatusCode 200 với message "Hospital retrieved successfully"
- Not Found: StatusCode 404 với message "Hospital with ID {id} not found"
- Bad Request: StatusCode 400 với message "Invalid hospital ID format"
- Internal Server Error: StatusCode 500 với message "Internal server error"

## Test Results Analysis

### Pass Rate
- **Total Test Cases**: 4
- **Passed**: 4
- **Failed**: 0
- **Pass Rate**: 100%

### Test Case Distribution
| Type | Count | Percentage |
|------|-------|------------|
| **Normal (N)** | 1 | 25% |
| **Abnormal (A)** | 3 | 75% |
| **Boundary (B)** | 0 | 0% |

### Key Findings
- ✅ Tất cả 4 test cases đều passed
- ✅ Có đầy đủ test cases cho các trường hợp Normal và Abnormal
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 404, 400, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với GUID ở giới hạn (min/max values)
- Test với special characters trong GUID
- Test với GUID của deleted hospitals

### 2. Cải thiện Test Coverage
- Test với database timeout scenarios
- Test với memory pressure situations

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_GETHOSPITALBYID_001 |
| 404 | Not Found | TC_GETHOSPITALBYID_002 |
| 400 | Bad Request | TC_GETHOSPITALBYID_003 |
| 500 | Internal Server Error | TC_GETHOSPITALBYID_004 |

## Conclusion

Báo cáo test case cho function `GetHospitalById` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Tất cả test cases đều passed với pass rate 100%.

### Key Points:
- ✅ Not Found được handle đúng với StatusCode 404
- ✅ Invalid GUID format được handle đúng với StatusCode 400
- ✅ Internal server error được handle đúng với StatusCode 500
- ✅ Log messages phù hợp cho từng trường hợp
- ✅ Test coverage tốt cho các trường hợp chính của function

