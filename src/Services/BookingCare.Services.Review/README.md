# ReviewService API Documentation - Frontend Integration Guide

## 📖 Overview
The ReviewService provides a comprehensive API for managing patient reviews and replies for doctors and medical services in the BookingCare system. This service includes **appointment history validation** to ensure only patients who have completed appointments can create reviews.

## 🚀 Service Information

| Information | Value |
|-------------|-------|
| **Service Name** | BookingCare.Services.Review |
| **Technology** | .NET 8, ASP.NET Core, MongoDB |
| **REST API Port** | 6012 (HTTP/1.1 + HTTP/2) |
| **gRPC Port** | 6022 (HTTP/2 only) |
| **Database** | MongoDB |
| **API Version** | v1.0 |

## 🔐 Base URLs
```
Development: http://localhost:6012/api/v1.0/reviews
Production: https://api.bookingcare.com/api/v1.0/reviews
```

---

## ⚠️ **Critical Business Rules for Frontend**

### 🚨 **NEW: Appointment History Validation**
**Effective immediately, patients can only create reviews for doctors/services they have completed appointments with.**

#### **Validation Rules:**
1. **Appointment Status Must be COMPLETED**: Only appointments with `COMPLETED` status count
2. **Target-Specific Validation**: 
   - For doctor reviews: Must have completed appointment **with that specific doctor**
 - For service reviews: Must have completed appointment **with that specific service**
3. **One Review Per Target**: Each patient can create only one review per doctor or service
4. **No Validation for Replies**: Anyone with appropriate permissions can reply (no appointment history required)

---

## 📋 Complete API Reference for Frontend

### 🔐 Authentication
All endpoints require JWT Bearer token:
```http
Authorization: Bearer <your-jwt-token>
Content-Type: application/json
```

---

### 1. 🆕 **Create Review** (With Appointment Validation)

```http
POST /api/v1.0/reviews
```

**Request Body:**
```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",
  "targetType": "DOCTOR",  // "DOCTOR" or "SERVICE"
  "doctorId": "550e8400-e29b-41d4-a716-446655440001",  // Required if targetType = "DOCTOR"
  "serviceId": null,       // Required if targetType = "SERVICE"
  "rating": 5,      // 1-5 stars
  "comment": "Great doctor! Very professional and caring."
}
```

