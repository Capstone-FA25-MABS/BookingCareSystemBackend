# GetProcessableRefundHistoriesByUser API Documentation

## T?ng quan

API endpoint m?i này ???c t?o ?? l?y danh sách refund histories c?a user v?i ch? các status **PENDING** và **COMPLETED**. ?ây là nh?ng refund histories "có th? x? lý" ho?c ?ã ???c x? lý xong.

## Business Logic

### Lý do t?o API này:
- **Status WAITING**: User ch?a có bank account, ch?a th? x? lý
- **Status PENDING**: ?ang ch? staff x? lý, có th? theo dõi
- **Status COMPLETED**: ?ã hoàn thành, có th? xem l?ch s?

### So sánh v?i API hi?n t?i:
- **`GET /user/{userId}`**: Tr? v? **T?T C?** refund histories (bao g?m WAITING)
- **`GET /user/{userId}/processable`**: Ch? tr? v? **PENDING + COMPLETED** (lo?i b? WAITING)

## API Endpoint

### GET `/api/v1.0/refundhistories/user/{userId}/processable`

**Description**: L?y danh sách refund histories c?a user ch? v?i status PENDING và COMPLETED

**Parameters:**
- `userId` (required): GUID - ID c?a user

## Request Examples

### 1. Basic Request
```http
GET /api/v1.0/refundhistories/user/550e8400-e29b-41d4-a716-446655440001/processable
```

### 2. Invalid User ID
```http
GET /api/v1.0/refundhistories/user/00000000-0000-0000-0000-000000000000/processable
```

## Response Examples

### 1. Thành công - Có refund histories

