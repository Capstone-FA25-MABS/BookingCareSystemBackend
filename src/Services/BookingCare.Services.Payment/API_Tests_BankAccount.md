# BankAccount API Test Collection

## Base URL
```
http://localhost:6011/api/v1.0/bankaccounts
```

## 1. Create Bank Account
```http
POST /api/v1.0/bankaccounts
Content-Type: application/json

{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "bankCode": "VCB",
    "bankName": "Vietcombank",
    "accountNumber": "1234567890",
    "accountName": "NGUYEN VAN A",
    "isDefault": true
}
```

## 2. Get Bank Account by ID
```http
GET /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
```

## 3. Get All Bank Accounts by User
```http
GET /api/v1.0/bankaccounts/user/550e8400-e29b-41d4-a716-446655440000
```

### Response Examples for Get All Bank Accounts by User

#### Multiple Accounts Response
```json
{
    "success": true,
    "message": "Lấy 3 bank accounts thành công",
    "data": {
        "accounts": [
            {
                "id": "550e8400-e29b-41d4-a716-446655440001",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "bankCode": "VCB",
                "bankName": "Vietcombank",
                "accountNumber": "******7890",
                "fullAccountNumber": "1234567890",
                "accountName": "NGUYEN VAN A",
                "isDefault": true,
                "isActive": true,
                "createdAt": "2024-01-06T10:00:00Z",
                "updatedAt": "2024-01-06T10:00:00Z"
            },
            {
                "id": "550e8400-e29b-41d4-a716-446655440002",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "bankCode": "TCB",
                "bankName": "Techcombank",
                "accountNumber": "******1234",
                "fullAccountNumber": "9876541234",
                "accountName": "NGUYEN VAN A",
                "isDefault": false,
                "isActive": true,
                "createdAt": "2024-01-06T11:00:00Z",
                "updatedAt": "2024-01-06T11:00:00Z"
            },
            {
                "id": "550e8400-e29b-41d4-a716-446655440003",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "bankCode": "BIDV",
                "bankName": "BIDV",
                "accountNumber": "******5678",
                "fullAccountNumber": "1111225678",
                "accountName": "NGUYEN VAN A",
                "isDefault": false,
                "isActive": true,
                "createdAt": "2024-01-06T12:00:00Z",
                "updatedAt": "2024-01-06T12:00:00Z"
            }
        ],
        "count": 3
    },
    "timestamp": "2024-01-06T12:00:00Z"
}
```

#### Single Account Response
```json
{
    "success": true,
    "message": "Lấy 1 bank account thành công",
    "data": {
        "accounts": [
            {
                "id": "550e8400-e29b-41d4-a716-446655440001",
                "userId": "550e8400-e29b-41d4-a716-446655440000",
                "bankCode": "VCB",
                "bankName": "Vietcombank",
                "accountNumber": "******7890",
                "fullAccountNumber": "1234567890",
                "accountName": "NGUYEN VAN A",
                "isDefault": true,
                "isActive": true,
                "createdAt": "2024-01-06T10:00:00Z",
                "updatedAt": "2024-01-06T10:00:00Z"
            }
        ],
        "count": 1
    },
    "timestamp": "2024-01-06T12:00:00Z"
}
```

#### No Accounts Response
```json
{
    "success": true,
    "message": "Không tìm thấy bank account nào",
    "data": {
        "accounts": [],
        "count": 0
    },
    "timestamp": "2024-01-06T12:00:00Z"
}
```

### Frontend Usage Examples

#### JavaScript/TypeScript Usage
```javascript
// Easy access to count for frontend logic
const response = await fetch('/api/v1.0/bankaccounts/user/{userId}');
const result = await response.json();

// Direct access to count - no need to calculate array length
const accountCount = result.data.count;
const accounts = result.data.accounts;

// Frontend logic based on count
if (accountCount === 0) {
    showEmptyState();
} else if (accountCount === 1) {
    showSingleAccountView(accounts[0]);
} else {
    showMultipleAccountsView(accounts, accountCount);
}

// Display count in UI
document.getElementById('account-count').textContent = `${accountCount} tài khoản`;
```

#### React/Vue.js Usage
```jsx
// React example
const BankAccountsList = ({ userId }) => {
    const [data, setData] = useState({ accounts: [], count: 0 });
    
    useEffect(() => {
        fetchBankAccounts(userId).then(response => {
            setData(response.data); // { accounts: [...], count: 3 }
        });
    }, [userId]);
    
    return (
        <div>
            <h3>Tài khoản ngân hàng ({data.count})</h3>
            {data.count === 0 ? (
                <EmptyState />
            ) : (
                <AccountList accounts={data.accounts} />
            )}
        </div>
    );
};
```

## 4. Get Default Bank Account
```http
GET /api/v1.0/bankaccounts/user/550e8400-e29b-41d4-a716-446655440000/default
```

## 5. Get Bank Accounts with Pagination
```http
POST /api/v1.0/bankaccounts/search
Content-Type: application/json

{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "page": 1,
    "pageSize": 20,
    "activeOnly": true
}
```

## 6. Update Bank Account
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "bankCode": "TCB",
    "bankName": "Techcombank - Chi nhánh Hà Nội",
    "accountNumber": "9876543210",
    "accountName": "NGUYEN VAN A",
    "isDefault": false,
    "isActive": true
}
```

### Update Bank Account - Partial Update Examples

#### Update only Bank Code
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "bankCode": "BIDV"
}
```

