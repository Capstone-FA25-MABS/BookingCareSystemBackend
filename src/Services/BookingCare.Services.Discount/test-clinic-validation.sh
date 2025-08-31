#!/bin/bash

echo "Testing Clinic Validation via gRPC Integration"
echo "=============================================="

# Test data for creating a discount
DISCOUNT_DATA='{
  "code": "TESTCLINIC50",
  "name": "Test Clinic Discount",
  "description": "Test discount for clinic validation",
  "clinicId": 1,
  "amount": 10.0,
  "discountType": "PERCENTAGE",
  "startDate": "2025-09-01T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z",
  "maxUses": 100,
  "applicableTo": "ALL",
  "status": "ACTIVE"
}'

echo "1. Testing discount creation with valid clinic ID (1)..."
echo "Request payload:"
echo "$DISCOUNT_DATA" | jq .

echo -e "\nSending request to Discount service..."
RESPONSE=$(curl -s -X POST "http://localhost:6007/api/discounts" \
  -H "Content-Type: application/json" \
  -d "$DISCOUNT_DATA")

echo "Response:"
echo "$RESPONSE" | jq .

echo -e "\n2. Testing discount creation with invalid clinic ID (999)..."
INVALID_DISCOUNT_DATA='{
  "code": "TESTINVALID50",
  "name": "Test Invalid Clinic Discount",
  "description": "Test discount with invalid clinic",
  "clinicId": 999,
  "amount": 10.0,
  "discountType": "PERCENTAGE",
  "startDate": "2025-09-01T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z",
  "maxUses": 100,
  "applicableTo": "ALL",
  "status": "ACTIVE"
}'

echo "Request payload:"
echo "$INVALID_DISCOUNT_DATA" | jq .

echo -e "\nSending request to Discount service..."
INVALID_RESPONSE=$(curl -s -X POST "http://localhost:6007/api/discounts" \
  -H "Content-Type: application/json" \
  -d "$INVALID_DISCOUNT_DATA")

echo "Response:"
echo "$INVALID_RESPONSE" | jq .

echo -e "\nTest completed!"
