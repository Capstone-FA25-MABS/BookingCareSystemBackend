#!/bin/bash

# EventBus Integration Test Script
# This script tests the complete integration between EventBus Common library and RabbitMQ container

set -e

echo "🚀 Starting EventBus Integration Tests..."
echo "============================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DOCKER_COMPOSE_FILE="docker-compose.yml"
TEST_SERVICE_URL="http://localhost:5099"
RABBITMQ_MANAGEMENT_URL="http://localhost:15672"

echo -e "${BLUE}📋 Test Configuration:${NC}"
echo "   - Docker Compose File: $DOCKER_COMPOSE_FILE"
echo "   - Test Service URL: $TEST_SERVICE_URL"
echo "   - RabbitMQ Management: $RABBITMQ_MANAGEMENT_URL"
echo ""

# Function to check if service is ready
check_service() {
    local url=$1
    local service_name=$2
    local max_attempts=30
    local attempt=0

    echo -e "${YELLOW}⏳ Waiting for $service_name to be ready...${NC}"
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -s "$url" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ $service_name is ready!${NC}"
            return 0
        fi
        
        attempt=$((attempt + 1))
        echo "   Attempt $attempt/$max_attempts - waiting 5 seconds..."
        sleep 5
    done
    
    echo -e "${RED}❌ $service_name failed to start within expected time${NC}"
    return 1
}

# Function to test API endpoint
test_api() {
    local endpoint=$1
    local method=$2
    local data=$3
    local description=$4
    
    echo -e "${BLUE}🧪 Testing: $description${NC}"
    echo "   Endpoint: $method $endpoint"
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" "$endpoint")
    else
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X "$method" \
                   -H "Content-Type: application/json" \
                   -d "$data" "$endpoint")
    fi
    
    # Extract status code
    status_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    response_body=$(echo "$response" | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$status_code" -eq 200 ]; then
        echo -e "${GREEN}   ✅ Success (HTTP $status_code)${NC}"
        if [ ! -z "$response_body" ]; then
            echo "   Response: $response_body" | head -c 200
            if [ ${#response_body} -gt 200 ]; then
                echo "..."
            fi
            echo ""
        fi
        return 0
    else
        echo -e "${RED}   ❌ Failed (HTTP $status_code)${NC}"
        echo "   Response: $response_body"
        return 1
    fi
}

# Main test execution
main() {
    echo -e "${BLUE}🐳 Step 1: Starting Docker services...${NC}"
    docker-compose -f "$DOCKER_COMPOSE_FILE" up -d rabbitmq eventbus-test-service
    
    echo ""
    echo -e "${BLUE}🔍 Step 2: Checking service health...${NC}"
    
    # Check RabbitMQ Management
    if ! check_service "$RABBITMQ_MANAGEMENT_URL" "RabbitMQ Management"; then
        exit 1
    fi
    
    # Check Test Service
    if ! check_service "$TEST_SERVICE_URL/health" "EventBus Test Service"; then
        exit 1
    fi
    
    echo ""
    echo -e "${BLUE}📊 Step 3: Running integration tests...${NC}"
    
    # Test 1: Service Status
    if ! test_api "$TEST_SERVICE_URL/api/EventTest/status" "GET" "" "Service Status Check"; then
        exit 1
    fi
    
    echo ""
    
    # Test 2: User Registration Event
    user_data='{
        "email": "test.user@example.com",
        "role": "Patient",
        "fullName": "Integration Test User",
        "phoneNumber": "+1-555-0123"
    }'
    
    if ! test_api "$TEST_SERVICE_URL/api/EventTest/publish/user-registered" "POST" "$user_data" "User Registration Event"; then
        exit 1
    fi
    
    echo ""
    
    # Test 3: Appointment Creation Event
    appointment_data='{
        "appointmentDateTime": "2024-12-25T10:00:00Z",
        "duration": 45,
        "appointmentType": "Consultation"
    }'
    
    if ! test_api "$TEST_SERVICE_URL/api/EventTest/publish/appointment-created" "POST" "$appointment_data" "Appointment Creation Event"; then
        exit 1
    fi
    
    echo ""
    
    # Test 4: Payment Processing Event
    payment_data='{
        "amount": 150.75,
        "currency": "USD",
        "paymentMethod": "Credit Card",
        "status": "Completed"
    }'
    
    if ! test_api "$TEST_SERVICE_URL/api/EventTest/publish/payment-processed" "POST" "$payment_data" "Payment Processing Event"; then
        exit 1
    fi
    
    echo ""
    
    # Test 5: Notification Event with Routing Key
    notification_data='{
        "type": "Email",
        "message": "Your appointment has been confirmed",
        "subject": "Appointment Confirmation",
        "priority": "High",
        "routingKey": "notification.email"
    }'
    
    if ! test_api "$TEST_SERVICE_URL/api/EventTest/publish/notification" "POST" "$notification_data" "Notification Event (with routing key)"; then
        exit 1
    fi
    
    echo ""
    echo -e "${BLUE}📋 Step 4: Container logs verification...${NC}"
    echo "EventBus Test Service logs (last 20 lines):"
    echo "----------------------------------------"
    docker-compose -f "$DOCKER_COMPOSE_FILE" logs --tail=20 eventbus-test-service
    
    echo ""
    echo -e "${BLUE}🌐 Step 5: RabbitMQ Management Interface${NC}"
    echo "   You can access RabbitMQ Management at: $RABBITMQ_MANAGEMENT_URL"
    echo "   Username: bookingcare"
    echo "   Password: bookingcare@1234"
    echo "   Check the 'Queues' tab to see eventbus-test-service-queue"
    
    echo ""
    echo -e "${GREEN}🎉 All integration tests completed successfully!${NC}"
    echo -e "${GREEN}✅ EventBus Common library is properly integrated with RabbitMQ container${NC}"
    echo ""
    echo -e "${YELLOW}🔧 Next steps:${NC}"
    echo "   1. Check logs: docker-compose logs -f eventbus-test-service"
    echo "   2. Test Swagger UI: $TEST_SERVICE_URL/swagger"
    echo "   3. Monitor RabbitMQ: $RABBITMQ_MANAGEMENT_URL"
    echo "   4. Stop services: docker-compose down"
    echo ""
}

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}🧹 Cleaning up...${NC}"
    read -p "Do you want to stop the services? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker-compose -f "$DOCKER_COMPOSE_FILE" down
        echo -e "${GREEN}✅ Services stopped${NC}"
    else
        echo -e "${BLUE}ℹ️  Services are still running${NC}"
        echo "   Use 'docker-compose down' to stop them manually"
    fi
}

# Trap cleanup on script exit
trap cleanup EXIT

# Run main function
main
