# Discount Service Documentation

This directory contains comprehensive documentation and testing resources for the BookingCare Discount Service.

## 📋 Contents

### 📖 Documentation Files

- **[API_TESTING_GUIDE.md](./API_TESTING_GUIDE.md)** - Complete API testing guide with examples for all endpoints
- **[DOCKER_DEPLOYMENT_GUIDE.md](./DOCKER_DEPLOYMENT_GUIDE.md)** - Comprehensive Docker deployment instructions
- **[README.md](./README.md)** - This file

### 🔧 Testing Tools

- **[test-discount-api.sh](./test-discount-api.sh)** - Bash script for automated API testing with curl
- **[Discount_Service_API.postman_collection.json](./Discount_Service_API.postman_collection.json)** - Postman collection for API testing

### 🐳 Docker Tools

- **[docker-build.sh](../docker-build.sh)** - Docker build and deployment automation script
- **[Dockerfile](../Dockerfile)** - Multi-stage Docker build configuration
- **[docker-compose.yml](../docker-compose.yml)** - Development deployment with database
- **[docker-compose.prod.yml](../docker-compose.prod.yml)** - Production deployment overrides
- **[.env.template](../.env.template)** - Environment variables template

## 🚀 Quick Start

### Prerequisites

1. **Discount Service Running**: Ensure the service is running on `http://localhost:6007`
2. **curl**: For command-line testing
3. **jq** (optional): For JSON formatting (`brew install jq` on macOS)
4. **Postman** (optional): For GUI-based testing
5. **Docker** (optional): For containerized deployment

### Option 1: Test Against Running Service

If you already have the Discount Service running (either locally or in Docker):

```bash
# Navigate to the docs directory
cd src/Services/BookingCare.Services.Discount/docs

# Run the comprehensive test script
./test-discount-api.sh
```

### Option 2: Deploy and Test with Docker

```bash
# Navigate to the service directory
cd src/Services/BookingCare.Services.Discount

# Deploy with Docker Compose (includes database)
./docker-build.sh latest compose dev

# Wait for services to be ready (about 60 seconds)
sleep 60

# Run API tests
cd docs
./test-discount-api.sh
```

### Option 3: Manual Testing with curl

```bash
# Navigate to the docs directory
cd src/Services/BookingCare.Services.Discount/docs

# Run the comprehensive test script
./test-discount-api.sh
```

### Option 3: Manual Testing with curl

Use the examples in `API_TESTING_GUIDE.md` for manual testing:

```bash
# Health check
curl -X GET "http://localhost:6007/api/discounts/health"

# Get all discounts
curl -X GET "http://localhost:6007/api/discounts?clinicId=1&status=ACTIVE"

# Create a discount
curl -X POST "http://localhost:6007/api/discounts" \
  -H "Content-Type: application/json" \
  -d '{"code": "TEST2025", "name": "Test Discount", ...}'
```

### Option 4: Postman Collection

1. **Import Collection**:
   - Open Postman
   - Click "Import"
   - Select `Discount_Service_API.postman_collection.json`

2. **Set Variables**:
   - `base_url`: `http://localhost:6007`
   - `clinic_id`: `1`
   - `discount_id`: `1`

3. **Run Tests**:
   - Individual requests or entire collection
   - Built-in test scripts validate responses

## 📊 API Endpoints Overview

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `GET` | `/api/discounts/health` | Service health check |
| `GET` | `/api/discounts/{id}` | Get discount by ID |
| `GET` | `/api/discounts/by-code/{code}` | Get discount by code |
| `GET` | `/api/discounts` | Get discounts with filtering |
| `GET` | `/api/discounts/clinic/{clinicId}/active` | Get active discounts for clinic |
| `GET` | `/api/discounts/applicable` | Get applicable discounts |
| `POST` | `/api/discounts` | Create new discount |
| `PUT` | `/api/discounts/{id}` | Update existing discount |
| `DELETE` | `/api/discounts/{id}` | Delete discount |
| `POST` | `/api/discounts/validate` | Validate discount code |
| `POST` | `/api/discounts/use` | Use discount code |
| `POST` | `/api/discounts/revert` | Revert discount usage |
| `PATCH` | `/api/discounts/{id}/activate` | Activate discount |
| `PATCH` | `/api/discounts/{id}/deactivate` | Deactivate discount |
| `POST` | `/api/discounts/update-expired` | Update expired discounts |
| `POST` | `/api/discounts/calculate` | Calculate discount amount |

## 🧪 Testing Scenarios

### 1. **Complete Workflow Test**
- Create → Validate → Use → Revert → Deactivate → Delete

### 2. **Business Logic Testing**
- ✅ Valid discount codes
- ❌ Expired discounts
- ❌ Exceeded usage limits
- ❌ Invalid clinic/specialty combinations

### 3. **Error Handling**
- 400: Bad Request (validation errors)
- 404: Not Found (invalid IDs/codes)
- 500: Internal Server Error

### 4. **Pagination & Filtering**
- Different page sizes
- Status filtering
- Search terms
- Date ranges

## 📝 Sample Data

### Valid Discount Creation
```json
{
  "code": "SUMMER2025",
  "name": "Summer Discount",
  "description": "20% off all appointments",
  "clinicId": 1,
  "applicableTo": "ALL",
  "amount": 20.00,
  "discountType": "PERCENTAGE",
  "startDate": "2025-06-01T00:00:00",
  "endDate": "2025-08-31T23:59:59",
  "maxUses": 100,
  "status": "ACTIVE"
}
```

### Discount Validation Request
```json
{
  "code": "SUMMER2025",
  "clinicId": 1,
  "specialtyId": 2,
  "doctorId": 3,
  "totalAmount": 100.00
}
```

## 🔧 Troubleshooting

### Service Not Responding
```bash
# Check if service is running
curl -s -o /dev/null -w "%{http_code}" http://localhost:6007/api/discounts/health

# If not 200, start the service:
cd src/Services/BookingCare.Services.Discount
dotnet run
```

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database migrations are applied

### Port Conflicts
- HTTP API: Port 6007
- gRPC: Port 6017
- Change ports in `Program.cs` if needed

## 📞 Support

For questions or issues:
1. Check the [API Testing Guide](./API_TESTING_GUIDE.md) for detailed examples
2. Run the test script for automated validation
3. Review service logs for error details
4. Verify database connectivity and data

---

**Happy Testing! 🎉**