**✅ Success Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "674a1b2c3d4e5f6789abcdef",
    "patientId": "550e8400-e29b-41d4-a716-446655440000",
    "patientInfo": {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "email": "patient@example.com",
      "fullName": "John Doe",
      "avatarUrl": "https://example.com/avatar.jpg",
      "found": true
    },
    "doctorId": "550e8400-e29b-41d4-a716-446655440001",
    "serviceId": null,
    "rating": 5,
    "comment": "Great doctor! Very professional and caring.",
    "replies": [],
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  },
  "message": "Review created successfully"
}
```

**❌ Error: No Appointment History (400 Bad Request):**
```json
{
  "success": false,
  "errors": {
    "message": "You must complete an appointment with this doctor before creating a review.",
    "patientId": "550e8400-e29b-41d4-a716-446655440000",
    "doctorId": "550e8400-e29b-41d4-a716-446655440001",
    "targetType": "DOCTOR",
    "suggestedAction": "Complete an appointment with this doctor before creating a review.",
    "requirementInfo": "Reviews can only be created after completing an appointment with the target doctor or service."
  }
}
```

**❌ Error: Duplicate Review (409 Conflict):**
```json
{
  "success": false,
  "errors": {
    "message": "Patient has already reviewed doctor 550e8400-e29b-41d4-a716-446655440001. Please update the existing review instead of creating a new one.",
  "existingReview": {
      "id": "674a1b2c3d4e5f6789abcde0",
      "rating": 4,
      "comment": "Previous review...",
      "createdAt": "2024-01-10T09:00:00Z"
  },
    "suggestedAction": "Please update the existing review instead of creating a new one.",
    "updateEndpoint": "/api/v1.0/reviews"
  }
}
```

---

### 2. **Update Review**

```http
PUT /api/v1.0/reviews
```

**Request Body:**
```json
{
  "id": "674a1b2c3d4e5f6789abcdef",
  "rating": 4,
  "comment": "Updated: Still a great doctor, very recommended!"
}
```

---

### 3. **Get Reviews for Doctor** (With User Info)

```http
GET /api/v1.0/reviews/doctor/{doctorId}?page=1&pageSize=10
```

**Success Response:**
```json
{
  "success": true,
  "data": {
    "reviews": [
      {
        "id": "674a1b2c3d4e5f6789abcdef",
        "patientId": "550e8400-e29b-41d4-a716-446655440000",
        "patientInfo": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "patient@example.com",
          "fullName": "John Doe",
   "avatarUrl": "https://example.com/avatar.jpg",
          "found": true
        },
        "doctorId": "550e8400-e29b-41d4-a716-446655440001",
   "rating": 5,
   "comment": "Great doctor! Very professional and caring.",
        "replies": [
     {
      "id": "674a1b2c3d4e5f6789abcdf1",
      "authorId": "550e8400-e29b-41d4-a716-446655440001",
"authorInfo": {
          "accountId": "550e8400-e29b-41d4-a716-446655440001",
 "email": "doctor@example.com",
              "fullName": "Dr. Smith",
   "avatarUrl": "https://example.com/doctor-avatar.jpg",
              "role": "DOCTOR",
     "found": true
  },
    "content": "Thank you for your feedback!",
   "createdAt": "2024-01-15T12:00:00Z",
      "updatedAt": "2024-01-15T12:00:00Z"
       }
        ],
     "createdAt": "2024-01-15T10:30:00Z",
        "updatedAt": "2024-01-15T10:30:00Z"
 }
    ],
    "totalCount": 25,
"page": 1,
  "pageSize": 10,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "message": "Doctor reviews retrieved successfully"
}
```

---

### 4. **Get Reviews for Service**

```http
GET /api/v1.0/reviews/service/{serviceId}?page=1&pageSize=10
```

---

### 5. **Get Reviews by Patient**

```http
GET /api/v1.0/reviews/patient/{patientId}?page=1&pageSize=10
```

---

### 6. **Search Reviews (Advanced)**

```http
POST /api/v1.0/reviews/search
```

**Request Body:**
```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",  // Optional
  "doctorId": "550e8400-e29b-41d4-a716-446655440001",   // Optional
  "serviceId": "550e8400-e29b-41d4-a716-446655440002",  // Optional
  "minRating": 4,    // Optional: 1-5
  "maxRating": 5,        // Optional: 1-5
  "fromDate": "2024-01-01T00:00:00Z",  // Optional
  "toDate": "2024-01-31T23:59:59Z",    // Optional
  "page": 1,
  "pageSize": 10
}
```

---

## 💬 Reply Management

### 7. **Add Reply to Review**

```http
POST /api/v1.0/reviews/reply
```

**Request Body:**
```json
{
  "reviewId": "674a1b2c3d4e5f6789abcdef",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "content": "Thank you for your feedback! We appreciate your review."
}
```

### 8. **Update Reply**

```http
PUT /api/v1.0/reviews/reply
```

### 9. **Remove Reply**

```http
DELETE /api/v1.0/reviews/{reviewId}/reply/{replyId}
```

---

## 📊 Statistics & Analytics

### 10. **Get Doctor Statistics**

```http
GET /api/v1.0/reviews/doctor/{doctorId}/statistics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "targetId": "550e8400-e29b-41d4-a716-446655440001",
    "averageRating": 4.7,
    "totalReviews": 45,
    "ratingDistribution": {
      "1": 1,
    "2": 2,
      "3": 5,
      "4": 12,
   "5": 25
    }
  },
  "message": "Doctor statistics retrieved successfully"
}
```

### 11. **Batch Doctor Statistics** (Performance Optimized)

```http
POST /api/v1.0/reviews/doctors/batch-statistics
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

---

## 🎨 Frontend Integration Examples

### React/TypeScript Implementation

