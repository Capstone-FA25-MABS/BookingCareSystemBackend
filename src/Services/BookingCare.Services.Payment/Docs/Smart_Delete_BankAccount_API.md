# Smart Delete Bank Account API Documentation

## T?ng quan

Smart Delete là m?t tính n?ng thông minh trong BankAccountsController giúp x? lý vi?c xóa bank account m?t cách an toàn b?ng cách ki?m tra xem bank account có ???c s? d?ng trong RefundHistories hay không.

## Business Logic

### Lu?ng x? lý Smart Delete:

```
Ki?m tra Bank Account t?n t?i
         ?
Ki?m tra có ph?i Default Account không
         ?
Ki?m tra có RefundHistories liên quan không
         ?
    ?????????????????????????????
    ?             ?             ?
Có RefundH.  Không có RefundH.  L?i
    ?             ?             ?
DEACTIVATE    DELETE HOÀN TOÀN  BadRequest
```

### Chi ti?t logic:

1. **Có RefundHistories** ? **DEACTIVATE**
   - Ch? set `IsActive = false`
   - B? `IsDefault = false` n?u là tài kho?n m?c ??nh
   - Tr? v? bank account ?ã ???c c?p nh?t
   - B?o toàn d? li?u quan tr?ng

2. **Không có RefundHistories** ? **DELETE HOÀN TOÀN**
   - Xóa h?n record kh?i database
   - Tr? v? thông báo thành công ??n gi?n

3. **L?i validation** ? **BadRequest**
   - Bank account không t?n t?i
   - ?ang là default account mà còn accounts khác

## API Endpoint

### DELETE `/api/v1.0/bankaccounts/{id}`

**Request:**
```http
DELETE /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
```

## Response Examples

### 1. Xóa hoàn toàn thành công (Không có RefundHistories)

