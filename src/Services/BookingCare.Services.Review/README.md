# BookingCare Review Service - Developer Guide

## 📖 Tổng quan

BookingCare Review Service là microservice chuyên biệt quản lý đánh giá và phản hồi trong hệ thống BookingCare. Service này cung cấp cả REST API và gRPC endpoints để các service khác có thể tương tác hiệu quả.

## 🚀 Thông tin Service

| Thông tin | Giá trị |
|-----------|---------|
| **Service Name** | BookingCare.Services.Review |
| **Technology** | .NET 8, ASP.NET Core, MongoDB |
| **REST API Port** | 6012 (HTTP/1.1 + HTTP/2) |
| **gRPC Port** | 6022 (HTTP/2 only) |
| **Database** | MongoDB |
| **Protocol** | REST API + gRPC |

## 🏗️ Kiến trúc

```
BookingCare.Services.Review/
├── Controllers/                    # REST API Controllers
├── Grpc/Services/                 # gRPC Service Implementation
├── Models/
│   ├── DTOs/                      # Data Transfer Objects
│   ├── Entities/                  # MongoDB Entities
│   └── Enums/                     # Enumerations
├── Services/                      # Business Logic
├── Repositories/                  # Data Access Layer
├── Protos/                        # gRPC Proto Definitions
└── Validators/                    # Request Validation
```

## 📊 Core Features

### ✅ Business Rules
- **One Review Per Target**: Mỗi patient chỉ được review 1 lần cho mỗi doctor/service
- **Rating Scale**: Đánh giá từ 1-5 sao
- **Reply System**: Doctor/Admin có thể phản hồi reviews
- **Comprehensive Statistics**: Thống kê chi tiết với rating distribution

### 🎯 Target Types
- **DOCTOR**: Đánh giá bác sĩ
- **SERVICE**: Đánh giá dịch vụ phòng khám

---

# 🌐 REST API Documentation

## Base URL
```
Development: http://localhost:6012/api/reviews
Production: https://api.bookingcare.com/reviews
```

