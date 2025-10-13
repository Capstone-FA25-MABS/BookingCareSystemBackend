# RefundHistory System - Payment Service

## ?? T?ng quan

RefundHistory System là m?t h? th?ng hoàn ch?nh ?? qu?n lý l?ch s? refund c?a b?nh nhân trong BookingCare Payment Service. H? th?ng h? tr? workflow t? ??ng t? WAITING ? PENDING ? COMPLETED v?i business logic ph?c t?p.

## ??? Ki?n trúc ?ã implement

### 1. **Database Schema**
- **RefundHistoryEntity**: Entity chính v?i quan h? 1:1 v?i PaymentEntity và optional v?i BankAccountEntity
- **RefundStatus Enum**: WAITING, PENDING, COMPLETED
- **Indexes**: Optimized cho performance v?i composite indexes
- **Check Constraints**: Ensure data integrity t?i database level

### 2. **API Layer** 
- **RefundHistoriesController**: Full CRUD + business operations
- **11 endpoints** bao g?m search, statistics, batch processing
- **API Versioning**: V1.0 v?i backward compatibility
- **Comprehensive validation** v?i FluentValidation

### 3. **Business Logic Layer**
- **RefundHistoryService**: Business logic v?i ExecuteWithErrorHandling
- **Auto status determination**: T? ??ng xác ??nh WAITING/PENDING d?a trên bank account
- **Status transition validation**: Strict rules cho state changes
- **Batch processing**: ProcessWaitingRefunds cho automation

### 4. **Data Access Layer**
- **RefundHistoryRepository**: Optimized queries v?i Include statements
- **Paged results**: Performance-oriented pagination
- **Complex filtering**: User, status, date range filters

### 5. **Background Processing**
- **RefundHistoryProcessingService**: Auto-process WAITING ? PENDING m?i 5 phút
- **Error handling**: Robust error handling không crash service
- **Logging**: Comprehensive logging cho monitoring

## ?? Business Logic Flow

```
CREATE REFUND
     ?
Has Bank Account? ??Yes??? PENDING ??Staff Process??? COMPLETED
     ?                        ?                          ?
    No                   Update Bank                Transfer Done
     ?                   Account Info                    ?
  WAITING ???????????????????????                       ?
     ?                                                  ?
Background Service                                      ?
(Auto-detect bank account) ??????????????????????????????
```

## ?? Key Features Implemented

### ? **Automatic Status Management**
- Auto-determine initial status based on user's bank account
- Background service t? ??ng upgrade WAITING ? PENDING
- Business rules validation cho status transitions

### ? **Comprehensive API**
```csharp
// Core CRUD
GET    /api/v1.0/refundhistories/{id}
POST   /api/v1.0/refundhistories
PUT    /api/v1.0/refundhistories/{id}/status
DELETE /api/v1.0/refundhistories/{id}

// Business Operations
GET    /api/v1.0/refundhistories/user/{userId}
GET    /api/v1.0/refundhistories/payment/{paymentId}
POST   /api/v1.0/refundhistories/search
POST   /api/v1.0/refundhistories/process-waiting
GET    /api/v1.0/refundhistories/statistics
```

### ? **Validation & Security**
- FluentValidation cho input validation
- Business rules validation
- Check constraints t?i database level
- Under-posting protection v?i JsonRequired

### ? **Frontend-Friendly Response**
```json
{
  "refundHistories": [...],
  "count": 5  // Direct access cho frontend
}
```

### ? **Performance Optimization**
- Strategic indexes cho common queries
- Paged results v?i filtering
- Efficient eager loading v?i Include()
- Composite indexes cho multi-column queries

## ?? Files Created

