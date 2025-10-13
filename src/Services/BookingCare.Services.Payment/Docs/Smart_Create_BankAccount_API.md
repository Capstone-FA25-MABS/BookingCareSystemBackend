# Smart Create Bank Account API Documentation

## T?ng quan

Smart Create là tính n?ng thông minh trong BankAccountsController giúp x? lý vi?c t?o bank account m?t cách thông minh b?ng cách:

1. **Ki?m tra user ?ã có account này ch?a** (cùng AccountNumber + BankCode)
2. **N?u có và ?ang inactive** ? **Reactive l?i** thay vì báo l?i
3. **N?u ch?a có** ? **T?o m?i** bình th??ng
4. **N?u có và ?ang active** ? **Báo l?i conflict**

## Business Logic Flow

```
Create Bank Account Request
         ?
   Find existing account (same AccountNumber + BankCode + UserId)
         ?
    ???????????????????
    ?                 ?
  Found?           Not Found
    ?                 ?
Is Active?        Check Global
    ??Yes??? 409 Conflict    ?
    ?                Account exists for other user?
   No                  ??Yes??? 409 Conflict  
    ?                  ?
REACTIVE            No
(Update + Active)    ?
    ?              CREATE NEW
200 OK + Account     ?
                 201 Created + Account
```

## API Endpoint

### POST `/api/v1.0/bankaccounts`

**Request Body:**
```json
{
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "bankCode": "VCB",
    "bankName": "Vietcombank",
    "accountNumber": "1234567890",
    "accountName": "NGUYEN VAN A",
    "isDefault": false
}
```

## Response Examples

### 1. T?o m?i thành công (Account ch?a t?n t?i)

```json
{
    "success": true,
    "message": "T?o bank account thành công",
    "data": {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "userId": "550e8400-e29b-41d4-a716-446655440001",
        "bankCode": "VCB",
        "bankName": "Vietcombank",
        "accountNumber": "******7890",
        "fullAccountNumber": "1234567890",
        "accountName": "NGUYEN VAN A",
        "isDefault": true,
        "isActive": true,
        "createdAt": "2024-01-07T10:00:00Z",
        "updatedAt": "2024-01-07T10:00:00Z"
    },
    "timestamp": "2024-01-07T10:00:00Z"
}
```

### 2. Reactive thành công (Account ?ã t?n t?i nh?ng inactive)

```json
{
    "success": true,
    "message": "T?o bank account thành công",
    "data": {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "userId": "550e8400-e29b-41d4-a716-446655440001",
        "bankCode": "VCB",
        "bankName": "Vietcombank - C?p nh?t",
        "accountNumber": "******7890",
        "fullAccountNumber": "1234567890",
        "accountName": "NGUYEN VAN A - C?p nh?t",
        "isDefault": false,
        "isActive": true,
        "createdAt": "2024-01-01T10:00:00Z",
        "updatedAt": "2024-01-07T10:00:00Z"
    },
    "timestamp": "2024-01-07T10:00:00Z"
}
```

### 3. Conflict - Account ?ã t?n t?i và active

```json
{
    "success": false,
    "message": "Tài kho?n ngân hàng 1234567890 t?i VCB ?ã t?n t?i và ?ang ho?t ??ng",
    "timestamp": "2024-01-07T10:00:00Z"
}
```

### 4. Conflict - Account thu?c user khác

```json
{
    "success": false,
    "message": "Tài kho?n ngân hàng 1234567890 t?i VCB ?ã ???c s? d?ng b?i user khác",
    "timestamp": "2024-01-07T10:00:00Z"
}
```

### 5. Validation Error

```json
{
    "success": false,
    "message": "D? li?u không h?p l?",
    "errors": [
        "Mã ngân hàng không ???c ?? tr?ng",
        "S? tài kho?n ch? ???c ch?a các ch? s?"
    ],
    "timestamp": "2024-01-07T10:00:00Z"
}
```

## Backend Logic Details

### Repository Layer Changes

**New Methods Added:**

1. **`AccountNumberExistsForUserAsync`** - Ki?m tra account theo user c? th?
2. **`FindByAccountNumberAndUserAsync`** - Tìm account c?a user (bao g?m inactive)

**Method Separation:**

- **`AccountNumberExistsAsync`** - Global check (t?t c? users)  
- **`AccountNumberExistsForUserAsync`** - User-specific check

### Service Layer Logic

```csharp
// Step 1: Tìm existing account c?a user này
var existingAccount = await FindByAccountNumberAndUserAsync(accountNumber, bankCode, userId);

if (existingAccount != null) {
    if (existingAccount.IsActive) {
        // Conflict: Account active
        throw ConflictException("Account already active");
    } else {
        // Reactive: Update info + set active
        existingAccount.BankName = newBankName;
        existingAccount.AccountName = newAccountName;
        existingAccount.IsActive = true;
        // Handle default logic...
        return Update(existingAccount);
    }
}

// Step 2: Ki?m tra global (user khác)
var globalExists = await AccountNumberExistsAsync(accountNumber, bankCode);
if (globalExists) {
    throw ConflictException("Account used by other user");
}

// Step 3: T?o m?i
return Create(newAccount);
```

## Frontend Integration

### JavaScript Example