## 🔧 Authentication
```http
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

## 📋 API Endpoints

### 1. Health Check
```http
GET /api/reviews/health
```

**Response:**
```json
{
  "status": "Healthy",
  "service": "Review",
  "timestamp": "2025-01-01T10:00:00Z"
}
```

---

### 2. Create Review
```http
POST /api/reviews
```

**Request Body:**
```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440001",
  "targetType": "DOCTOR",
  "doctorId": "550e8400-e29b-41d4-a716-446655440003",
  "clinicServiceId": null,
  "rating": 5,
  "comment": "Excellent doctor, very professional!"
}
```

**Success Response (201 Created):**
```json
{
  "success": true,
  "message": "Review created successfully",
  "data": {
    "id": "674a1b2c3d4e5f6789abcdef",
    "patientId": "550e8400-e29b-41d4-a716-446655440001",
    "targetType": "DOCTOR",
    "doctorId": "550e8400-e29b-41d4-a716-446655440003",
    "rating": 5,
    "comment": "Excellent doctor, very professional!",
    "replies": [],
    "createdAt": "2025-01-01T10:30:00Z",
    "updatedAt": "2025-01-01T10:30:00Z"
  }
}
```

**Duplicate Review Error (409 Conflict):**
```json
{
  "success": false,
  "message": "Patient has already reviewed doctor 550e8400-e29b-41d4-a716-446655440003. Please update the existing review instead of creating a new one.",
  "data": {
    "message": "Patient has already reviewed doctor...",
    "existingReview": {
      "id": "674a1b2c3d4e5f6789abcdef",
      "rating": 4,
      "comment": "Previous review...",
      // ... other fields
    },
    "suggestedAction": "Please update the existing review instead of creating a new one.",
    "updateEndpoint": "/api/reviews"
  }
}
```

---

### 3. Update Review
```http
PUT /api/reviews
```

**Request Body:**
```json
{
  "id": "674a1b2c3d4e5f6789abcdef",
  "rating": 4,
  "comment": "Updated: Good doctor, but could be improved."
}
```

---

### 4. Get Review by ID
```http
GET /api/reviews/{id}
```

**Parameters:**
- `id` (string): Review ID

---

### 5. Delete Review
```http
DELETE /api/reviews/{id}
```

**Parameters:**
- `id` (string): Review ID

---

### 6. Get Reviews by Doctor
```http
GET /api/reviews/doctor/{doctorId}?page=1&pageSize=10
```

**Parameters:**
- `doctorId` (guid): Doctor ID
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Page size (default: 10)

---

### 7. Get Reviews by Service
```http
GET /api/reviews/service/{clinicServiceId}?page=1&pageSize=10
```

**Parameters:**
- `clinicServiceId` (guid): Clinic Service ID
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Page size (default: 10)

---

### 8. Get Reviews by Patient
```http
GET /api/reviews/patient/{patientId}?page=1&pageSize=10
```

**Parameters:**
- `patientId` (guid): Patient ID
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Page size (default: 10)

---

### 9. Search Reviews (Advanced)
```http
POST /api/reviews/search
```

**Request Body:**
```json
{
  "doctorId": "550e8400-e29b-41d4-a716-446655440003",
  "minRating": 4,
  "maxRating": 5,
  "page": 1,
  "pageSize": 10
}
```

---

## 📊 Statistics Endpoints

### 10. Get Doctor Statistics
```http
GET /api/reviews/doctor/{doctorId}/statistics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "targetId": "550e8400-e29b-41d4-a716-446655440003",
    "targetType": "DOCTOR",
    "averageRating": 4.32,
    "totalReviews": 127,
    "ratingDistribution": {
      "1": 3,
      "2": 8,
      "3": 15,
      "4": 42,
      "5": 59
    }
  }
}
```

### 11. Get Service Statistics
```http
GET /api/reviews/service/{clinicServiceId}/statistics
```

### 12. Batch Doctor Statistics (Performance Optimized)
```http
POST /api/reviews/doctors/batch-statistics
```

**Request Body:**
```json
{
  "doctorIds": [
    "550e8400-e29b-41d4-a716-446655440001",
    "550e8400-e29b-41d4-a716-446655440002",
    "550e8400-e29b-41d4-a716-446655440003"
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Batch doctor statistics retrieved successfully - 2/3 doctors with reviews",
  "data": {
    "doctorStatistics": {
      "550e8400-e29b-41d4-a716-446655440001": {
        "targetId": "550e8400-e29b-41d4-a716-446655440001",
        "targetType": "DOCTOR",
        "averageRating": 4.5,
        "totalReviews": 24,
        "ratingDistribution": {
          "1": 1, "2": 2, "3": 5, "4": 8, "5": 8
        }
      }
    },
    "notFoundDoctorIds": [
      "550e8400-e29b-41d4-a716-446655440003"
    ],
    "totalProcessed": 3,
    "withStatistics": 2
  }
}
```

### 13. Batch Service Statistics
```http
POST /api/reviews/services/batch-statistics
```

---

## 💬 Reply Management

### 14. Add Reply
```http
POST /api/reviews/reply
```

**Request Body:**
```json
{
  "reviewId": "674a1b2c3d4e5f6789abcdef",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "content": "Thank you for your feedback! We appreciate your review."
}
```

### 15. Update Reply
```http
PUT /api/reviews/reply
```

**Request Body:**
```json
{
  "reviewId": "674a1b2c3d4e5f6789abcdef",
  "replyId": "674a1b2c3d4e5f6789abcdf0",
  "content": "Updated: Thank you for your valuable feedback!"
}
```

### 16. Remove Reply
```http
DELETE /api/reviews/{reviewId}/reply/{replyId}
```

---

# 🚀 gRPC Service Documentation

## Connection Information
```
Server: localhost:6022 (Development)
Protocol: HTTP/2
Package: reviewservice
Service: ReviewService
```

## 🔧 Setup gRPC Client

### .NET Client
```csharp
// Add package reference
// <PackageReference Include="Grpc.Net.Client" Version="2.57.0" />

using Grpc.Net.Client;
using BookingCare.Services.Review.Grpc;

// Create channel
var channel = GrpcChannel.ForAddress("http://localhost:6022");
var client = new ReviewService.ReviewServiceClient(channel);
```

### Node.js Client
```bash
npm install @grpc/grpc-js @grpc/proto-loader
```

### Python Client
```bash
pip install grpcio grpcio-tools
```

## 📋 gRPC Methods

### 1. Health Check
```csharp
var request = new HealthCheckRequest();
var response = await client.HealthCheckAsync(request);

Console.WriteLine($"Status: {response.Status}");
Console.WriteLine($"Service: {response.Service}");
```

### 2. Get Doctor Statistics
```csharp
var request = new GetDoctorStatisticsRequest 
{ 
    DoctorId = "550e8400-e29b-41d4-a716-446655440003" 
};

var statistics = await client.GetDoctorStatisticsAsync(request);

Console.WriteLine($"Average Rating: {statistics.AverageRating}");
Console.WriteLine($"Total Reviews: {statistics.TotalReviews}");

// Access rating distribution
foreach (var kvp in statistics.RatingDistribution)
{
    Console.WriteLine($"{kvp.Key} stars: {kvp.Value} reviews");
}
```

### 3. Get Service Statistics
```csharp
var request = new GetServiceStatisticsRequest 
{ 
    ServiceId = "550e8400-e29b-41d4-a716-446655440004" 
};

var statistics = await client.GetServiceStatisticsAsync(request);
```

### 4. Batch Doctor Statistics
```csharp
var request = new BatchDoctorsStatisticsRequest();
request.DoctorIds.Add("550e8400-e29b-41d4-a716-446655440001");
request.DoctorIds.Add("550e8400-e29b-41d4-a716-446655440002");
request.DoctorIds.Add("550e8400-e29b-41d4-a716-446655440003");

var batchResponse = await client.GetBatchDoctorsStatisticsAsync(request);

Console.WriteLine($"Processed: {batchResponse.TotalProcessed}");
Console.WriteLine($"With Statistics: {batchResponse.WithStatistics}");

// Access individual statistics
foreach (var kvp in batchResponse.DoctorStatistics)
{
    var doctorId = kvp.Key;
    var stats = kvp.Value;
    Console.WriteLine($"Doctor {doctorId}: {stats.AverageRating:F2} stars, {stats.TotalReviews} reviews");
}

// Check not found doctors
foreach (var notFoundId in batchResponse.NotFoundDoctorIds)
{
    Console.WriteLine($"Doctor {notFoundId}: No reviews found");
}
```

### 5. Batch Service Statistics
```csharp
var request = new BatchServicesStatisticsRequest();
request.ServiceIds.Add("550e8400-e29b-41d4-a716-446655440001");
request.ServiceIds.Add("550e8400-e29b-41d4-a716-446655440002");

var batchResponse = await client.GetBatchServicesStatisticsAsync(request);
```

### 6. Get Doctor Reviews
```csharp
var request = new GetDoctorReviewsRequest 
{ 
    DoctorId = "550e8400-e29b-41d4-a716-446655440003",
    Page = 1,
    PageSize = 10
};

var reviews = await client.GetDoctorReviewsAsync(request);

Console.WriteLine($"Total Reviews: {reviews.TotalCount}");
Console.WriteLine($"Page: {reviews.Page}/{reviews.TotalPages}");

foreach (var review in reviews.Reviews)
{
    Console.WriteLine($"Rating: {review.Rating}/5");
    Console.WriteLine($"Comment: {review.Comment}");
    Console.WriteLine($"Created: {DateTimeOffset.FromUnixTimeSeconds(review.CreatedAt)}");
    
    // Access replies
    foreach (var reply in review.Replies)
    {
        Console.WriteLine($"  Reply: {reply.Content}");
        Console.WriteLine($"  By: {reply.AuthorId}");
    }
}
```

### 7. Get Service Reviews
```csharp
var request = new GetServiceReviewsRequest 
{ 
    ServiceId = "550e8400-e29b-41d4-a716-446655440004",
    Page = 1,
    PageSize = 10
};

var reviews = await client.GetServiceReviewsAsync(request);
```

---

# 🔧 Integration Examples

## Doctor Service Integration

```csharp
// In Doctor Service - Update doctor profile with review stats
public class DoctorService
{
    private readonly ReviewService.ReviewServiceClient _reviewClient;
    
    public async Task<DoctorProfileResponse> GetDoctorProfileAsync(Guid doctorId)
    {
        // Get doctor basic info
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        
        // Get review statistics via gRPC
        var reviewStats = await _reviewClient.GetDoctorStatisticsAsync(
            new GetDoctorStatisticsRequest { DoctorId = doctorId.ToString() });
        
        return new DoctorProfileResponse
        {
            Id = doctor.Id,
            Name = doctor.Name,
            Specialty = doctor.Specialty,
            AverageRating = reviewStats.AverageRating,
            TotalReviews = reviewStats.TotalReviews,
            RatingDistribution = reviewStats.RatingDistribution.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value)
        };
    }
    
    public async Task<List<DoctorSummary>> GetDoctorListWithRatingsAsync(List<Guid> doctorIds)
    {
        // Get batch statistics for performance
        var batchRequest = new BatchDoctorsStatisticsRequest();
        batchRequest.DoctorIds.AddRange(doctorIds.Select(id => id.ToString()));
        
        var batchStats = await _reviewClient.GetBatchDoctorsStatisticsAsync(batchRequest);
        
        var doctors = await _doctorRepository.GetByIdsAsync(doctorIds);
        
        return doctors.Select(doctor => new DoctorSummary
        {
            Id = doctor.Id,
            Name = doctor.Name,
            AverageRating = batchStats.DoctorStatistics.TryGetValue(
                doctor.Id.ToString(), out var stats) ? stats.AverageRating : 0,
            TotalReviews = stats?.TotalReviews ?? 0
        }).ToList();
    }
}
```

## Clinic Service Integration

```csharp
// In Clinic Service - Show clinic services with ratings
public class ClinicService
{
    private readonly ReviewService.ReviewServiceClient _reviewClient;
    