```json
{
    "success": true,
    "message": "Bank account ?ã ???c xóa hoàn toàn thành công",
    "data": null,
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 2. Deactivate thành công (Có RefundHistories)

```json
{
    "success": true,
    "message": "Bank account ?ã ???c vô hi?u hóa do có liên k?t v?i l?ch s? refund. Không th? xóa hoàn toàn.",
    "data": {
        "message": "Bank account ?ã ???c vô hi?u hóa do có liên k?t v?i l?ch s? refund. Không th? xóa hoàn toàn.",
        "action": "deactivated",
        "bankAccount": {
            "id": "550e8400-e29b-41d4-a716-446655440001",
            "userId": "660e8400-e29b-41d4-a716-446655440001",
            "bankCode": "VCB",
            "bankName": "Vietcombank",
            "accountNumber": "******1234",
            "fullAccountNumber": "1234567890",
            "accountName": "NGUYEN VAN A",
            "isDefault": false,
            "isActive": false,
            "createdAt": "2024-01-01T10:00:00Z",
            "updatedAt": "2024-01-07T15:00:00Z"
        },
        "refundHistoriesCount": 0
    },
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 3. L?i validation (Default account có accounts khác)

```json
{
    "success": false,
    "message": "Không th? xóa tài kho?n m?c ??nh khi còn tài kho?n khác. Hãy ??t tài kho?n khác làm m?c ??nh tr??c.",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 4. Bank account không t?n t?i

```json
{
    "success": false,
    "message": "Bank account v?i ID 550e8400-e29b-41d4-a716-446655440001 không tìm th?y",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

### 5. ID không h?p l?

```json
{
    "success": false,
    "message": "ID bank account không h?p l?",
    "timestamp": "2024-01-07T15:00:00Z"
}
```

## Frontend Integration

### JavaScript Example

```javascript
// Function ?? x? lý smart delete
async function smartDeleteBankAccount(bankAccountId) {
    try {
        const response = await fetch(`/api/v1.0/bankaccounts/${bankAccountId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Ki?m tra lo?i action
            if (result.data && result.data.action === 'deactivated') {
                // Bank account ?ã ???c deactivate
                showNotification('warning', result.message);
                // C?p nh?t UI ?? hi?n th? bank account b? deactivate
                updateBankAccountInUI(result.data.bankAccount);
            } else {
                // Bank account ?ã ???c xóa hoàn toàn
                showNotification('success', result.message);
                // Xóa bank account kh?i UI
                removeBankAccountFromUI(bankAccountId);
            }
        } else {
            // X? lý l?i
            showNotification('error', result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('error', 'Có l?i x?y ra khi xóa bank account');
    }
}

// Function ?? hi?n th? thông báo phù h?p
function showNotification(type, message) {
    // Implement notification logic
    console.log(`${type.toUpperCase()}: ${message}`);
}

// Function ?? c?p nh?t UI khi bank account b? deactivate
function updateBankAccountInUI(bankAccount) {
    const element = document.getElementById(`bank-account-${bankAccount.id}`);
    if (element) {
        // Thêm class ?? hi?n th? tr?ng thái inactive
        element.classList.add('inactive');
        // C?p nh?t text hi?n th?
        const statusElement = element.querySelector('.status');
        if (statusElement) {
            statusElement.textContent = '?ã vô hi?u hóa';
            statusElement.classList.add('text-muted');
        }
        // ?n nút delete, hi?n th? nút reactive
        const deleteBtn = element.querySelector('.delete-btn');
        const reactiveBtn = element.querySelector('.reactive-btn');
        if (deleteBtn) deleteBtn.style.display = 'none';
        if (reactiveBtn) reactiveBtn.style.display = 'inline-block';
    }
}

// Function ?? xóa bank account kh?i UI hoàn toàn
function removeBankAccountFromUI(bankAccountId) {
    const element = document.getElementById(`bank-account-${bankAccountId}`);
    if (element) {
        element.remove();
    }
}
```

### React Example

```jsx
// Hook ?? x? lý smart delete
const useSmartDeleteBankAccount = () => {
    const [loading, setLoading] = useState(false);
    
    const smartDelete = async (bankAccountId) => {
        setLoading(true);
        try {
            const response = await fetch(`/api/v1.0/bankaccounts/${bankAccountId}`, {
                method: 'DELETE'
            });
            
            const result = await response.json();
            
            if (result.success) {
                if (result.data?.action === 'deactivated') {
                    // Deactivated - c?p nh?t state
                    return {
                        success: true,
                        action: 'deactivated',
                        bankAccount: result.data.bankAccount,
                        message: result.message
                    };
                } else {
                    // Deleted completely
                    return {
                        success: true,
                        action: 'deleted',
                        message: result.message
                    };
                }
            } else {
                throw new Error(result.message);
            }
        } catch (error) {
            return {
                success: false,
                message: error.message
            };
        } finally {
            setLoading(false);
        }
    };
    
    return { smartDelete, loading };
};

// Component s? d?ng
const BankAccountItem = ({ bankAccount, onUpdate, onRemove }) => {
    const { smartDelete, loading } = useSmartDeleteBankAccount();
    
    const handleDelete = async () => {
        const result = await smartDelete(bankAccount.id);
        
        if (result.success) {
            if (result.action === 'deactivated') {
                toast.warning(result.message);
                onUpdate(result.bankAccount); // C?p nh?t bank account trong list
            } else {
                toast.success(result.message);
                onRemove(bankAccount.id); // Xóa kh?i list
            }
        } else {
            toast.error(result.message);
        }
    };
    
    return (
        <div className={`bank-account-item ${!bankAccount.isActive ? 'inactive' : ''}`}>
            {/* Bank account info */}
            <div className="bank-account-info">
                <h4>{bankAccount.bankName}</h4>
                <p>****{bankAccount.accountNumber.slice(-4)}</p>
                <span className={`status ${bankAccount.isActive ? 'active' : 'inactive'}`}>
                    {bankAccount.isActive ? 'Ho?t ??ng' : '?ã vô hi?u hóa'}
                </span>
            </div>
            
            {/* Actions */}
            <div className="actions">
                <button 
                    onClick={handleDelete}
                    disabled={loading}
                    className="btn btn-danger"
                >
                    {loading ? '?ang x? lý...' : 'Xóa'}
                </button>
            </div>
        </div>
    );
};
```

## Best Practices

### 1. **UI/UX Recommendations**

- **Confirmation Dialog**: Hi?n th? dialog xác nh?n tr??c khi delete
- **Clear Messaging**: Gi?i thích rõ s? khác bi?t gi?a "xóa" và "vô hi?u hóa"
- **Visual Indicators**: S? d?ng màu s?c và icon ?? phân bi?t tr?ng thái
- **Restore Option**: Cung c?p option ?? reactive tài kho?n ?ã b? deactivate

### 2. **Error Handling**

```javascript
// X? lý các case error ph? bi?n
const handleSmartDeleteError = (error, bankAccount) => {
    if (error.message.includes('tài kho?n m?c ??nh')) {
        showDialog({
            type: 'warning',
            title: 'Không th? xóa tài kho?n m?c ??nh',
            message: 'Vui lòng ??t tài kho?n khác làm m?c ??nh tr??c khi xóa.',
            actions: [
                { text: '??t m?c ??nh', action: () => showSetDefaultDialog() },
                { text: 'H?y', action: () => {} }
            ]
        });
    } else if (error.message.includes('không tìm th?y')) {
        showNotification('error', 'Tài kho?n ngân hàng không t?n t?i ho?c ?ã b? xóa.');
        // Remove from UI if it's stale data
        removeBankAccountFromUI(bankAccount.id);
    } else {
        showNotification('error', `L?i: ${error.message}`);
    }
};
```

### 3. **Security Considerations**

- **Authorization**: ??m b?o user ch? có th? xóa bank account c?a mình
- **Audit Trail**: Log các thao tác delete/deactivate cho audit
- **Rate Limiting**: Implement rate limiting ?? tránh spam requests

### 4. **Database Considerations**

- **Foreign Key Constraints**: RefundHistories có foreign key constraint v?i BankAccounts
- **Soft Delete Pattern**: Deactivate th?c ch?t là soft delete pattern
- **Data Integrity**: ??m b?o data consistency khi có concurrent requests

## Testing Scenarios

### Unit Tests c?n cover:

1. **Delete thành công khi không có RefundHistories**
2. **Deactivate thành công khi có RefundHistories**
3. **L?i khi bank account không t?n t?i**
4. **L?i khi là default account mà còn accounts khác**
5. **L?i khi ID không h?p l?**

### Integration Tests:

1. **End-to-end workflow** t? create bank account ? create refund ? delete bank account
2. **Concurrent delete requests** trên cùng bank account
3. **Performance test** v?i large dataset RefundHistories

Tính n?ng Smart Delete này ??m b?o data integrity trong khi cung c?p UX t?t nh?t cho users! ??