#### Update only Bank Name
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "bankName": "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam"
}
```

#### Update only Account Number
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "accountNumber": "9876543210"
}
```

#### Update only Account Name
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "accountName": "TRAN THI B"
}
```

#### Update Status Only
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "isActive": false
}
```

#### Update multiple fields including Account Number
```http
PUT /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
Content-Type: application/json

{
    "bankCode": "VTB",
    "accountNumber": "1111222233",
    "accountName": "LE VAN C"
}
```

## 7. Set Bank Account as Default
```http
PATCH /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001/set-default
```

## 8. Toggle Bank Account Status
```http
PATCH /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001/toggle-status
```

## 9. Delete Bank Account
```http
DELETE /api/v1.0/bankaccounts/550e8400-e29b-41d4-a716-446655440001
```

## Expected Response Format

### Success Response
```json
{
    "success": true,
    "message": "Operation completed successfully",
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440001",
        "userId": "550e8400-e29b-41d4-a716-446655440000",
        "bankCode": "VCB",
        "bankName": "Vietcombank",
        "accountNumber": "******7890",
        "fullAccountNumber": "1234567890",
        "accountName": "NGUYEN VAN A",
        "isDefault": true,
        "isActive": true,
        "createdAt": "2024-01-06T10:00:00Z",
        "updatedAt": "2024-01-06T10:00:00Z"
    },
    "timestamp": "2024-01-06T10:00:00Z"
}
```

### Error Response
```json
{
    "success": false,
    "message": "Validation failed",
    "errors": [
        "User ID không được để trống",
        "Số tài khoản chỉ được chứa các chữ số"
    ],
    "timestamp": "2024-01-06T10:00:00Z"
}
```

### Conflict Error Response (Duplicate Account Number)
```json
{
    "success": false,
    "message": "Tài khoản ngân hàng 9876543210 tại TCB đã tồn tại",
    "timestamp": "2024-01-06T10:00:00Z"
}
```

## Business Rules

### Validation Rules
1. **User ID**: Required, must be valid GUID
2. **Bank Code**: Required for creation, 2-10 characters, alphanumeric uppercase, optional for updates
3. **Bank Name**: Required for creation, max 255 characters, optional for updates
4. **Account Number**: Required for creation, 6-20 digits only, optional for updates with uniqueness validation
5. **Account Name**: Required for creation, max 255 characters, Vietnamese letters and spaces only, optional for updates

### Update Rules
- **Flexible Updates**: Any combination of `bankCode`, `bankName`, `accountNumber`, `accountName`, `isDefault`, `isActive` can be updated
- **Partial Updates**: Only specified fields are updated, others remain unchanged
- **At Least One Field**: At least one field must be provided in update request
- **Account Number Updates**: Account number can be updated but must be unique within the same bank

### Business Logic
1. **Unique Constraint**: Same account number + bank code cannot exist (enforced during both creation and updates)
2. **Default Account**: Only one default account per user
3. **First Account**: Automatically set as default if it's user's first account
4. **Delete Protection**: Cannot delete default account if other accounts exist
5. **Status Rules**: Cannot set inactive account as default
6. **Cross-field Validation**: When updating account number, uniqueness is checked against the current or new bank code

### API Response Enhancements
1. **Structured Data**: Response includes both `accounts` array and `count` field
2. **Frontend-Friendly**: Direct access to count without array length calculation
3. **Dynamic Messages**: 
   - `"Lấy 3 bank accounts thành công"` for multiple accounts
   - `"Lấy 1 bank account thành công"` for single account
   - `"Không tìm thấy bank account nào"` for no accounts
4. **Consistent Data Structure**: Always returns same structure regardless of count
5. **Easy Integration**: Simple to use in frontend frameworks (React, Vue, Angular)

### Frontend Benefits
1. **Direct Count Access**: `result.data.count` instead of `result.data.length`
2. **Conditional Logic**: Easy to implement different UI states based on count
3. **Performance**: No need to calculate array length on frontend
4. **Type Safety**: Clear data structure for TypeScript projects
5. **Consistent API**: Same response format for empty, single, and multiple results

### Security Features
1. **Account Number Masking**: Only show last 4 digits in public responses
2. **Full Number Access**: Only available to account owner
3. **Input Validation**: Comprehensive validation on all inputs
4. **SQL Injection Protection**: Parameterized queries and EF Core protection
5. **Uniqueness Validation**: Prevents duplicate account numbers within the same bank
6. **Transaction Safety**: Updates are performed atomically

### Common Bank Codes in Vietnam
- **VCB**: Vietcombank
- **TCB**: Techcombank  
- **BIDV**: BIDV
- **VTB**: Vietinbank
- **ACB**: ACB
- **SHB**: SHB
- **TPB**: TPBank
- **MB**: MBBank

### Error Scenarios
1. **Duplicate Account**: When trying to update to an account number that already exists with the same bank code
2. **Invalid Format**: Account number must be 6-20 digits only
3. **Empty Update**: At least one field must be provided for update
4. **Invalid Bank Code**: Must be uppercase alphanumeric, max 10 characters