    public async Task<ClinicDetailsResponse> GetClinicDetailsAsync(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        
        // Get batch statistics for all services
        var serviceIds = clinic.Services.Select(s => s.Id.ToString()).ToList();
        var batchStats = await _reviewClient.GetBatchServicesStatisticsAsync(
            new BatchServicesStatisticsRequest { ServiceIds = { serviceIds } });
        
        var servicesWithRatings = clinic.Services.Select(service =>
        {
            var hasStats = batchStats.ServiceStatistics.TryGetValue(
                service.Id.ToString(), out var stats);
            
            return new ServiceSummary
            {
                Id = service.Id,
                Name = service.Name,
                Price = service.Price,
                AverageRating = hasStats ? stats.AverageRating : 0,
                TotalReviews = hasStats ? stats.TotalReviews : 0
            };
        }).ToList();
        
        return new ClinicDetailsResponse
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Address = clinic.Address,
            Services = servicesWithRatings
        };
    }
}
```

## API Gateway / BFF Integration

```csharp
// In API Gateway - Aggregate data from multiple services
public class PatientDashboardController : ControllerBase
{
    private readonly ReviewService.ReviewServiceClient _reviewClient;
    private readonly DoctorServiceClient _doctorClient;
    
    [HttpGet("patient/{patientId}/dashboard")]
    public async Task<IActionResult> GetPatientDashboard(Guid patientId)
    {
        // Get patient's recent reviews via gRPC
        var reviewsRequest = new GetPatientReviewsRequest 
        { 
            PatientId = patientId.ToString(),
            Page = 1,
            PageSize = 5
        };
        
        var recentReviews = await _reviewClient.GetPatientReviewsAsync(reviewsRequest);
        
        // Get doctor information for reviews
        var doctorIds = recentReviews.Reviews
            .Where(r => r.TargetType == TargetType.Doctor && !string.IsNullOrEmpty(r.DoctorId))
            .Select(r => r.DoctorId)
            .Distinct()
            .ToList();
        
        var doctors = await _doctorClient.GetDoctorsByIdsAsync(doctorIds);
        
        var reviewsWithDoctorInfo = recentReviews.Reviews.Select(review =>
        {
            var doctor = doctors.FirstOrDefault(d => d.Id == review.DoctorId);
            return new PatientReviewSummary
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                DoctorName = doctor?.Name ?? "Unknown",
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(review.CreatedAt),
                HasReplies = review.Replies.Count > 0
            };
        }).ToList();
        
