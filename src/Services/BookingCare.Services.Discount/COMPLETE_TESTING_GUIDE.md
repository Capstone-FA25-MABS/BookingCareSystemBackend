# 🧪 Circuit Breaker Testing Guide - Complete Instructions

## ⚡ **How to Test Circuit Breaker Implementation**

### 🎯 **Testing Objectives**
Verify that the circuit breaker:
1. **Detects failures** and opens when threshold is reached
2. **Fails fast** when open (returns 503 immediately)
3. **Recovers automatically** when service becomes healthy
4. **Provides monitoring** via health endpoints
5. **Supports manual reset** for administrative control

---

## 🚀 **Quick Start Testing**

### **Step 1: Start the Service**
```bash
# Navigate to service directory
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/src/Services/BookingCare.Services.Discount

# Start the service
dotnet run --urls="http://localhost:6007"
```

### **Step 2: Run Automated Tests**
```bash
# Run comprehensive test suite
./test-circuit-breaker.sh

# Or run individual tests
./scripts/check-status.sh
./scripts/test-normal-operations.sh
./scripts/trigger-circuit-breaker.sh
./scripts/reset-circuit-breaker.sh
```

---

## 📋 **Manual Testing Steps**

### **Test 1: Service Health Check** ✅
```bash
# Check if service is responding
curl -X GET http://localhost:6007/api/health

# Expected: HTTP 200 with service info
```

### **Test 2: Circuit Breaker Status** ⚡
```bash
# Check initial circuit breaker state
curl -X GET http://localhost:6007/api/health/circuit-breaker

# Expected Response:
{
  "CircuitBreakers": {
    "Database": { "State": "Closed", "Type": "Database Operations" },
    "HttpClient": { "State": "Closed", "Type": "HTTP Client Operations" },
    "ExternalApi": { "State": "Closed", "Type": "External API Operations" }
  },
  "Timestamp": "2025-08-30T..."
}
```

### **Test 3: Normal Operations** 🟢
```bash
# Create a valid discount
curl -X POST http://localhost:6007/api/discounts \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TEST001",
    "amount": 15,
    "discountType": "FIXED",
    "description": "Test discount",
    "startDate": "2025-08-30T00:00:00.000Z",
    "endDate": "2025-09-30T23:59:59.000Z",
    "maxUses": 100,
    "isActive": true
  }'

# Expected: HTTP 201 (Created) or 200 (OK)
```

### **Test 4: Trigger Circuit Breaker** 🔴
```bash
# Method A: Invalid data (causes validation/database errors)
for i in {1..10}; do
  echo "Request $i:"
  curl -X POST http://localhost:6007/api/discounts \
    -H "Content-Type: application/json" \
    -d '{
      "code": "",
      "amount": -100,
      "discountType": "INVALID",
      "startDate": "invalid-date"
    }'
  echo ""
  sleep 0.5
done

# Method B: Non-existent resource (database errors)
for i in {1..10}; do
  echo "Request $i:"
  curl -w "HTTP %{http_code}\n" -X GET http://localhost:6007/api/discounts/99999
  sleep 0.5
done
```

### **Test 5: Verify Circuit Breaker is Open** 🚫
```bash
# Check circuit breaker status
curl -X GET http://localhost:6007/api/health/circuit-breaker

# Try normal operation (should fail fast)
curl -w "HTTP %{http_code}\n" -X GET http://localhost:6007/api/discounts/1

# Expected: HTTP 503 (Service Unavailable)
```

### **Test 6: Manual Reset** 🔄
```bash
# Reset all circuit breakers
curl -X POST http://localhost:6007/api/health/circuit-breaker/reset

# Reset specific circuit breaker
curl -X POST http://localhost:6007/api/health/circuit-breaker/reset/database

# Verify reset worked
curl -X GET http://localhost:6007/api/health/circuit-breaker
```

### **Test 7: Recovery Verification** ✅
```bash
# After reset, try normal operation
curl -X GET http://localhost:6007/api/discounts

# Expected: Normal response (not 503)
```

---

## 📊 **What to Look For**

### **🟢 Normal Operation (Circuit Closed)**
- **Response Time**: 100-500ms
- **HTTP Status**: 200, 201, 400 (validation errors)
- **Circuit State**: "Closed"
- **Behavior**: All requests processed normally

### **🔴 Circuit Breaker Open**
- **Response Time**: 1-10ms (fail fast)
- **HTTP Status**: 503 (Service Unavailable)
- **Circuit State**: "Open"
- **Behavior**: Immediate failures, no database calls

