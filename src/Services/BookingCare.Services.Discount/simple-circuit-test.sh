#!/bin/bash

# Simple Circuit Breaker Test Script
# This works even with database configuration issues

echo "🔧 Simple Circuit Breaker Test"
echo "============================="

BASE_URL="http://localhost:6007"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Function to test endpoint
test_endpoint() {
    local method=$1
    local url=$2
    local description=$3
    
    echo -e "${BLUE}Testing: $description${NC}"
    
    response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X "$method" "$url" --connect-timeout 10)
    http_code=$(echo $response | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    content=$(echo $response | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" -eq 200 ]; then
        echo -e "${GREEN}✅ SUCCESS (HTTP $http_code)${NC}"
        echo "$content" | head -c 200
        if [ ${#content} -gt 200 ]; then echo "..."; fi
    elif [ "$http_code" -eq 503 ]; then
        echo -e "${YELLOW}⚠️  SERVICE UNAVAILABLE (HTTP $http_code) - Circuit Breaker Active!${NC}"
        echo "$content"
    else
        echo -e "${RED}❌ FAILED (HTTP $http_code)${NC}"
        echo "$content" | head -c 200
    fi
    echo ""
}

# Test 1: Basic connectivity
echo -e "${BLUE}=== Test 1: Basic Connectivity ===${NC}"
test_endpoint "GET" "$BASE_URL/api/health" "Service Health Check"

# Test 2: Circuit breaker status
echo -e "${BLUE}=== Test 2: Circuit Breaker Status ===${NC}"
test_endpoint "GET" "$BASE_URL/api/health/circuit-breaker" "Circuit Breaker Status"

# Test 3: Generate some load to test circuit breaker
echo -e "${BLUE}=== Test 3: Load Testing (May Trigger Circuit Breaker) ===${NC}"
echo "Making multiple requests to potentially trigger circuit breaker..."

for i in {1..8}; do
    echo -e "${YELLOW}Request $i:${NC}"
    
    # Try different endpoints that might cause database errors
    response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X GET "$BASE_URL/api/discounts/999")
    http_code=$(echo $response | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    
    if [ "$http_code" -eq 503 ]; then
        echo -e "${YELLOW}⚠️  503 - Circuit Breaker Triggered!${NC}"
        break
    elif [ "$http_code" -eq 404 ]; then
        echo -e "${GREEN}✅ 404 - Normal response${NC}"
    elif [ "$http_code" -eq 500 ]; then
        echo -e "${RED}❌ 500 - Server error (may trigger circuit breaker)${NC}"
    else
        echo -e "${BLUE}ℹ️  HTTP $http_code${NC}"
    fi
    
    sleep 0.5
done

echo ""

# Test 4: Check circuit breaker status after load
echo -e "${BLUE}=== Test 4: Circuit Breaker Status After Load ===${NC}"
test_endpoint "GET" "$BASE_URL/api/health/circuit-breaker" "Circuit Breaker Status After Load"

# Test 5: Manual reset
echo -e "${BLUE}=== Test 5: Manual Reset Test ===${NC}"
test_endpoint "POST" "$BASE_URL/api/health/circuit-breaker/reset" "Manual Circuit Breaker Reset"

# Test 6: Status after reset
echo -e "${BLUE}=== Test 6: Status After Reset ===${NC}"
test_endpoint "GET" "$BASE_URL/api/health/circuit-breaker" "Circuit Breaker Status After Reset"

echo -e "${GREEN}🎉 Circuit Breaker Testing Complete!${NC}"
echo ""
echo "What to look for:"
echo "- ✅ Health endpoints should respond (200)"
echo "- ⚡ Circuit breaker status should show states"
echo "- 🔴 After multiple failures, you should see 503 responses"
echo "- 🔄 Manual reset should work"
echo "- 📊 Status should show 'Closed' after reset"
