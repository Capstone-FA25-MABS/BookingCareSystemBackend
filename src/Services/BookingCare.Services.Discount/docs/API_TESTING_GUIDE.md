# Discount Service API Testing Guide

This guide provides comprehensive examples for testing all Discount Service APIs using both **Postman** and **curl** commands.

## Service Information

- **Base URL**: `http://localhost:6007/api/discounts`
- **Health Check**: `http://localhost:6007/api/discounts/health`
- **Protocol**: HTTP/1.1 and HTTP/2
- **Response Format**: JSON

## Table of Contents

1. [Health Check](#health-check)
2. [Get Discount by ID](#get-discount-by-id)
3. [Get Discount by Code](#get-discount-by-code)
4. [Get Discounts with Filtering](#get-discounts-with-filtering)
5. [Get Active Discounts by Clinic](#get-active-discounts-by-clinic)
6. [Get Applicable Discounts](#get-applicable-discounts)
7. [Create Discount](#create-discount)
8. [Update Discount](#update-discount)
9. [Delete Discount](#delete-discount)
10. [Validate Discount](#validate-discount)
11. [Use Discount](#use-discount)
12. [Revert Discount Usage](#revert-discount-usage)
13. [Activate Discount](#activate-discount)
14. [Deactivate Discount](#deactivate-discount)
15. [Update Expired Discounts](#update-expired-discounts)
16. [Calculate Discount Amount](#calculate-discount-amount)

---

## 1. Health Check

### Purpose
Check if the Discount Service is running and healthy.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts/health`

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts/health" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount service is healthy",
  "data": {
    "Status": "Healthy",
    "Service": "Discount",
    "Timestamp": "2025-08-29T10:30:00.000Z"
  },
  "statusCode": 200
}
```

---

## 2. Get Discount by ID

### Purpose
Retrieve a specific discount by its ID.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts/{id}`
- **Path Variables**: `id` = `1`

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts/1" \
  -H "Content-Type: application/json"
```

### Expected Response (Success)
```json
{
  "success": true,
  "message": "Discount retrieved successfully",
  "data": {
    "id": 1,
    "code": "SUMMER2025",
    "name": "Summer Discount",
    "description": "20% off all appointments",
    "clinicId": 1,
    "specialtyId": null,
    "doctorId": null,
    "applicableTo": "ALL",
    "amount": 20.00,
    "discountType": "PERCENTAGE",
    "startDate": "2025-06-01T00:00:00",
    "endDate": "2025-08-31T23:59:59",
    "maxUses": 100,
    "usesCount": 5,
    "status": "ACTIVE",
    "createdAt": "2025-06-01T10:00:00",
    "updatedAt": "2025-06-01T10:00:00"
  },
  "statusCode": 200
}
```

### Expected Response (Not Found)
```json
{
  "success": false,
  "message": "Discount with ID 999 not found",
  "statusCode": 404
}
```

---

## 3. Get Discount by Code

### Purpose
Retrieve a discount by its unique code.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts/by-code/{code}`
- **Path Variables**: `code` = `SUMMER2025`

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts/by-code/SUMMER2025" \
  -H "Content-Type: application/json"
```

### Expected Response
Same as "Get Discount by ID" but retrieved by code instead of ID.

---

## 4. Get Discounts with Filtering

### Purpose
Get a paginated list of discounts with optional filtering.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts`
- **Query Parameters**:
  - `clinicId`: `1`
  - `status`: `ACTIVE`
  - `pageNumber`: `1`
  - `pageSize`: `10`
  - `searchTerm`: `summer` (optional)
  - `applicableTo`: `ALL` (optional)
  - `startDate`: `2025-06-01` (optional)
  - `endDate`: `2025-12-31` (optional)

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts?clinicId=1&status=ACTIVE&pageNumber=1&pageSize=10&searchTerm=summer" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Discounts retrieved successfully",
  "data": {
    "discounts": [
      {
        "id": 1,
        "code": "SUMMER2025",
        "name": "Summer Discount",
        "description": "20% off all appointments",
        "clinicId": 1,
        "specialtyId": null,
        "doctorId": null,
        "applicableTo": "ALL",
        "amount": 20.00,
        "discountType": "PERCENTAGE",
        "startDate": "2025-06-01T00:00:00",
        "endDate": "2025-08-31T23:59:59",
        "maxUses": 100,
        "usesCount": 5,
        "status": "ACTIVE",
        "createdAt": "2025-06-01T10:00:00",
        "updatedAt": "2025-06-01T10:00:00"
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "statusCode": 200
}
```

---

## 5. Get Active Discounts by Clinic

### Purpose
Get all active discounts for a specific clinic.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts/clinic/{clinicId}/active`
- **Path Variables**: `clinicId` = `1`

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts/clinic/1/active" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Active discounts for clinic 1 retrieved successfully",
  "data": [
    {
      "id": 1,
      "code": "SUMMER2025",
      "name": "Summer Discount",
      "amount": 20.00,
      "discountType": "PERCENTAGE",
      "startDate": "2025-06-01T00:00:00",
      "endDate": "2025-08-31T23:59:59",
      "remainingUses": 95
    }
  ],
  "statusCode": 200
}
```

---

## 6. Get Applicable Discounts

### Purpose
Get discounts applicable to specific clinic/specialty/doctor combination.

### Postman Setup
- **Method**: `GET`
- **URL**: `http://localhost:6007/api/discounts/applicable`
- **Query Parameters**:
  - `clinicId`: `1`
  - `specialtyId`: `2` (optional)
  - `doctorId`: `3` (optional)

### curl Command
```bash
curl -X GET "http://localhost:6007/api/discounts/applicable?clinicId=1&specialtyId=2&doctorId=3" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Applicable discounts retrieved successfully",
  "data": [
    {
      "id": 1,
      "code": "SUMMER2025",
      "name": "Summer Discount",
      "amount": 20.00,
      "discountType": "PERCENTAGE",
      "applicableTo": "ALL",
      "remainingUses": 95
    }
  ],
  "statusCode": 200
}
```

---

## 7. Create Discount

### Purpose
Create a new discount.

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "code": "NEWPATIENT50",
  "name": "New Patient Discount",
  "description": "50% off first appointment for new patients",
  "clinicId": 1,
  "specialtyId": null,
  "doctorId": null,
  "applicableTo": "ALL",
  "amount": 50.00,
  "discountType": "PERCENTAGE",
  "startDate": "2025-09-01T00:00:00",
  "endDate": "2025-12-31T23:59:59",
  "maxUses": 200,
  "status": "ACTIVE"
}
```

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "NEWPATIENT50",
    "name": "New Patient Discount",
    "description": "50% off first appointment for new patients",
    "clinicId": 1,
    "specialtyId": null,
    "doctorId": null,
    "applicableTo": "ALL",
    "amount": 50.00,
    "discountType": "PERCENTAGE",
    "startDate": "2025-09-01T00:00:00",
    "endDate": "2025-12-31T23:59:59",
    "maxUses": 200,
    "status": "ACTIVE"
  }'
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount created successfully",
  "data": {
    "id": 2,
    "code": "NEWPATIENT50",
    "name": "New Patient Discount",
    "description": "50% off first appointment for new patients",
    "clinicId": 1,
    "specialtyId": null,
    "doctorId": null,
    "applicableTo": "ALL",
    "amount": 50.00,
    "discountType": "PERCENTAGE",
    "startDate": "2025-09-01T00:00:00",
    "endDate": "2025-12-31T23:59:59",
    "maxUses": 200,
    "usesCount": 0,
    "status": "ACTIVE",
    "createdAt": "2025-08-29T10:30:00",
    "updatedAt": "2025-08-29T10:30:00"
  },
  "statusCode": 201
}
```

---

## 8. Update Discount

### Purpose
Update an existing discount.

### Postman Setup
- **Method**: `PUT`
- **URL**: `http://localhost:6007/api/discounts/{id}`
- **Path Variables**: `id` = `2`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "id": 2,
  "name": "Updated New Patient Discount",
  "description": "Updated: 30% off first appointment for new patients",
  "amount": 30.00,
  "endDate": "2025-11-30T23:59:59",
  "maxUses": 150
}
```

### curl Command
```bash
curl -X PUT "http://localhost:6007/api/discounts/2" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 2,
    "name": "Updated New Patient Discount",
    "description": "Updated: 30% off first appointment for new patients",
    "amount": 30.00,
    "endDate": "2025-11-30T23:59:59",
    "maxUses": 150
  }'
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount updated successfully",
  "data": {
    "id": 2,
    "code": "NEWPATIENT50",
    "name": "Updated New Patient Discount",
    "description": "Updated: 30% off first appointment for new patients",
    "amount": 30.00,
    "endDate": "2025-11-30T23:59:59",
    "maxUses": 150,
    "updatedAt": "2025-08-29T10:35:00"
  },
  "statusCode": 200
}
```

---

## 9. Delete Discount

### Purpose
Delete a discount (only if it hasn't been used).

### Postman Setup
- **Method**: `DELETE`
- **URL**: `http://localhost:6007/api/discounts/{id}`
- **Path Variables**: `id` = `2`

### curl Command
```bash
curl -X DELETE "http://localhost:6007/api/discounts/2" \
  -H "Content-Type: application/json"
```

### Expected Response (Success)
```json
{
  "success": true,
  "message": "Discount deleted successfully",
  "statusCode": 200
}
```

### Expected Response (Cannot Delete - Used)
```json
{
  "success": false,
  "message": "Cannot delete discount that has been used",
  "statusCode": 400
}
```

---

## 10. Validate Discount

### Purpose
Validate if a discount code can be applied for a specific scenario.

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts/validate`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "code": "SUMMER2025",
  "clinicId": 1,
  "specialtyId": 2,
  "doctorId": 3,
  "totalAmount": 100.00
}
```

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SUMMER2025",
    "clinicId": 1,
    "specialtyId": 2,
    "doctorId": 3,
    "totalAmount": 100.00
  }'
```

### Expected Response (Valid)
```json
{
  "success": true,
  "message": "Discount is valid",
  "data": {
    "isValid": true,
    "discountAmount": 20.00,
    "finalAmount": 80.00,
    "validationMessage": "Discount can be applied successfully"
  },
  "statusCode": 200
}
```

### Expected Response (Invalid)
```json
{
  "success": true,
  "message": "Discount validation completed",
  "data": {
    "isValid": false,
    "discountAmount": 0.00,
    "finalAmount": 100.00,
    "validationMessage": "Discount code has expired"
  },
  "statusCode": 200
}
```

---

## 11. Use Discount

### Purpose
Apply a discount code to a transaction.

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts/use`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "code": "SUMMER2025",
  "clinicId": 1,
  "specialtyId": 2,
  "doctorId": 3,
  "totalAmount": 100.00
}
```

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts/use" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SUMMER2025",
    "clinicId": 1,
    "specialtyId": 2,
    "doctorId": 3,
    "totalAmount": 100.00
  }'
```

### Expected Response (Success)
```json
{
  "success": true,
  "message": "Discount applied successfully",
  "data": {
    "success": true,
    "discountId": 1,
    "discountAmount": 20.00,
    "finalAmount": 80.00,
    "remainingUses": 94
  },
  "statusCode": 200
}
```

### Expected Response (Failed)
```json
{
  "success": true,
  "message": "Discount usage completed",
  "data": {
    "success": false,
    "discountId": 0,
    "discountAmount": 0.00,
    "finalAmount": 100.00,
    "errorMessage": "Discount code has reached maximum usage limit"
  },
  "statusCode": 200
}
```

---

## 12. Revert Discount Usage

### Purpose
Revert a discount usage (for order cancellations).

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts/revert`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "code": "SUMMER2025",
  "clinicId": 1
}
```

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts/revert" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SUMMER2025",
    "clinicId": 1
  }'
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount usage reverted successfully",
  "statusCode": 200
}
```

---

## 13. Activate Discount

### Purpose
Activate a currently inactive discount.

### Postman Setup
- **Method**: `PATCH`
- **URL**: `http://localhost:6007/api/discounts/{id}/activate`
- **Path Variables**: `id` = `1`

### curl Command
```bash
curl -X PATCH "http://localhost:6007/api/discounts/1/activate" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount activated successfully",
  "statusCode": 200
}
```

---

## 14. Deactivate Discount

### Purpose
Deactivate a currently active discount.

### Postman Setup
- **Method**: `PATCH`
- **URL**: `http://localhost:6007/api/discounts/{id}/deactivate`
- **Path Variables**: `id` = `1`

### curl Command
```bash
curl -X PATCH "http://localhost:6007/api/discounts/1/deactivate" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount deactivated successfully",
  "statusCode": 200
}
```

---

## 15. Update Expired Discounts

### Purpose
Administrative operation to update all expired discounts to EXPIRED status.

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts/update-expired`

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts/update-expired" \
  -H "Content-Type: application/json"
```

### Expected Response
```json
{
  "success": true,
  "message": "Updated 3 expired discounts",
  "data": {
    "UpdatedCount": 3
  },
  "statusCode": 200
}
```

---

## 16. Calculate Discount Amount

### Purpose
Calculate the discount amount for a given code and original amount.

### Postman Setup
- **Method**: `POST`
- **URL**: `http://localhost:6007/api/discounts/calculate`
- **Headers**: `Content-Type: application/json`
- **Body** (raw JSON):

```json
{
  "code": "SUMMER2025",
  "originalAmount": 150.00,
  "clinicId": 1,
  "specialtyId": 2,
  "doctorId": 3
}
```

### curl Command
```bash
curl -X POST "http://localhost:6007/api/discounts/calculate" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SUMMER2025",
    "originalAmount": 150.00,
    "clinicId": 1,
    "specialtyId": 2,
    "doctorId": 3
  }'
```

### Expected Response
```json
{
  "success": true,
  "message": "Discount amount calculated successfully",
  "data": {
    "DiscountAmount": 30.00,
    "FinalAmount": 120.00,
    "OriginalAmount": 150.00
  },
  "statusCode": 200
}
```

---

## Error Responses

### Common Error Formats

#### Validation Error (400)
```json
{
  "success": false,
  "message": "Invalid request data",
  "errors": [
    "Code is required",
    "Amount must be greater than 0"
  ],
  "statusCode": 400
}
```

#### Not Found Error (404)
```json
{
  "success": false,
  "message": "Discount with ID 999 not found",
  "statusCode": 404
}
```

#### Business Logic Error (400)
```json
{
  "success": false,
  "message": "Cannot delete discount that has been used",
  "statusCode": 400
}
```

#### Internal Server Error (500)
```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "statusCode": 500
}
```

---

## Testing Scenarios

### 1. Complete Discount Lifecycle Test
1. Create a new discount
2. Validate the discount code
3. Use the discount
4. Check remaining uses
5. Revert the usage
6. Deactivate the discount
7. Delete the discount

### 2. Pagination Test
1. Create multiple discounts
2. Test pagination with different page sizes
3. Test filtering by status, clinic, etc.

### 3. Validation Edge Cases
1. Test with expired discount
2. Test with exceeded usage limit
3. Test with invalid clinic/specialty/doctor combinations
4. Test with insufficient amounts

### 4. Error Handling Test
1. Test with invalid IDs
2. Test with malformed JSON
3. Test with missing required fields
4. Test concurrent usage scenarios

---

## Postman Collection Setup

To create a Postman collection for all these endpoints:

1. **Create New Collection**: "Discount Service API"
2. **Set Collection Variables**:
   - `base_url`: `http://localhost:6007`
   - `discount_id`: `1`
   - `clinic_id`: `1`
3. **Import all the requests** from this guide
4. **Set up Tests** in Postman to validate response status codes and data structure
5. **Create Test Sequences** to run complete workflows

### Example Postman Test Script
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has success field", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('success');
    pm.expect(jsonData.success).to.eql(true);
});

pm.test("Response has data field", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('data');
});
```

---

This comprehensive guide covers all Discount Service API endpoints with practical examples for both Postman and curl testing approaches.