        return Ok(new PatientDashboardResponse
        {
            PatientId = patientId,
            RecentReviews = reviewsWithDoctorInfo,
            TotalReviews = recentReviews.TotalCount
        });
    }
}
```

---

# ⚡ Performance Considerations

## Best Practices

### 1. Use Batch Operations
```csharp
// ❌ Bad: Multiple individual calls
foreach (var doctorId in doctorIds)
{
    var stats = await client.GetDoctorStatisticsAsync(
        new GetDoctorStatisticsRequest { DoctorId = doctorId.ToString() });
    // Process stats...
}

// ✅ Good: Single batch call
var batchRequest = new BatchDoctorsStatisticsRequest();
batchRequest.DoctorIds.AddRange(doctorIds.Select(id => id.ToString()));
var batchStats = await client.GetBatchDoctorsStatisticsAsync(batchRequest);
```

### 2. Connection Management
```csharp
// ✅ Reuse gRPC channels
public class ReviewServiceClient
{
    private static readonly GrpcChannel _channel = GrpcChannel.ForAddress("http://review-service:6022");
    private static readonly ReviewService.ReviewServiceClient _client = new(_channel);
    
    public static ReviewService.ReviewServiceClient Instance => _client;
}
```

### 3. Error Handling
```csharp
try
{
    var statistics = await client.GetDoctorStatisticsAsync(request);
    return statistics;
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
{
    // Handle not found
    return new ReviewStatisticsResponse 
    { 
        TargetId = request.DoctorId,
        AverageRating = 0,
        TotalReviews = 0
    };
}
catch (RpcException ex)
{
    _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
    throw;
}
```

---

# 🐛 Error Handling

## Common HTTP Status Codes

| Status | Description |
|--------|-------------|
| `200 OK` | Success |
| `201 Created` | Resource created |
| `400 Bad Request` | Invalid request data |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Duplicate review attempt |
| `500 Internal Server Error` | Server error |

## gRPC Status Codes

| Status | Description |
|--------|-------------|
| `OK` | Success |
| `INVALID_ARGUMENT` | Invalid input |
| `NOT_FOUND` | Resource not found |
| `INTERNAL` | Server error |

## Example Error Responses

### REST API Error
```json
{
  "success": false,
  "message": "Rating must be between 1 and 5 stars",
  "errors": [
    "Rating must be between 1 and 5 stars"
  ]
}
```

### gRPC Error
```csharp
catch (RpcException ex)
{
    switch (ex.StatusCode)
    {
        case StatusCode.InvalidArgument:
            // Handle validation error
            break;
        case StatusCode.NotFound:
            // Handle not found
            break;
        case StatusCode.Internal:
            // Handle server error
            break;
    }
}
```

---

# 📈 Monitoring & Logging

## Health Checks
```http
GET /api/reviews/health
GET grpc://localhost:6022/reviewservice.ReviewService/HealthCheck
```

## Logging
Service sử dụng structured logging với correlation IDs:

```json
{
  "timestamp": "2025-01-01T10:30:00Z",
  "level": "Information",
  "message": "Review created successfully",
  "correlationId": "abc-123-def",
  "properties": {
    "reviewId": "674a1b2c3d4e5f6789abcdef",
    "patientId": "550e8400-e29b-41d4-a716-446655440001",
    "targetType": "DOCTOR"
  }
}
```

---

# 🔒 Security

## Authentication
- REST API: JWT Bearer tokens
- gRPC: Metadata headers (trong production)

## Authorization
- Role-based access control
- Patient chỉ có thể tạo/sửa review của mình
- Doctor/Admin có thể reply và xem statistics

---

# 🌟 Quick Start Examples

## REST API với cURL

```bash
# Create review
curl -X POST http://localhost:6012/api/reviews \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "patientId": "550e8400-e29b-41d4-a716-446655440001",
    "targetType": "DOCTOR",
    "doctorId": "550e8400-e29b-41d4-a716-446655440003",
    "rating": 5,
    "comment": "Excellent doctor!"
  }'

