# Circuit Breaker Testing Guide

## 🧪 How to Test Circuit Breaker Implementation

### Prerequisites
1. **Start the Discount Service**:
   ```bash
   cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/src/Services/BookingCare.Services.Discount
   dotnet run
   ```

2. **Ensure Database is Running** (if using Docker):
   ```bash
   docker-compose up -d
   ```

---

## 🔍 **Testing Methods**

### **Method 1: Automated Test Script**

Run the comprehensive test script:
```bash
./test-circuit-breaker.sh
```

This will automatically test:
- ✅ Service health check
- ✅ Initial circuit breaker state
- ✅ Normal operations
- ✅ Circuit breaker triggering
- ✅ Service unavailable responses
- ✅ Manual reset functionality
- ✅ Recovery operations

---

### **Method 2: Manual cURL Commands**

#### **Step 1: Check Service Health**
```bash
# Basic health check
curl -X GET http://localhost:6007/api/health

# Circuit breaker status
curl -X GET http://localhost:6007/api/health/circuit-breaker
```

**Expected Response:**
```json
{
  "CircuitBreakers": {
    "Database": { "State": "Closed", "Type": "Database Operations" },
    "HttpClient": { "State": "Closed", "Type": "HTTP Client Operations" },
    "ExternalApi": { "State": "Closed", "Type": "External API Operations" }
  },
  "Timestamp": "2025-08-30T..."
}
```

#### **Step 2: Test Normal Operations**
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
```

**Expected**: HTTP 201 Created with discount details

#### **Step 3: Trigger Circuit Breaker (Simulate Failures)**

**Option A: Database Connection Issues**
```bash
# Stop database container
docker stop bookingcare-discount-db

# Make multiple requests (should trigger circuit breaker)
for i in {1..8}; do
  curl -X GET http://localhost:6007/api/discounts/1
  sleep 0.5
done
```

**Option B: Invalid Data (Validation Errors)**
```bash
# Send invalid requests to cause failures
for i in {1..8}; do
  curl -X POST http://localhost:6007/api/discounts \
    -H "Content-Type: application/json" \
    -d '{
      "code": "",
      "amount": -100,
      "discountType": "INVALID"
    }'
  sleep 0.5
done
```

#### **Step 4: Verify Circuit Breaker is Open**
```bash
# Check circuit breaker status
curl -X GET http://localhost:6007/api/health/circuit-breaker

# Try normal operation (should get 503)
curl -X GET http://localhost:6007/api/discounts/1
```

**Expected**: HTTP 503 Service Unavailable

#### **Step 5: Test Manual Reset**
```bash
# Reset all circuit breakers
curl -X POST http://localhost:6007/api/health/circuit-breaker/reset

# Reset specific circuit breaker
curl -X POST http://localhost:6007/api/health/circuit-breaker/reset/database
```

#### **Step 6: Verify Recovery**
```bash
# Restart database (if stopped)
docker start bookingcare-discount-db

# Wait a moment then test normal operation
sleep 5
curl -X GET http://localhost:6007/api/discounts
```

---

### **Method 3: Load Testing with Multiple Concurrent Requests**

```bash
# Create load_test.sh
#!/bin/bash
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

---

## 🔍 **What to Look For**

### **1. Circuit Breaker States**
- **🟢 Closed**: Normal operation, all requests pass through
- **🔴 Open**: Failing fast, requests immediately return 503
- **🟡 Half-Open**: Testing recovery, limited requests allowed

### **2. HTTP Status Codes**
- **200/201**: Normal operation
- **503**: Service Unavailable (circuit breaker open)
- **408**: Request Timeout
- **400**: Bad Request (validation errors)

### **3. Log Messages**
Watch for these log entries:
```
Circuit breaker closed
Circuit breaker opened  
Circuit breaker half-opened
Retry attempt 1 for operation
Operation CreateDiscount completed successfully
Operation CreateDiscount timed out
```

### **4. Performance Behavior**
- **Normal**: Quick responses (~100-500ms)
- **Circuit Open**: Immediate failures (~1-10ms)
- **Recovery**: Gradual improvement in response times

---

## 📊 **Monitoring & Debugging**

### **Check Application Logs**
```bash
# If running with dotnet run
# Logs appear in console

# If running with Docker
docker logs bookingcare-discount-service
```

### **Monitor Circuit Breaker Metrics**
```bash
# Continuous monitoring
watch -n 2 'curl -s http://localhost:6007/api/health/circuit-breaker | jq'
```

### **Database Connection Testing**
```bash
# Test database connectivity
docker exec -it bookingcare-discount-db sqlcmd -S localhost -U sa -P 'Discount123!'
```

---

## 🎯 **Expected Test Results**

### **Successful Circuit Breaker Implementation Should Show:**

1. **Normal State**: All circuit breakers in "Closed" state
2. **Failure Detection**: Circuit opens after configured failure threshold (5 failures)
3. **Fast Failure**: Immediate 503 responses when circuit is open
4. **Timeout Protection**: Operations don't hang indefinitely
5. **Recovery**: Circuit closes after successful operations
6. **Manual Control**: Reset functionality works
7. **Proper Logging**: Detailed event tracking

### **Configuration Verification**
Current settings from `appsettings.json`:
- **Database Circuit Breaker**: 5 failures, 30s break, 50% threshold
- **HTTP Client Circuit Breaker**: 3 failures, 15s break, 30% threshold
- **External API Circuit Breaker**: 3 failures, 20s break, 40% threshold

---

## 🚨 **Troubleshooting**

### **Circuit Breaker Not Triggering**
- Increase failure rate by making more invalid requests
- Check configuration settings in `appsettings.json`
- Verify minimum throughput requirements

### **Service Not Responding**
- Check if service is running on port 6007
- Verify database connection
- Check firewall settings

### **Database Issues**
- Ensure SQL Server container is running
- Check connection string in `appsettings.json`
- Verify database credentials

---

This comprehensive testing approach will validate that your circuit breaker implementation is working correctly and providing the expected resilience benefits.