```javascript
async function createBankAccount(accountData) {
    try {
        const response = await fetch('/api/v1.0/bankaccounts', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(accountData)
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Check if it was created or reactivated
            if (response.status === 201) {
                showNotification('success', 'Tài kho?n ngân hàng ???c t?o m?i thành công!');
            } else {
                showNotification('info', 'Tài kho?n ngân hàng ?ã t?n t?i và ???c kích ho?t l?i!');
            }
            
            // Update UI with the account data
            updateBankAccountsList(result.data);
            
        } else {
            // Handle error
            if (result.message.includes('?ã t?n t?i và ?ang ho?t ??ng')) {
                showNotification('warning', 'Tài kho?n này ?ã ???c thêm tr??c ?ó và ?ang ho?t ??ng.');
            } else if (result.message.includes('?ã ???c s? d?ng b?i user khác')) {
                showNotification('error', 'S? tài kho?n này ?ã ???c s? d?ng b?i ng??i dùng khác.');
            } else {
                showNotification('error', result.message);
            }
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('error', 'Có l?i x?y ra khi thêm tài kho?n ngân hàng');
    }
}

// Usage
createBankAccount({
    userId: "550e8400-e29b-41d4-a716-446655440001",
    bankCode: "VCB",
    bankName: "Vietcombank",
    accountNumber: "1234567890",
    accountName: "NGUYEN VAN A",
    isDefault: false
});
```

### React Hook Example

```jsx
const useBankAccountCreate = () => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const createBankAccount = async (accountData) => {
        setLoading(true);
        setError(null);

        try {
            const response = await fetch('/api/v1.0/bankaccounts', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(accountData)
            });

            const result = await response.json();

            if (result.success) {
                // Determine action type based on status code or response data
                const actionType = response.status === 201 ? 'created' : 'reactivated';
                
                return {
                    success: true,
                    action: actionType,
                    account: result.data,
                    message: result.message
                };
            } else {
                throw new Error(result.message);
            }
        } catch (err) {
            setError(err.message);
            return { success: false, message: err.message };
        } finally {
            setLoading(false);
        }
    };

    return { createBankAccount, loading, error };
};

// Component usage
const BankAccountForm = () => {
    const { createBankAccount, loading } = useBankAccountCreate();
    const [toast, setToast] = useState(null);

    const handleSubmit = async (formData) => {
        const result = await createBankAccount(formData);
        
        if (result.success) {
            if (result.action === 'created') {
                setToast({ type: 'success', message: 'Tài kho?n m?i ???c t?o thành công!' });
            } else {
                setToast({ type: 'info', message: 'Tài kho?n ?ã ???c kích ho?t l?i!' });
            }
            
            // Update parent state or redirect
            onAccountCreated(result.account);
        } else {
            setToast({ type: 'error', message: result.message });
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            {/* Form fields */}
            <button type="submit" disabled={loading}>
                {loading ? '?ang x? lý...' : 'Thêm tài kho?n'}
            </button>
            
            {toast && <Toast type={toast.type} message={toast.message} />}
        </form>
    );
};
```

## Database Schema Impact

### No Schema Changes Required

Logic m?i hoàn toàn backward compatible:
- Không thay ??i table structure
- Không thay ??i existing indexes
- Ch? thêm business logic m?i

### Query Performance

**New Queries Added:**
1. `FindByAccountNumberAndUserAsync`: Single user lookup - **Fast**
2. `AccountNumberExistsForUserAsync`: User-specific check - **Fast**

**Existing Performance:**
- Global check (`AccountNumberExistsAsync`) v?n ???c s? d?ng
- T?t c? queries ??u có index support

## Security Considerations

### Improved Security

1. **User Isolation**: Account ch? reactive cho chính user ?ó
2. **Global Uniqueness**: V?n ??m b?o 1 account number = 1 user
3. **No Data Leakage**: Không expose thông tin account c?a user khác

### Audit Trail

```csharp
// Logs ???c thêm cho audit
LogInfo("Tìm th?y bank account inactive, ?ang reactive l?i: {Id}", null, existingAccount.Id);
LogInfo("Bank account ?ã ???c reactive thành công: {Id}", null, reactivated.Id);
LogInfo("T?o bank account hoàn toàn m?i cho User: {UserId}", null, request.UserId);
```

## Testing Scenarios

### Unit Tests Required

1. **Create New Account** - Normal flow
2. **Reactivate Inactive Account** - Smart logic
3. **Conflict Active Account** - Same user
4. **Conflict Other User** - Different user  
5. **Update Account Info** - During reactivation
6. **Default Account Logic** - For both create & reactive

### Integration Tests

1. **Concurrent Creation** - Same account, same user
2. **Cross-User Conflicts** - Same account, different users
3. **Mixed Scenarios** - Create + reactive + conflicts

### API Tests

```bash
# Test 1: Create new account
POST /api/v1.0/bankaccounts
# Expect: 201 Created

# Test 2: Try create same account again  
POST /api/v1.0/bankaccounts (same data)
# Expect: 409 Conflict (already active)

# Test 3: Deactivate account
PATCH /api/v1.0/bankaccounts/{id}/toggle-status
# Expect: 200 OK (deactivated)

# Test 4: Create same account again (after deactivate)
POST /api/v1.0/bankaccounts (same data)
# Expect: 200 OK (reactivated)
```

## Performance Impact

### Minimal Performance Cost

- **1 extra query** per create operation (FindByAccountNumberAndUser)
- **Same number of database hits** as before
- **All queries indexed** - no performance degradation

### Improved User Experience

- **No more "account exists" errors** for user's own inactive accounts
- **Seamless reactivation** without manual steps
- **Consistent data** (updated bank names, account names)

Smart Create gi?i quy?t ???c v?n ?? user experience khi user mu?n re-add m?t account ?ã b? deactivate tr??c ?ó! ??