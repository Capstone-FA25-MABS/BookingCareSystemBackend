# Unit Test Case Report - SearchHospitalsFilter Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | SearchHospitalsFilter |
| **Function Name** | Search Hospitals (Optimized Filter) |
| **Class Name** | HospitalsController |
| **Service** | BookingCare.Services.Hospital |
| **Created By** | TuDT |
| **Executed By** | TuDT |
| **Lines of code** | 30 |
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
| TC_SEARCHHOSPITALSFILTER_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>Hospitals exist matching criteria. | Search: Bệnh viện<br>SpecialtyIds: [a1b2c3d4-e5f6-4a5b-8c9d-1e2f3a4b5c6d, b2c3d4e5-f6a7-5b6c-9d0e-2f3g4h5i6j7k]<br>ProvinceId: 79<br>Page: 1<br>PageSize: 10 | 200: Success | - | "Hospital list retrieved successfully" | N | P | 06/12/2025 | - |
| TC_SEARCHHOSPITALSFILTER_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid filter criteria provided. | Search: (very long string exceeding max length)<br>SpecialtyIds: [invalid-guid-format]<br>Page: -1<br>PageSize: -1 | 400: Bad Request | VALIDATION_ERROR | "Invalid filter criteria" | A | P | 06/12/2025 | - |
| TC_SEARCHHOSPITALSFILTER_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>No hospitals match criteria. | Search: NonExistentHospitalName<br>SpecialtyIds: [00000000-0000-0000-0000-000000000000]<br>Page: 1<br>PageSize: 10 | 200: Success (Empty) | - | "Hospital list retrieved successfully" | A | P | 06/12/2025 | - |
| TC_SEARCHHOSPITALSFILTER_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>Database connection issues. | Search: Bệnh viện<br>SpecialtyIds: [a1b2c3d4-e5f6-4a5b-8c9d-1e2f3a4b5c6d]<br>Page: 1<br>PageSize: 10 | 500: Internal Server Error | INTERNAL_SERVER_ERROR | "Internal server error" | A | F | 06/12/2025 | DFID014 |
| TC_SEARCHHOSPITALSFILTER_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid specialty IDs provided. | Search: Test<br>SpecialtyIds: [99999999-9999-9999-9999-999999999999]<br>Page: 1<br>PageSize: 10 | 200: Success (Empty) | - | "Hospital list retrieved successfully" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `GET /api/v1/hospitals/list` - Search hospitals with optimized filter (line 60-78)

### Error Handling
- ModelState validation cho request data
- Database query với multiple filters
- Specialty ID validation via gRPC
- Try-catch blocks cho exception handling
- Specific error responses với status codes
- Logging được thực hiện qua ILogger

### Response Format
- Success: StatusCode 200 với message "Hospital list retrieved successfully"
- Validation Error: StatusCode 400 với message "Invalid filter criteria"
- Internal Server Error: StatusCode 500 với message "Internal server error"
- Empty result: StatusCode 200 (vẫn là success case)

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
- ✅ Kiểm tra đầy đủ các status code khác nhau (200, 400, 500)
- ✅ Có log messages phù hợp cho từng trường hợp
- ✅ Kiểm tra validation của specialty IDs qua gRPC
- ⚠️ Có 1 test case failed: TC_SEARCHHOSPITALSFILTER_004 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với multiple specialty filter combinations
- Test với large result sets
- Test với extreme pagination values
- Test với very long search strings

### 2. Cải thiện Test Coverage
- Fix failed test case TC_SEARCHHOSPITALSFILTER_004
- Test với database timeout scenarios
- Test với gRPC communication failures

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_SEARCHHOSPITALSFILTER_001, TC_SEARCHHOSPITALSFILTER_003, TC_SEARCHHOSPITALSFILTER_005 |
| 400 | Bad Request | TC_SEARCHHOSPITALSFILTER_002 |
| 500 | Internal Server Error | TC_SEARCHHOSPITALSFILTER_004 |

## Conclusion

Báo cáo test case cho function `SearchHospitalsFilter` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 80% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Empty result vẫn là success case (StatusCode 200) - đúng theo REST API standards
- ✅ Specialty ID validation được handle qua gRPC communication
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_SEARCHHOSPITALSFILTER_004