```typescript
// types/review.ts
interface ReviewRequest {
  patientId: string;
  targetType: 'DOCTOR' | 'SERVICE';
  doctorId?: string;
  serviceId?: string;
  rating: number; // 1-5
  comment: string;
}

interface Review {
  id: string;
  patientId: string;
  patientInfo: {
    userId: string;
    email: string;
    fullName: string;
    avatarUrl: string;
    found: boolean;
  };
  doctorId?: string;
  serviceId?: string;
  rating: number;
  comment: string;
  replies: Reply[];
  createdAt: string;
  updatedAt: string;
}

// services/reviewService.ts
class ReviewService {
  private baseUrl = 'http://localhost:6012/api/v1.0/reviews';
  
  async createReview(reviewData: ReviewRequest): Promise<Review> {
    try {
      const response = await fetch(`${this.baseUrl}`, {
        method: 'POST',
 headers: {
  'Content-Type': 'application/json',
      'Authorization': `Bearer ${getAuthToken()}`
     },
        body: JSON.stringify(reviewData)
      });

      const result = await response.json();
      
      if (!response.ok) {
        if (response.status === 400) {
          // No appointment history
throw new NoAppointmentHistoryError(result.errors);
        } else if (response.status === 409) {
          // Duplicate review
     throw new DuplicateReviewError(result.errors);
     }
        throw new Error(result.message || 'Failed to create review');
      }
      
      return result.data;
    } catch (error) {
      console.error('Create review error:', error);
      throw error;
    }
  }

  async getDoctorReviews(doctorId: string, page = 1, pageSize = 10) {
    const response = await fetch(
      `${this.baseUrl}/doctor/${doctorId}?page=${page}&pageSize=${pageSize}`,
      {
   headers: {
          'Authorization': `Bearer ${getAuthToken()}`
        }
      }
    );
    
    const result = await response.json();
    return result.data;
  }

  async getDoctorStatistics(doctorId: string) {
    const response = await fetch(`${this.baseUrl}/doctor/${doctorId}/statistics`, {
      headers: {
        'Authorization': `Bearer ${getAuthToken()}`
      }
    });
    
    const result = await response.json();
    return result.data;
  }
}

// Custom error classes for better error handling
class NoAppointmentHistoryError extends Error {
  constructor(public errorDetails: any) {
    super(errorDetails.message);
    this.name = 'NoAppointmentHistoryError';
  }
}

class DuplicateReviewError extends Error {
  constructor(public errorDetails: any) {
    super(errorDetails.message);
    this.name = 'DuplicateReviewError';
  }
}

// React component example
import React, { useState } from 'react';

const ReviewForm: React.FC<{ doctorId: string; patientId: string }> = ({ doctorId, patientId }) => {
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const reviewService = new ReviewService();
 await reviewService.createReview({
        patientId,
targetType: 'DOCTOR',
        doctorId,
     rating,
        comment
      });
      
      // Success - redirect or show success message
      alert('Review created successfully!');
    } catch (err) {
      if (err instanceof NoAppointmentHistoryError) {
        setError('You need to complete an appointment with this doctor before leaving a review.');
    // Optionally redirect to booking page
      } else if (err instanceof DuplicateReviewError) {
      setError('You have already reviewed this doctor. You can update your existing review instead.');
        // Optionally show update form
      } else {
        setError('Failed to create review. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="review-form">
      <div className="rating-input">
   <label>Rating:</label>
        <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
          {[1, 2, 3, 4, 5].map(num => (
   <option key={num} value={num}>{num} star{num > 1 ? 's' : ''}</option>
  ))}
        </select>
      </div>

      <div className="comment-input">
        <label>Comment:</label>
        <textarea
          value={comment}
       onChange={(e) => setComment(e.target.value)}
          required
          rows={4}
          placeholder="Share your experience with this doctor..."
    />
      </div>

      {error && <div className="error-message">{error}</div>}

      <button type="submit" disabled={loading}>
  {loading ? 'Creating Review...' : 'Submit Review'}
      </button>
    </form>
  );
};
```

### Vue.js Example

```javascript
// composables/useReviews.js
import { ref, reactive } from 'vue'

export function useReviews() {
  const loading = ref(false)
  const error = ref(null)
  
  const createReview = async (reviewData) => {
    loading.value = true
    error.value = null
    
    try {
      const response = await $fetch('/api/v1.0/reviews', {
        method: 'POST',
     body: reviewData,
        headers: {
  'Authorization': `Bearer ${useAuthStore().token}`
        }
      })
      
      return response.data
    } catch (err) {
    if (err.status === 400) {
        error.value = {
  type: 'NO_APPOINTMENT_HISTORY',
message: err.data.errors.message,
          suggestion: err.data.errors.suggestedAction
        }
      } else if (err.status === 409) {
        error.value = {
          type: 'DUPLICATE_REVIEW',
     message: err.data.errors.message,
     existingReview: err.data.errors.existingReview
        }
      } else {
        error.value = {
     type: 'GENERAL_ERROR',
       message: err.message || 'An error occurred'
      }
      }
    throw error.value
    } finally {
      loading.value = false
    }
  }
  
  return {
    loading,
    error,
    createReview
  }
}
```

---

## 🚨 **Error Handling Guide for Frontend**

### HTTP Status Codes

| Status | Description | When it occurs |
|--------|-------------|----------------|
| 200 | OK | Successful GET, PUT, DELETE operations |
| 201 | Created | Successful review creation |
| 400 | Bad Request | Validation errors, **no appointment history** |
| 401 | Unauthorized | Missing or invalid JWT token |
| 404 | Not Found | Review, doctor, or service not found |
| 409 | Conflict | **Duplicate review attempt** |
| 500 | Internal Server Error | Unexpected server error |