```json
{
    "success": true,
    "message": "L?y 3 refund histories ?ang x? lý/hoàn thành thành công",
    "data": {
        "refundHistories": [
            {
                "id": "660e8400-e29b-41d4-a716-446655440001",
                "userId": "550e8400-e29b-41d4-a716-446655440001",
                "paymentId": "770e8400-e29b-41d4-a716-446655440001",
                "bankAccountId": "880e8400-e29b-41d4-a716-446655440001",
                "status": "COMPLETED",
                "statusName": "Hoàn thành",
                "refundAmount": 500000,
                "refundReason": "H?y cu?c h?n do bác s? b?n",
                "transferDate": "2024-01-07T10:30:00Z",
                "staffNotes": "?ã chuy?n kho?n thành công qua VCB",
                "processedByStaffId": "990e8400-e29b-41d4-a716-446655440001",
                "createdAt": "2024-01-05T08:00:00Z",
                "updatedAt": "2024-01-07T10:30:00Z",
                "daysFromCreated": 2,
                "canProcess": false,
                "canUpdateBankAccount": false,
                "payment": {
                    "id": "770e8400-e29b-41d4-a716-446655440001",
                    "amount": 500000,
                    "status": "COMPLETED",
                    "paymentMethod": {
                        "name": "Chuy?n kho?n ngân hàng",
                        "code": "BANK_TRANSFER"
                    }
                },
                "bankAccount": {
                    "id": "880e8400-e29b-41d4-a716-446655440001",
                    "bankName": "Vietcombank",
                    "bankCode": "VCB",
                    "accountNumber": "******7890",
                    "accountName": "NGUYEN VAN A"
                }
            },
            {
                "id": "661e8400-e29b-41d4-a716-446655440002",
                "userId": "550e8400-e29b-41d4-a716-446655440001",
                "paymentId": "771e8400-e29b-41d4-a716-446655440002",
                "bankAccountId": "881e8400-e29b-41d4-a716-446655440002",
                "status": "PENDING",
                "statusName": "?ang x? lý",
                "refundAmount": 300000,
                "refundReason": "??i l?ch khám",
                "transferDate": null,
                "staffNotes": null,
                "processedByStaffId": null,
                "createdAt": "2024-01-06T14:00:00Z",
                "updatedAt": "2024-01-06T14:00:00Z",
                "daysFromCreated": 1,
                "canProcess": true,
                "canUpdateBankAccount": false,
                "payment": {
                    "id": "771e8400-e29b-41d4-a716-446655440002",
                    "amount": 300000,
                    "status": "COMPLETED"
                },
                "bankAccount": {
                    "id": "881e8400-e29b-41d4-a716-446655440002",
                    "bankName": "Techcombank",
                    "bankCode": "TCB",
                    "accountNumber": "******1234",
                    "accountName": "NGUYEN VAN A"
                }
            }
        ],
        "count": 2
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 2. Thành công - Không có refund histories

```json
{
    "success": true,
    "message": "Không tìm th?y refund history nào ?ang x? lý ho?c ?ã hoàn thành",
    "data": {
        "refundHistories": [],
        "count": 0
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 3. L?i - User ID không h?p l?

```json
{
    "success": false,
    "message": "User ID không h?p l?",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 4. L?i server

```json
{
    "success": false,
    "message": "Có l?i x?y ra khi l?y danh sách refund histories có th? x? lý",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## Database Query

### SQL Query ???c th?c hi?n:
```sql
SELECT r.*, p.*, pm.*, ba.*
FROM refund_histories r
LEFT JOIN payments p ON r.payment_id = p.id
LEFT JOIN payment_methods pm ON p.payment_method_id = pm.id  
LEFT JOIN bank_accounts ba ON r.bank_account_id = ba.id
WHERE r.user_id = @userId 
  AND (r.status = 'PENDING' OR r.status = 'COMPLETED')
ORDER BY r.created_at DESC
```

### Performance Notes:
- **Index s? d?ng**: `(user_id, status)` composite index
- **Include relationships**: Payment, PaymentMethod, BankAccount
- **Optimal performance** cho user-specific queries

## Frontend Integration

### JavaScript Example

```javascript
// L?y refund histories có th? x? lý c?a user
async function getProcessableRefunds(userId) {
    try {
        const response = await fetch(`/api/v1.0/refundhistories/user/${userId}/processable`);
        const result = await response.json();
        
        if (result.success) {
            const { refundHistories, count } = result.data;
            
            console.log(`Found ${count} processable refunds`);
            
            // X? lý hi?n th?
            displayProcessableRefunds(refundHistories);
            
            return refundHistories;
        } else {
            console.error('Error:', result.message);
            return [];
        }
    } catch (error) {
        console.error('Network error:', error);
        return [];
    }
}

// Hi?n th? danh sách refund có th? x? lý
function displayProcessableRefunds(refunds) {
    const container = document.getElementById('processable-refunds');
    
    if (refunds.length === 0) {
        container.innerHTML = '<p>Không có yêu c?u hoàn ti?n nào ?ang x? lý.</p>';
        return;
    }
    
    container.innerHTML = refunds.map(refund => `
        <div class="refund-item ${refund.status.toLowerCase()}">
            <div class="refund-header">
                <span class="amount">${formatCurrency(refund.refundAmount)}</span>
                <span class="status status-${refund.status.toLowerCase()}">${refund.statusName}</span>
            </div>
            <div class="refund-details">
                <p><strong>Lý do:</strong> ${refund.refundReason || 'Không có'}</p>
                <p><strong>Ngày t?o:</strong> ${formatDate(refund.createdAt)}</p>
                ${refund.transferDate ? `<p><strong>Ngày chuy?n:</strong> ${formatDate(refund.transferDate)}</p>` : ''}
                ${refund.bankAccount ? `
                    <p><strong>Tài kho?n:</strong> ${refund.bankAccount.bankName} - ${refund.bankAccount.accountNumber}</p>
                ` : ''}
            </div>
            ${refund.status === 'PENDING' ? `
                <div class="refund-actions">
                    <span class="pending-note">?ang ch? x? lý...</span>
                </div>
            ` : ''}
        </div>
    `).join('');
}

// Helper functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('vi-VN');
}

// Usage
getProcessableRefunds('550e8400-e29b-41d4-a716-446655440001');
```

### React Hook Example

```jsx
import { useState, useEffect } from 'react';

const useProcessableRefunds = (userId) => {
    const [refunds, setRefunds] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const fetchProcessableRefunds = async () => {
        if (!userId) return;
        
        setLoading(true);
        setError(null);

        try {
            const response = await fetch(`/api/v1.0/refundhistories/user/${userId}/processable`);
            const result = await response.json();

            if (result.success) {
                setRefunds(result.data.refundHistories);
            } else {
                setError(result.message);
            }
        } catch (err) {
            setError('Có l?i x?y ra khi t?i d? li?u');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchProcessableRefunds();
    }, [userId]);

    return { 
        refunds, 
        loading, 
        error, 
        refetch: fetchProcessableRefunds 
    };
};

// Component s? d?ng
const ProcessableRefundsPage = ({ userId }) => {
    const { refunds, loading, error } = useProcessableRefunds(userId);

    if (loading) return <div>?ang t?i...</div>;
    if (error) return <div>L?i: {error}</div>;

    return (
        <div className="processable-refunds">
            <h2>Yêu c?u hoàn ti?n ({refunds.length})</h2>
            
            {refunds.length === 0 ? (
                <p>Không có yêu c?u hoàn ti?n nào ?ang x? lý.</p>
            ) : (
                <div className="refunds-list">
                    {refunds.map(refund => (
                        <RefundCard key={refund.id} refund={refund} />
                    ))}
                </div>
            )}
        </div>
    );
};

const RefundCard = ({ refund }) => (
    <div className={`refund-card status-${refund.status.toLowerCase()}`}>
        <div className="card-header">
            <h3>{formatCurrency(refund.refundAmount)}</h3>
            <span className={`status-badge ${refund.status.toLowerCase()}`}>
                {refund.statusName}
            </span>
        </div>
        
        <div className="card-body">
            <p><strong>Lý do:</strong> {refund.refundReason || 'Không có'}</p>
            <p><strong>Ngày t?o:</strong> {formatDate(refund.createdAt)}</p>
            
            {refund.status === 'COMPLETED' && refund.transferDate && (
                <p><strong>Ngày hoàn ti?n:</strong> {formatDate(refund.transferDate)}</p>
            )}
            
            {refund.bankAccount && (
                <div className="bank-info">
                    <p><strong>Tài kho?n nh?n:</strong></p>
                    <p>{refund.bankAccount.bankName} - {refund.bankAccount.accountNumber}</p>
                </div>
            )}
        </div>
        
        {refund.status === 'PENDING' && (
            <div className="card-footer">
                <span className="pending-indicator">? ?ang ch? x? lý</span>
            </div>
        )}
    </div>
);
```

## Use Cases

### 1. **User Dashboard**
```javascript
// Hi?n th? overview cho user
const refunds = await getProcessableRefunds(userId);
const pendingCount = refunds.filter(r => r.status === 'PENDING').length;
const completedCount = refunds.filter(r => r.status === 'COMPLETED').length;

updateDashboard({
    pendingRefunds: pendingCount,
    completedRefunds: completedCount,
    totalAmount: refunds.reduce((sum, r) => sum + r.refundAmount, 0)
});
```

### 2. **Notification System**
```javascript
// Check có refund m?i ???c complete không
const completedRefunds = refunds.filter(r => 
    r.status === 'COMPLETED' && 
    isToday(r.transferDate)
);

if (completedRefunds.length > 0) {
    showNotification(`B?n ?ã nh?n ???c ${completedRefunds.length} kho?n hoàn ti?n hôm nay!`);
}
```

### 3. **Financial Summary**
```javascript
// Tính t?ng ti?n ?ã/?ang hoàn
const summary = refunds.reduce((acc, refund) => {
    if (refund.status === 'COMPLETED') {
        acc.completed += refund.refundAmount;
    } else if (refund.status === 'PENDING') {
        acc.pending += refund.refundAmount;
    }
    return acc;
}, { completed: 0, pending: 0 });
```

## Comparison v?i API hi?n t?i

### API c?: `GET /user/{userId}`
```json
{
    "refundHistories": [
        { "status": "WAITING" },    // ? Lo?i này s? không có trong API m?i
        { "status": "PENDING" },    // ? Có trong API m?i
        { "status": "COMPLETED" }   // ? Có trong API m?i
    ],
    "count": 3
}
```

### API m?i: `GET /user/{userId}/processable`
```json
{
    "refundHistories": [
        { "status": "PENDING" },    // ? Ch? có 2 lo?i này
        { "status": "COMPLETED" }
    ],
    "count": 2                      // ? Count th?p h?n vì l?c b? WAITING
}
```

## Security & Performance

### Security
- **Same authorization** nh? API g?c
- **User isolation**: Ch? l?y refunds c?a user ?ó
- **No sensitive data exposure**

### Performance  
- **Optimized query** v?i status filter
- **Same include strategy** ?? avoid N+1
- **Index support** cho composite query (user_id + status)

### Caching Considerations
```javascript
// Cache strategy
const cacheKey = `processable-refunds:${userId}`;
const cachedData = await redis.get(cacheKey);

if (cachedData) {
    return JSON.parse(cachedData);
}

const freshData = await getProcessableRefunds(userId);
await redis.setex(cacheKey, 300, JSON.stringify(freshData)); // Cache 5 minutes

return freshData;
```

API m?i này cung c?p view focused h?n cho user v? nh?ng refund "có ý ngh?a" và có th? theo dõi ???c, lo?i b? nh?ng refund ?ang WAITING mà user ch?a th? làm gì! ??