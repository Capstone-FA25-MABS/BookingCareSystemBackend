# RefundHistory API Test Collection

## Base URL
```
http://localhost:6011/api/v1.0/refundhistories
```

## 1. Create Refund History
```http
POST /api/v1.0/refundhistories
Content-Type: application/json

{
    "paymentId": "550e8400-e29b-41d4-a716-446655440001",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
    "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
    "refundAmount": 500000,
    "refundReason": "H?y cu?c h?n do lý do cá nhân"
}
```

### Response Examples for Create Refund History

#### Success Response
```json
{
    "success": true,
    "message": "T?o refund history thành công",
    "data": {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
        "bankAccount": {
            "id": "550e8400-e29b-41d4-a716-446655440002",
            "userId": "550e8400-e29b-41d4-a716-446655440000",
            "bankCode": "VCB",
            "bankName": "Vietcombank",
            "accountNumber": "******7890",
            "fullAccountNumber": "1234567890",
            "accountName": "NGUYEN VAN A",
            "isDefault": true,
            "isActive": true
        },
        "userId": "550e8400-e29b-41d4-a716-446655440000",
        "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
        "status": "PENDING",
        "statusName": "?ang x? lý",
        "transferDate": null,
        "paymentId": "550e8400-e29b-41d4-a716-446655440001",
        "payment": {
            "id": "550e8400-e29b-41d4-a716-446655440001",
            "amount": 500000,
            "transactionType": "APPOINTMENT",
            "status": "COMPLETED",
            "paymentMethodName": "BANK_TRANSFER"
        },
        "refundAmount": 500000,
        "refundReason": "H?y cu?c h?n do lý do cá nhân",
        "staffNotes": null,
        "processedByStaffId": null,
        "createdAt": "2024-01-07T10:00:00Z",
        "updatedAt": "2024-01-07T10:00:00Z",
        "daysFromCreated": 0,
        "canProcess": true,
        "canUpdateBankAccount": false
    },
    "timestamp": "2024-01-07T10:00:00Z"
}
```

#### Auto Status: WAITING (User ch?a có bank account)
```json
{
    "success": true,
    "message": "T?o refund history thành công",
    "data": {
        "id": "660e8400-e29b-41d4-a716-446655440002",
        "bankAccountId": null,
        "bankAccount": null,
        "userId": "550e8400-e29b-41d4-a716-446655440000",
        "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
        "status": "WAITING",
        "statusName": "?ang ch?",
        "transferDate": null,
        "paymentId": "550e8400-e29b-41d4-a716-446655440001",
        "refundAmount": 500000,
        "refundReason": "H?y cu?c h?n do lý do cá nhân",
        "staffNotes": null,
        "processedByStaffId": null,
        "createdAt": "2024-01-07T10:00:00Z",
        "updatedAt": "2024-01-07T10:00:00Z",
        "daysFromCreated": 0,
        "canProcess": false,
        "canUpdateBankAccount": true
    },
    "timestamp": "2024-01-07T10:00:00Z"
}
```

## 2. Get Refund History by ID
```http
GET /api/v1.0/refundhistories/660e8400-e29b-41d4-a716-446655440001
```

## 3. Get Refund History by Payment ID
```http
GET /api/v1.0/refundhistories/payment/550e8400-e29b-41d4-a716-446655440001
```

## 4. Get Refund Histories by User
```http
GET /api/v1.0/refundhistories/user/550e8400-e29b-41d4-a716-446655440000
```

## 5. Get Refund Histories by Hospital
```http
GET /api/v1.0/refundhistories/hospital/660e8400-e29b-41d4-a716-446655440001
```

### Response Examples for Get by Hospital