### **🟡 Circuit Breaker Half-Open**
- **Response Time**: Variable
- **HTTP Status**: Mixed responses
- **Circuit State**: "HalfOpen"
- **Behavior**: Limited requests allowed for testing

---

## 🔍 **Configuration Details**

### **Database Circuit Breaker**
- **Failure Threshold**: 50% (5 out of 10 requests)
- **Break Duration**: 30 seconds
- **Minimum Throughput**: 5 requests
- **Timeout**: 30 seconds
- **Retry Count**: 3 attempts

### **HTTP Client Circuit Breaker**
- **Failure Threshold**: 30% (3 out of 10 requests)
- **Break Duration**: 15 seconds
- **Minimum Throughput**: 3 requests
- **Timeout**: 10 seconds
- **Retry Count**: 2 attempts

---

## 📝 **Log Messages to Monitor**

Watch for these key log entries:

### **Circuit Breaker Events**
```
Circuit breaker closed
Circuit breaker opened
Circuit breaker half-opened
```

### **Operation Events**
```
Executing operation CreateDiscount with circuit breaker
Operation CreateDiscount completed successfully
Operation CreateDiscount timed out
```

### **Retry Events**
```
Retry attempt 1 for operation
Retry attempt 2 for operation
```

### **Error Events**
```
Circuit breaker is open for operation CreateDiscount
Service temporarily unavailable: CreateDiscount
```

---

## 🧪 **Advanced Testing Scenarios**

### **Load Testing**
```bash
# Create multiple concurrent requests
for i in {1..20}; do
  (
    curl -X POST http://localhost:6007/api/discounts \
      -H "Content-Type: application/json" \
      -d "{
        \"code\": \"LOAD$i\",
        \"amount\": $((10 + i)),
        \"discountType\": \"FIXED\",
        \"description\": \"Load test $i\",
        \"startDate\": \"$(date -u +%Y-%m-%dT%H:%M:%S.000Z)\",
        \"endDate\": \"$(date -u -v+30d +%Y-%m-%dT%H:%M:%S.000Z)\",
        \"maxUses\": 100,
        \"isActive\": true
      }"
  ) &
done
wait
```

### **Database Connection Testing**
```bash
# If using Docker, stop database
docker stop bookingcare-discount-db

# Make requests (should trigger circuit breaker)
for i in {1..5}; do
  curl -w "HTTP %{http_code}\n" -X GET http://localhost:6007/api/discounts
  sleep 1
done

# Restart database
docker start bookingcare-discount-db

# Wait for recovery and test
sleep 10
curl -X GET http://localhost:6007/api/discounts
```

### **Continuous Monitoring**
```bash
# Monitor circuit breaker status continuously
watch -n 2 'curl -s http://localhost:6007/api/health/circuit-breaker | jq'

# Monitor with timestamps
while true; do
  echo "$(date): $(curl -s http://localhost:6007/api/health/circuit-breaker | jq -r '.CircuitBreakers.Database.State')"
  sleep 5
done
```

---

## ✅ **Success Indicators**

### **Circuit Breaker Working Correctly**
1. ✅ **Health endpoints respond** with circuit breaker status
2. ✅ **Normal operations work** when circuit is closed
3. ✅ **Failures trigger circuit opening** after threshold
4. ✅ **Fast failure responses** when circuit is open (503 status)
5. ✅ **Manual reset functionality** works
6. ✅ **Automatic recovery** after break duration
7. ✅ **Proper logging** of circuit breaker events

### **Performance Benefits**
- **Reduced latency** during failures (fail fast)
- **Resource protection** (no wasted database calls)
- **Graceful degradation** (proper error responses)
- **Monitoring visibility** (status endpoints)

---

## 🚨 **Troubleshooting**

### **Circuit Breaker Not Triggering**
- Increase number of failure requests
- Check configuration in `appsettings.json`
- Verify minimum throughput settings

### **Service Not Responding**
- Check if port 6007 is available
- Verify service is running (`dotnet run`)
- Check firewall settings

### **Database Issues**
- Ensure SQL Server is running
- Check connection string configuration
- Verify database credentials

### **Configuration Issues**
- Check Polly package versions
- Verify dependency injection setup
- Review configuration binding

---

## 📈 **Expected Results**

When testing is successful, you should see:

1. **Initial State**: All circuit breakers "Closed"
2. **Failure Detection**: Circuit opens after 5 failures
3. **Fast Failure**: 503 responses in ~1-10ms
4. **Recovery**: Circuit closes after successful requests
5. **Manual Control**: Reset functionality works
6. **Monitoring**: Real-time status visibility

This comprehensive testing approach validates that your circuit breaker implementation provides the expected resilience and fault tolerance benefits for your microservice architecture.