### **Core Components**
```
??? Enums/
?   ??? RefundStatus.cs
??? Models/
?   ??? Entities/RefundHistoryEntity.cs
?   ??? DTOs/Requests/RefundHistoryRequests.cs
?   ??? DTOs/Responses/RefundHistoryResponse.cs
?   ??? Events/RefundHistoryEvents.cs
??? Repositories/
?   ??? Interfaces/IRefundHistoryRepository.cs
?   ??? Implementations/RefundHistoryRepository.cs
??? Services/
?   ??? Interfaces/IRefundHistoryService.cs
?   ??? Implementations/RefundHistoryService.cs
?   ??? BackgroundServices/RefundHistoryProcessingService.cs
??? Controllers/
?   ??? RefundHistoriesController.cs
??? Validators/
?   ??? RefundHistoryValidators.cs
??? Extensions/
?   ??? RefundHistoryExtensions.cs
??? Migrations/
    ??? 20251007052723_AddRefundHistoryTable.cs
```

### **Documentation**
```
??? API_Tests_RefundHistory.md     # Complete API documentation
??? README_RefundHistory.md        # This summary file
```

## ?? Business Rules Implemented

### **1. Payment Validation**
- Payment must have status = COMPLETED
- Payment cannot already have a refund history (1:1 relationship)
- Refund amount ? Payment amount

### **2. Status Workflow**
```
WAITING  ??  PENDING  ?  COMPLETED
```
- **WAITING**: Ch?a có bank account active
- **PENDING**: Có bank account, ??i staff x? lý  
- **COMPLETED**: ?ã chuy?n ti?n thành công

### **3. Bank Account Integration**
- Auto-detect user's default bank account
- Validate bank account ownership và active status
- Support manual bank account assignment

### **4. Security & Permissions**
- Users ch? xem ???c refund histories c?a mình
- Staff có th? process và update status
- Comprehensive audit trail v?i staff notes

## ?? Usage Examples

### **Create Refund (Auto Status)**
```csharp
// User có bank account ? Status = PENDING
// User ch?a có bank account ? Status = WAITING
var request = new CreateRefundHistoryRequest 
{
    PaymentId = paymentId,
    UserId = userId,
    RefundAmount = 500000,
    RefundReason = "H?y cu?c h?n"
};
```

### **Staff Processing**
```csharp
// Update to COMPLETED
var request = new UpdateRefundHistoryStatusRequest
{
    Status = RefundStatus.COMPLETED,
    StaffNotes = "?ã chuy?n kho?n thành công",
    ProcessedByStaffId = staffId
    // TransferDate auto-set
};
```

### **Background Processing**
```csharp
// Auto-runs every 5 minutes
// Checks WAITING refunds và upgrade to PENDING
// Khi user thêm bank account
var processedCount = await _refundHistoryService.ProcessWaitingRefundsAsync();
```

## ?? Performance Considerations

### **Database Optimization**
- **Primary Index**: `id` (PK)
- **Unique Index**: `payment_id` (1:1 relationship)
- **Query Indexes**: `user_id`, `status`
- **Composite Index**: `(user_id, status)` for user-specific queries

### **Query Optimization**
- Eager loading v?i `Include()` cho related entities
- Paged results ?? handle large datasets
- Efficient filtering v?i compiled queries

### **Background Service**
- Non-blocking execution
- Graceful error handling
- Configurable interval (default: 5 minutes)

## ?? K?t qu?

H? th?ng RefundHistory ?ã ???c implement hoàn ch?nh v?i:

- ? **Complete CRUD operations**
- ? **Advanced business logic**
- ? **Automatic background processing** 
- ? **Comprehensive validation**
- ? **Performance optimization**
- ? **Frontend-friendly APIs**
- ? **Full documentation**
- ? **Production-ready code**

System s?n sàng ?? handle refund workflow ph?c t?p v?i scalability và reliability cao, ?áp ?ng ??y ?? yêu c?u business c?a BookingCare platform.

## ?? Next Steps (Optional)

N?u mu?n extend thêm:
1. **Event Bus Integration**: Publish events cho inter-service communication
2. **Email Notifications**: Notify users v? refund status changes  
3. **Advanced Analytics**: Refund trends và reporting
4. **Mobile API**: Optimized endpoints cho mobile app
5. **Webhook Support**: External system integrations