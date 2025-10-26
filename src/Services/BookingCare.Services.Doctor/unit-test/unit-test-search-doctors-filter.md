# Unit Test Case Report - SearchDoctorsFilter Function

## Thông tin Test Function

| Thuộc tính | Giá trị |
|------------|---------|
| **Function Code** | SearchDoctorsFilter |
| **Function Name** | Search Doctors (Advanced Filter) |
| **Class Name** | DoctorsController |
| **Service** | BookingCare.Services.Doctor |
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
| TC_SEARCHDOCTORSFILTER_001 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>Doctors exist matching criteria. | SpecialtyId: a1b2c3d4-e5f6-4a5b-8c9d-1e2f3a4b5c6d<br>Gender: MALE<br>MinYearsOfExperience: 5<br>ProvinceId: 79<br>PageNumber: 1<br>PageSize: 10 | 200: Success | - | "Doctors filtered successfully" | N | P | 06/12/2025 | - |
| TC_SEARCHDOCTORSFILTER_002 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Invalid filter criteria provided. | SpecialtyId: invalid-guid<br>Gender: INVALID_GENDER<br>MinYearsOfExperience: -5<br>PageNumber: 0<br>PageSize: 0 | 400: Bad Request | VALIDATION_ERROR | "Invalid filter criteria" | A | P | 06/12/2025 | - |
| TC_SEARCHDOCTORSFILTER_003 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>No doctors match criteria. | SpecialtyId: 00000000-0000-0000-0000-000000000000<br>Gender: MALE<br>MinYearsOfExperience: 100<br>PageNumber: 1<br>PageSize: 10 | 200: Success (Empty) | - | "Doctors filtered successfully" | A | P | 06/12/2025 | - |
| TC_SEARCHDOCTORSFILTER_004 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Valid filter criteria provided.<br>Database connection issues. | SpecialtyId: a1b2c3d4-e5f6-4a5b-8c9d-1e2f3a4b5c6d<br>Gender: FEMALE<br>PageNumber: 1<br>PageSize: 10 | 500: Internal Server Error | INTERNAL_SERVER_ERROR | "Internal server error" | A | F | 06/12/2025 | DFID013 |
| TC_SEARCHDOCTORSFILTER_005 | Access to MABS web application.<br>Internet connection must be stable.<br>Server running.<br>Database available.<br>Missing required fields in request. | (missing PageNumber and PageSize) | 400: Bad Request | VALIDATION_ERROR | "Invalid request data" | A | P | 06/12/2025 | - |

## Endpoint Information

### API Endpoints
1. `POST /api/v1/doctors/filter` - Search doctors with advanced filter (line 60-67)

### Error Handling
- ModelState validation cho request data
- Database query với multiple filters
- Try-catch blocks cho exception handling
- Specific error responses với status codes
- Logging được thực hiện qua ILogger

### Response Format
- Success: StatusCode 200 với message "Doctors filtered successfully"
- Validation Error: StatusCode 400 với message "Invalid filter criteria" hoặc "Invalid request data"
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
- ⚠️ Có 1 test case failed: TC_SEARCHDOCTORSFILTER_004 (Database connection issues)
- ⚠️ Thiếu test cases cho trường hợp Boundary

## Recommendations

### 1. Add Boundary Test Cases
- Test với multiple filter criteria combinations
- Test với large result sets
- Test với extreme pagination values

### 2. Cải thiện Test Coverage
- Fix failed test case TC_SEARCHDOCTORSFILTER_004
- Test với database timeout scenarios

## Return Code Reference

| Code | Description | Used In |
|------|-------------|---------|
| 200 | Success | TC_SEARCHDOCTORSFILTER_001, TC_SEARCHDOCTORSFILTER_003 |
| 400 | Bad Request | TC_SEARCHDOCTORSFILTER_002, TC_SEARCHDOCTORSFILTER_005 |
| 500 | Internal Server Error | TC_SEARCHDOCTORSFILTER_004 |

## Conclusion

Báo cáo test case cho function `SearchDoctorsFilter` đã được thiết kế đúng với implementation thực tế và best practices của REST API. Pass rate đạt 80% với 1 failed test case cần được fix.

### Key Points:
- ✅ Validation errors được handle đúng với StatusCode 400
- ✅ Empty result vẫn là success case (StatusCode 200) - đúng theo REST API standards
- ✅ Log messages phù hợp cho từng trường hợp
- ⚠️ Cần fix failed test case TC_SEARCHDOCTORSFILTER_004