# Get doctor statistics
curl http://localhost:6012/api/reviews/doctor/550e8400-e29b-41d4-a716-446655440003/statistics

# Batch statistics
curl -X POST http://localhost:6012/api/reviews/doctors/batch-statistics \
  -H "Content-Type: application/json" \
  -d '{
    "doctorIds": [
      "550e8400-e29b-41d4-a716-446655440001",
      "550e8400-e29b-41d4-a716-446655440002"
    ]
  }'
```

## gRPC với grpcurl

```bash
# Health check
grpcurl -plaintext localhost:6022 reviewservice.ReviewService/HealthCheck

# Doctor statistics
grpcurl -plaintext \
  -d '{"doctor_id": "550e8400-e29b-41d4-a716-446655440003"}' \
  localhost:6022 reviewservice.ReviewService/GetDoctorStatistics

# Batch doctor statistics
grpcurl -plaintext \
  -d '{"doctor_ids": ["550e8400-e29b-41d4-a716-446655440001", "550e8400-e29b-41d4-a716-446655440002"]}' \
  localhost:6022 reviewservice.ReviewService/GetBatchDoctorsStatistics
```

---

# 📚 Additional Resources

## API Documentation
- **Swagger UI**: http://localhost:6012/swagger (Development)
- **Proto File**: `src/Services/BookingCare.Services.Review/Protos/ReviewService.proto`

## Support
- **Team**: BookingCare Development Team
- **Slack**: #bookingcare-development
- **Repository**: BookingCareSystemRepository

---

**Happy Coding! 🚀**

*Last Updated: January 2025*