### Error Handling Best Practices

```javascript
// 1. Handle Appointment History Validation
try {
  await createReview(reviewData);
} catch (error) {
  if (error.name === 'NoAppointmentHistoryError') {
    // Show user-friendly message
    showToast('Please complete an appointment before leaving a review', 'info');
    // Optionally redirect to booking page
    router.push(`/book-appointment?doctorId=${doctorId}`);
  }
}

// 2. Handle Duplicate Reviews Gracefully
catch (error) {
  if (error.name === 'DuplicateReviewError') {
    // Show existing review and offer to update it
    showUpdateReviewModal(error.errorDetails.existingReview);
  }
}

// 3. Implement Proper Loading States
const [loading, setLoading] = useState(false);
const [reviews, setReviews] = useState([]);

const loadReviews = async () => {
  setLoading(true);
  try {
    const data = await getDoctorReviews(doctorId, page);
    setReviews(data.reviews);
  } finally {
setLoading(false);
  }
};
```

---

## ⚡ Performance Optimization Tips

### 1. **Use Batch Statistics for Lists**
```javascript
// ❌ Bad: Multiple individual calls
const doctorIds = ['id1', 'id2', 'id3'];
for (const doctorId of doctorIds) {
  const stats = await getDoctorStatistics(doctorId);
  // Process stats...
}

// ✅ Good: Single batch call
const statistics = await getBatchDoctorStatistics(doctorIds);
// Process all statistics at once
```

### 2. **Implement Pagination Properly**
```javascript
const [pagination, setPagination] = useState({
  page: 1,
  pageSize: 10,
  totalPages: 0,
  hasNextPage: false
});

const loadMoreReviews = async () => {
  if (pagination.hasNextPage && !loading) {
    const nextPage = pagination.page + 1;
    const data = await getDoctorReviews(doctorId, nextPage);
    
    setReviews(prev => [...prev, ...data.reviews]);
    setPagination({
      ...pagination,
      page: nextPage,
      hasNextPage: data.hasNextPage
    });
  }
};
```

### 3. **Cache Statistics Data**
```javascript
// Cache statistics for 5 minutes
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
const statisticsCache = new Map();

const getCachedDoctorStatistics = async (doctorId) => {
  const cacheKey = `doctor_stats_${doctorId}`;
  const cached = statisticsCache.get(cacheKey);
  
  if (cached && Date.now() - cached.timestamp < CACHE_DURATION) {
    return cached.data;
  }
  
  const stats = await getDoctorStatistics(doctorId);
  statisticsCache.set(cacheKey, {
    data: stats,
    timestamp: Date.now()
  });
  
  return stats;
};
```

---

## 🔍 **User Experience Guidelines**

### 1. **Review Creation Flow**
```
1. User clicks "Leave Review"
2. Check if user has completed appointment (frontend validation optional)
3. Show review form
4. On submit:
   - Show loading state
   - Handle appointment history error gracefully
   - Handle duplicate review error
   - Show success message or redirect
```

### 2. **Error Messages**
```javascript
const ERROR_MESSAGES = {
  NO_APPOINTMENT: 'Complete an appointment first to leave a review',
  DUPLICATE_REVIEW: 'You\'ve already reviewed this doctor. Update your existing review instead',
  INVALID_RATING: 'Please select a rating from 1-5 stars',
  EMPTY_COMMENT: 'Please share your experience in the comment field'
};
```

### 3. **Loading States**
- Show skeleton loaders for reviews list
- Disable submit button during review creation
- Show progress indicators for batch operations

---

## 📞 **Support & Troubleshooting**

### Common Issues:

**1. "No appointment history" error**
- **Cause**: Patient hasn't completed appointments with target doctor/service
- **Solution**: Guide user to book and complete an appointment first

**2. "Duplicate review" error**
- **Cause**: Patient already reviewed this doctor/service
- **Solution**: Offer to update existing review instead

**3. Reviews not loading**
- **Solution**: Check doctor/service ID validity, verify authentication

**4. Statistics showing 0**
- **Cause**: No reviews exist yet for the target
- **Solution**: This is normal for new doctors/services

---

## 🔄 **Changelog**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-01-15 | Initial release with appointment history validation |
| 1.1 | 2024-01-15 | Enhanced frontend integration documentation |

---

**Last Updated**: January 15, 2024  
**API Version**: v1.0  
**Documentation Version**: 1.1

**Frontend Team**: Use this guide for seamless integration with the ReviewService. For questions, contact the backend team or check the #bookingcare-development Slack channel.