#### Multiple Refund Histories Response
```json
{
    "success": true,
    "message": "L?y 3 refund histories cho hospital thành công",
    "data": {
        "refundHistories": [
            {
                "id": "660e8400-e29b-41d4-a716-446655440001",
                "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
                "status": "COMPLETED",
                "statusName": "Hoàn thành",
                "transferDate": "2024-01-07T14:30:00Z",
                "paymentId": "550e8400-e29b-41d4-a716-446655440001",
                "refundAmount": 500000,
                "refundReason": "H?y cu?c h?n do lý do cá nhân",
                "staffNotes": "?ã chuy?n kho?n thành công",
                "processedByStaffId": "770e8400-e29b-41d4-a716-446655440001",
                "createdAt": "2024-01-07T10:00:00Z",
                "updatedAt": "2024-01-07T14:30:00Z",
                "daysFromCreated": 0,
                "canProcess": false,
                "canUpdateBankAccount": false
            },
            {
                "id": "660e8400-e29b-41d4-a716-446655440002",
                "bankAccountId": null,
                "userId": "550e8400-e29b-41d4-a716-446655440001",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
                "status": "WAITING",
                "statusName": "?ang ch?",
                "transferDate": null,
                "paymentId": "550e8400-e29b-41d4-a716-446655440003",
                "refundAmount": 300000,
                "refundReason": "H?y cu?c h?n do bác s? không có m?t",
                "staffNotes": null,
                "processedByStaffId": null,
                "createdAt": "2024-01-07T11:00:00Z",
                "updatedAt": "2024-01-07T11:00:00Z",
                "daysFromCreated": 0,
                "canProcess": false,
                "canUpdateBankAccount": true
            },
            {
                "id": "660e8400-e29b-41d4-a716-446655440003",
                "bankAccountId": "550e8400-e29b-41d4-a716-446655440003",
                "userId": "550e8400-e29b-41d4-a716-446655440002",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
                "status": "PENDING",
                "statusName": "?ang x? lý",
                "transferDate": null,
                "paymentId": "550e8400-e29b-41d4-a716-446655440005",
                "refundAmount": 750000,
                "refundReason": "Thay ??i l?ch h?n",
                "staffNotes": null,
                "processedByStaffId": null,
                "createdAt": "2024-01-07T09:00:00Z",
                "updatedAt": "2024-01-07T09:00:00Z",
                "daysFromCreated": 0,
                "canProcess": true,
                "canUpdateBankAccount": false
            }
        ],
        "count": 3
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Response Examples for Get by User

#### Multiple Refund Histories Response
```json
{
    "success": true,
    "message": "L?y 2 refund histories thành công",
    "data": {
        "refundHistories": [
            {
                "id": "660e8400-e29b-41d4-a716-446655440001",
                "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
                "status": "COMPLETED",
                "statusName": "Hoàn thành",
                "transferDate": "2024-01-07T14:30:00Z",
                "paymentId": "550e8400-e29b-41d4-a716-446655440001",
                "refundAmount": 500000,
                "refundReason": "H?y cu?c h?n do lý do cá nhân",
                "staffNotes": "?ã chuy?n kho?n thành công",
                "processedByStaffId": "770e8400-e29b-41d4-a716-446655440001",
                "createdAt": "2024-01-07T10:00:00Z",
                "updatedAt": "2024-01-07T14:30:00Z",
                "daysFromCreated": 0,
                "canProcess": false,
                "canUpdateBankAccount": false
            },
            {
                "id": "660e8400-e29b-41d4-a716-446655440002",
                "bankAccountId": null,
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440002",
                "status": "WAITING",
                "statusName": "?ang ch?",
                "transferDate": null,
                "paymentId": "550e8400-e29b-41d4-a716-446655440003",
                "refundAmount": 300000,
                "refundReason": "H?y cu?c h?n do bác s? không có m?t",
                "staffNotes": null,
                "processedByStaffId": null,
                "createdAt": "2024-01-07T11:00:00Z",
                "updatedAt": "2024-01-07T11:00:00Z",
                "daysFromCreated": 0,
                "canProcess": false,
                "canUpdateBankAccount": true
            }
        ],
        "count": 2
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## 6. Get Refund Histories by Status
```http
GET /api/v1.0/refundhistories/status/PENDING
GET /api/v1.0/refundhistories/status/WAITING
GET /api/v1.0/refundhistories/status/COMPLETED
```

## 7. Get Paged Refund Histories (Search)
```http
POST /api/v1.0/refundhistories/search
Content-Type: application/json

{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
    "status": "PENDING",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z",
    "page": 1,
    "pageSize": 20
}
```

### Search All Refund Histories (Staff View)
```http
POST /api/v1.0/refundhistories/search
Content-Type: application/json

{
    "status": "PENDING",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z",
    "page": 1,
    "pageSize": 50
}
```

### Search by Hospital (Hospital Staff View)
```http
POST /api/v1.0/refundhistories/search
Content-Type: application/json

{
    "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
    "status": "PENDING",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z",
    "page": 1,
    "pageSize": 50
}
```

## 8. Update Refund History Status
```http
PUT /api/v1.0/refundhistories/660e8400-e29b-41d4-a716-446655440001/status
Content-Type: application/json

{
    "status": "COMPLETED",
    "transferDate": "2024-01-07T14:30:00Z",
    "staffNotes": "?ã chuy?n kho?n thành công qua VCB",
    "processedByStaffId": "770e8400-e29b-41d4-a716-446655440001"
}
```

### Update from WAITING to PENDING (Add Bank Account)
```http
PUT /api/v1.0/refundhistories/660e8400-e29b-41d4-a716-446655440002/status
Content-Type: application/json

{
    "status": "PENDING",
    "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
    "staffNotes": "User ?ã c?p nh?t bank account"
}
```

### Update from PENDING to WAITING (Remove Bank Account)
```http
PUT /api/v1.0/refundhistories/660e8400-e29b-41d4-a716-446655440001/status
Content-Type: application/json

{
    "status": "WAITING",
    "staffNotes": "Bank account không còn active, chuy?n v? WAITING"
}
```

## 9. Delete Refund History (Only WAITING status)
```http
DELETE /api/v1.0/refundhistories/660e8400-e29b-41d4-a716-446655440001
```

## 10. Process Waiting Refunds (Batch Operation)
```http
POST /api/v1.0/refundhistories/process-waiting
```

### Response for Process Waiting
```json
{
    "success": true,
    "message": "?ã x? lý 5 refund histories thành công",
    "data": {
        "processedCount": 5
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## 11. Get Refund Statistics
```http
GET /api/v1.0/refundhistories/statistics
```

### Response for Statistics
```json
{
    "success": true,
    "message": "L?y th?ng kê refund thành công",
    "data": {
        "WAITING": 12,
        "PENDING": 8,
        "COMPLETED": 45
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## 12. Check if Payment Can Refund
```http
GET /api/v1.0/refundhistories/payment/550e8400-e29b-41d4-a716-446655440001/can-refund
```

### Response for Can Refund Check
```json
{
    "success": true,
    "message": "Payment có th? refund",
    "data": {
        "paymentId": "550e8400-e29b-41d4-a716-446655440001",
        "canRefund": true
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Response for Cannot Refund
```json
{
    "success": true,
    "message": "Payment không th? refund",
    "data": {
        "paymentId": "550e8400-e29b-41d4-a716-446655440001",
        "canRefund": false
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## 13. Get Processable Refund Histories by User
```http
GET /api/v1.0/refundhistories/user/550e8400-e29b-41d4-a716-446655440000/processable
```

### Response for Processable Refunds
```json
{
    "success": true,
    "message": "L?y 2 processable/completed refund histories thành công",
    "data": {
        "refundHistories": [
            {
                "id": "660e8400-e29b-41d4-a716-446655440001",
                "bankAccountId": "550e8400-e29b-41d4-a716-446655440002",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440001",
                "status": "COMPLETED",
                "statusName": "Hoàn thành",
                "transferDate": "2024-01-07T14:30:00Z",
                "paymentId": "550e8400-e29b-41d4-a716-446655440001",
                "refundAmount": 500000,
                "refundReason": "H?y cu?c h?n do lý do cá nhân",
                "staffNotes": "?ã chuy?n kho?n thành công",
                "processedByStaffId": "770e8400-e29b-41d4-a716-446655440001",
                "createdAt": "2024-01-07T10:00:00Z",
                "updatedAt": "2024-01-07T14:30:00Z",
                "daysFromCreated": 0,
                "canProcess": false,
                "canUpdateBankAccount": false
            },
            {
                "id": "660e8400-e29b-41d4-a716-446655440003",
                "bankAccountId": "550e8400-e29b-41d4-a716-446655440003",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "hospitalId": "660e8400-e29b-41d4-a716-446655440002",
                "status": "PENDING",
                "statusName": "?ang x? lý",
                "transferDate": null,
                "paymentId": "550e8400-e29b-41d4-a716-446655440005",
                "refundAmount": 300000,
                "refundReason": "Thay ??i l?ch h?n",
                "staffNotes": null,
                "processedByStaffId": null,
                "createdAt": "2024-01-07T12:00:00Z",
                "updatedAt": "2024-01-07T12:00:00Z",
                "daysFromCreated": 0,
                "canProcess": true,
                "canUpdateBankAccount": false
            }
        ],
        "count": 2
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## Error Responses

### Validation Error
```json
{
    "success": false,
    "message": "D? li?u không h?p l?",
    "errors": [
        "Payment ID không ???c ?? tr?ng",
        "Hospital ID không ???c ?? tr?ng",
        "S? ti?n refund ph?i l?n h?n 0"
    ],
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Business Logic Error
```json
{
    "success": false,
    "message": "Ch? có th? refund payment có status COMPLETED. Payment hi?n t?i: PENDING",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Hospital ID Mismatch Error
```json
{
    "success": false,
    "message": "Hospital ID mismatch. Payment belongs to hospital 660e8400-e29b-41d4-a716-446655440001, but refund is for hospital 660e8400-e29b-41d4-a716-446655440002",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Conflict Error (Payment already has refund)
```json
{
    "success": false,
    "message": "Payment 550e8400-e29b-41d4-a716-446655440001 ?ã có refund history",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Not Found Error
```json
{
    "success": false,
    "message": "Refund history v?i ID 660e8400-e29b-41d4-a716-446655440001 không tìm th?y",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### Invalid Operation Error
```json
{
    "success": false,
    "message": "Ch? có th? xóa refund history có status WAITING. Status hi?n t?i: PENDING",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## Business Rules & Workflow

### RefundStatus Flow
```
WAITING ? PENDING ? COMPLETED
    ?         ?
    ???????????
```

### Status Descriptions
- **WAITING**: User ch?a có bank account ho?c không có bank account nào active
- **PENDING**: ?ang ??i staff refund ti?n cho user (?ã có bank account)
- **COMPLETED**: Refund ti?n thành công

### Automatic Status Determination
1. **Khi t?o refund history:**
   - N?u có `bankAccountId` valid và active ? `PENDING`
   - N?u user có default bank account active ? `PENDING`
   - N?u user ch?a có bank account nào ? `WAITING`

2. **Process Waiting Refunds:**
   - T? ??ng chuy?n t? `WAITING` ? `PENDING` khi user có bank account active

### Validation Rules
1. **Payment Requirements:**
   - Payment ph?i có status = `COMPLETED`
   - Payment ch?a có refund history (1:1 relationship)
   - Refund amount ? Payment amount

2. **Hospital Requirements:**
   - `HospitalId` là required field (NOT NULL)
   - N?u payment có `HospitalId`, ph?i match v?i refund's `HospitalId`
   - Cho phép hospital x? lý refund cho appointment payments không có `HospitalId`

3. **Status Transition Rules:**
   - `WAITING` ? `PENDING`: Ph?i có bank account valid
   - `WAITING` ? `COMPLETED`: Có th? skip PENDING (rare case)
   - `PENDING` ? `COMPLETED`: T? ??ng set transfer date
   - `PENDING` ? `WAITING`: Bank account b? inactive
   - `COMPLETED` ? others: Không cho phép

4. **Delete Rules:**
   - Ch? có th? xóa refund history có status = `WAITING`

### Security & Permissions
- **Patient**: Ch? xem ???c refund histories c?a mình
- **Hospital Staff**: Có th? xem refund histories c?a hospital mình
- **System Admin**: Full access to all refunds
- **Staff**: Có th? process và update status

### Performance Considerations
- Indexes ???c t?o cho: `user_id`, `hospital_id`, `status`, `payment_id` (unique)
- Composite index cho `(user_id, status)` ?? optimize queries
- Composite index cho `(hospital_id, status)` ?? optimize hospital queries
- Check constraints ensure data integrity

### Frontend Integration Tips
1. **Use the count field** t? get by user/hospital endpoints
2. **Check canProcess và canUpdateBankAccount** flags
3. **Display appropriate actions** based on status
4. **Auto-refresh** status cho pending refunds
5. **Handle different response formats** for empty/single/multiple results
6. **Hospital Dashboard**: Use hospital endpoint ?? show refunds c?n x? lý
7. **Patient Dashboard**: Use user endpoint ?? show refund history c?a patient

### API Endpoints Summary
```
# Core CRUD Operations
GET    /api/v1.0/refundhistories/{id}                                    # Get by ID
POST   /api/v1.0/refundhistories                                         # Create refund
PUT    /api/v1.0/refundhistories/{id}/status                            # Update status
DELETE /api/v1.0/refundhistories/{id}                                   # Delete (WAITING only)

# Query Operations
GET    /api/v1.0/refundhistories/payment/{paymentId}                    # Get by payment
GET    /api/v1.0/refundhistories/user/{userId}                          # Get by user
GET    /api/v1.0/refundhistories/hospital/{hospitalId}                  # Get by hospital (NEW)
GET    /api/v1.0/refundhistories/status/{status}                        # Get by status
GET    /api/v1.0/refundhistories/user/{userId}/processable              # Get processable by user
POST   /api/v1.0/refundhistories/search                                 # Advanced search with filters

# Business Operations
POST   /api/v1.0/refundhistories/process-waiting                        # Batch process waiting
GET    /api/v1.0/refundhistories/statistics                             # Get statistics
GET    /api/v1.0/refundhistories/payment/{paymentId}/can-refund         # Check if refundable
```

### Key Changes from Previous Version
1. **NEW REQUIRED FIELD**: `hospitalId` is now required when creating refund history
2. **NEW ENDPOINT**: `GET /api/v1.0/refundhistories/hospital/{hospitalId}` to get refunds by hospital
3. **ENHANCED SEARCH**: Search endpoint now supports `hospitalId` filter
4. **IMPROVED VALIDATION**: Hospital ID validation and mismatch error handling
5. **BETTER RESPONSES**: All responses now include `hospitalId` field
6. **HOSPITAL DASHBOARD SUPPORT**: Endpoints optimized for hospital staff to manage refunds