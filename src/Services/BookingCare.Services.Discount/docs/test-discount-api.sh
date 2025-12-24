#!/bin/bash

# Discount Service API Testing Script
# This script provides easy-to-use curl commands for testing all Discount Service endpoints

# Configuration
BASE_URL="http://localhost:6007"
API_BASE="${BASE_URL}/api/discounts"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_header() {
    echo -e "${BLUE}=== $1 ===${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Function to make requests and display results
make_request() {
    local method=$1
    local url=$2
    local data=$3
    local description=$4
    
    print_header "$description"
    echo "Request: $method $url"
    
    if [ -n "$data" ]; then
        echo "Data: $data"
        response=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -d "$data")
    else
        response=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X "$method" "$url" \
            -H "Content-Type: application/json")
    fi
    
    http_code=$(echo "$response" | grep "HTTP_CODE:" | cut -d: -f2)
    body=$(echo "$response" | sed '/HTTP_CODE:/d')
    
    echo "HTTP Status: $http_code"
    echo "Response:"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
    echo
}

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    print_info "jq is not installed. Response formatting will be basic."
    print_info "Install jq for better JSON formatting: brew install jq"
    echo
fi

# Check if service is running
print_header "Checking Service Health"
health_response=$(curl -s -w "%{http_code}" "${API_BASE}/health" -o /dev/null)
if [ "$health_response" == "200" ]; then
    print_success "Discount Service is running"
else
    print_error "Discount Service is not responding (HTTP: $health_response)"
    print_info "Make sure the service is running on $BASE_URL"
    exit 1
fi
echo

# 1. Health Check
make_request "GET" "${API_BASE}/health" "" "Health Check"

# 2. Get Discounts with Filtering
make_request "GET" "${API_BASE}?clinicId=1&status=ACTIVE&pageNumber=1&pageSize=10" "" "Get Discounts with Filtering"

# 3. Create a Test Discount
test_discount_data='{
  "code": "APITEST2025",
  "name": "API Test Discount",
  "description": "Test discount created via API testing script",
  "clinicId": 1,
  "specialtyId": null,
  "doctorId": null,
  "applicableTo": "ALL",
  "amount": 25.00,
  "discountType": "PERCENTAGE",
  "startDate": "2025-08-29T00:00:00",
  "endDate": "2025-12-31T23:59:59",
  "maxUses": 50,
  "status": "ACTIVE"
}'

create_response=$(curl -s -X POST "${API_BASE}" \
    -H "Content-Type: application/json" \
    -d "$test_discount_data")

make_request "POST" "${API_BASE}" "$test_discount_data" "Create Test Discount"

# Extract created discount ID and code
created_id=$(echo "$create_response" | jq -r '.data.id // empty' 2>/dev/null)
created_code=$(echo "$create_response" | jq -r '.data.code // empty' 2>/dev/null)

if [ -n "$created_id" ] && [ "$created_id" != "null" ]; then
    print_success "Created discount with ID: $created_id, Code: $created_code"
    
    # 4. Get Discount by ID
    make_request "GET" "${API_BASE}/${created_id}" "" "Get Discount by ID"
    
    # 5. Get Discount by Code
    make_request "GET" "${API_BASE}/by-code/${created_code}" "" "Get Discount by Code"
    
    # 6. Update Discount
    update_data="{
      \"id\": $created_id,
      \"name\": \"Updated API Test Discount\",
      \"description\": \"Updated test discount via API testing script\",
      \"amount\": 30.00,
      \"maxUses\": 75
    }"
    make_request "PUT" "${API_BASE}/${created_id}" "$update_data" "Update Discount"
    
    # 7. Validate Discount
    validate_data="{
      \"code\": \"$created_code\",
      \"clinicId\": 1,
      \"specialtyId\": 2,
      \"doctorId\": 3,
      \"totalAmount\": 100.00
    }"
    make_request "POST" "${API_BASE}/validate" "$validate_data" "Validate Discount"
    
    # 8. Use Discount
    use_data="{
      \"code\": \"$created_code\",
      \"clinicId\": 1,
      \"specialtyId\": 2,
      \"doctorId\": 3,
      \"totalAmount\": 100.00
    }"
    make_request "POST" "${API_BASE}/use" "$use_data" "Use Discount"
    
    # 9. Calculate Discount Amount
    calculate_data="{
      \"code\": \"$created_code\",
      \"originalAmount\": 150.00,
      \"clinicId\": 1,
      \"specialtyId\": 2,
      \"doctorId\": 3
    }"
    make_request "POST" "${API_BASE}/calculate" "$calculate_data" "Calculate Discount Amount"
    
    # 10. Revert Discount Usage
    revert_data="{
      \"code\": \"$created_code\",
      \"clinicId\": 1
    }"
    make_request "POST" "${API_BASE}/revert" "$revert_data" "Revert Discount Usage"
    
    # 11. Deactivate Discount
    make_request "PATCH" "${API_BASE}/${created_id}/deactivate" "" "Deactivate Discount"
    
    # 12. Activate Discount
    make_request "PATCH" "${API_BASE}/${created_id}/activate" "" "Activate Discount"
    
    # 13. Get Active Discounts by Clinic
    make_request "GET" "${API_BASE}/clinic/1/active" "" "Get Active Discounts by Clinic"
    
    # 14. Get Applicable Discounts
    make_request "GET" "${API_BASE}/applicable?clinicId=1&specialtyId=2&doctorId=3" "" "Get Applicable Discounts"
    
    # 15. Update Expired Discounts (Admin operation)
    make_request "POST" "${API_BASE}/update-expired" "" "Update Expired Discounts"
    
    # 16. Delete Test Discount (cleanup)
    print_info "Cleaning up: Deleting test discount..."
    make_request "DELETE" "${API_BASE}/${created_id}" "" "Delete Test Discount"
    
else
    print_error "Failed to create test discount. Cannot proceed with ID-dependent tests."
    print_info "You can still test the following endpoints manually:"
    echo "  - Get Discount by ID: GET ${API_BASE}/{id}"
    echo "  - Get Discount by Code: GET ${API_BASE}/by-code/{code}"
    echo "  - Update Discount: PUT ${API_BASE}/{id}"
    echo "  - Delete Discount: DELETE ${API_BASE}/{id}"
fi

# Additional tests that don't require a specific discount
print_header "Testing Error Cases"

# Test with invalid ID
make_request "GET" "${API_BASE}/99999" "" "Get Non-existent Discount (Should return 404)"

# Test with invalid code
make_request "GET" "${API_BASE}/by-code/NONEXISTENT" "" "Get Non-existent Discount by Code (Should return 404)"

# Test validation with invalid data
invalid_validate_data='{
  "code": "",
  "clinicId": 0,
  "totalAmount": -1
}'
make_request "POST" "${API_BASE}/validate" "$invalid_validate_data" "Validate with Invalid Data (Should return 400)"

print_header "Testing Complete"
print_success "All API endpoints have been tested!"
print_info "Check the responses above for any errors or unexpected behavior."
echo
print_info "For more detailed testing:"
print_info "  1. Import the Postman collection: docs/Discount_Service_API.postman_collection.json"
print_info "  2. Read the full API guide: docs/API_TESTING_GUIDE.md"
print_info "  3. Customize the test data in this script as needed"
