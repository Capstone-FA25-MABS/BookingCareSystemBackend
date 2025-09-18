using AutoMapper;
using BookingCare.Services.Review.Data;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;
using BookingCare.Services.Review.Enums;
using BookingCare.Services.Review.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookingCare.Services.Review.Repositories.Implementations;

/// <summary>
/// Repository implementation for Review operations
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly IMongoCollection<ReviewEntity> _reviews;
    private readonly IMapper _mapper;

    public ReviewRepository(IReviewDbContext dbContext, IMapper mapper)
    {
        _reviews = dbContext.GetCollection<ReviewEntity>("reviews");
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new review
    /// </summary>
    public async Task<ReviewEntity> CreateAsync(ReviewEntity review)
    {
        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _reviews.InsertOneAsync(review);
        return review;
    }

    /// <summary>
    /// Updates an existing review
    /// </summary>
    public async Task<ReviewEntity?> UpdateAsync(ReviewEntity review)
    {
        review.UpdatedAt = DateTime.UtcNow;
        var result = await _reviews.ReplaceOneAsync(r => r.Id == review.Id, review);
        return result.ModifiedCount > 0 ? review : null;
    }

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    public async Task<ReviewEntity?> GetByIdAsync(string id)
    {
        return await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets reviews with filtering and pagination
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsAsync(GetReviewsRequest request)
    {
        var filterBuilder = Builders<ReviewEntity>.Filter;
        var filter = filterBuilder.Empty;

        // Apply filters
        if (request.PatientId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.PatientId, request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.DoctorId, request.DoctorId.Value);
        }

        if (request.ClinicServiceId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.ClinicServiceId, request.ClinicServiceId.Value);
        }

        if (request.TargetType.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.TargetType, request.TargetType.Value);
        }

        if (request.MinRating.HasValue)
        {
            filter &= filterBuilder.Gte(r => r.Rating, request.MinRating.Value);
        }

        if (request.MaxRating.HasValue)
        {
            filter &= filterBuilder.Lte(r => r.Rating, request.MaxRating.Value);
        }

        // Get total count
        var totalCount = await _reviews.CountDocumentsAsync(filter);

        // Get paginated results
        var skip = (request.Page - 1) * request.PageSize;
        var reviews = await _reviews
            .Find(filter)
            .Sort(Builders<ReviewEntity>.Sort.Descending(r => r.CreatedAt))
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync();

        var reviewResponses = _mapper.Map<List<ReviewResponse>>(reviews);
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedReviewsResponse
        {
            Reviews = reviewResponses,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1,
        };
    }

    /// <summary>
    /// Gets reviews for a specific doctor
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByDoctorAsync(
        Guid doctorId,
        int page = 1,
        int pageSize = 10
    )
    {
        var request = new GetReviewsRequest
        {
            DoctorId = doctorId,
            TargetType = TargetType.DOCTOR,
            Page = page,
            PageSize = pageSize,
        };
        return await GetReviewsAsync(request);
    }

    /// <summary>
    /// Gets reviews for a specific clinic service
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByClinicServiceAsync(
        Guid clinicServiceId,
        int page = 1,
        int pageSize = 10
    )
    {
        var request = new GetReviewsRequest
        {
            ClinicServiceId = clinicServiceId,
            TargetType = TargetType.SERVICE,
            Page = page,
            PageSize = pageSize,
        };
        return await GetReviewsAsync(request);
    }

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByPatientAsync(
        Guid patientId,
        int page = 1,
        int pageSize = 10
    )
    {
        var request = new GetReviewsRequest
        {
            PatientId = patientId,
            Page = page,
            PageSize = pageSize,
        };
        return await GetReviewsAsync(request);
    }

    /// <summary>
    /// Adds a reply to a review
    /// </summary>
    public async Task<ReviewEntity?> AddReplyAsync(string reviewId, ReplyEntity reply)
    {
        reply.CreatedAt = DateTime.UtcNow;
        reply.UpdatedAt = DateTime.UtcNow;

        var update = Builders<ReviewEntity>
            .Update.Push(r => r.Replies, reply)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var result = await _reviews.FindOneAndUpdateAsync(
            r => r.Id == reviewId,
            update,
            new FindOneAndUpdateOptions<ReviewEntity> { ReturnDocument = ReturnDocument.After }
        );

        return result;
    }

    /// <summary>
    /// Removes a reply from a review
    /// </summary>
    public async Task<ReviewEntity?> RemoveReplyAsync(string reviewId, string replyId)
    {
        var update = Builders<ReviewEntity>
            .Update.PullFilter(r => r.Replies, reply => reply.Id == replyId)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var result = await _reviews.FindOneAndUpdateAsync(
            r => r.Id == reviewId,
            update,
            new FindOneAndUpdateOptions<ReviewEntity> { ReturnDocument = ReturnDocument.After }
        );

        return result;
    }

    /// <summary>
    /// Updates a reply in a review
    /// </summary>
    public async Task<ReviewEntity?> UpdateReplyAsync(
        string reviewId,
        string replyId,
        string content
    )
    {
        var update = Builders<ReviewEntity>
            .Update.Set("replies.$.content", content)
            .Set("replies.$.updatedAt", DateTime.UtcNow)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var result = await _reviews.FindOneAndUpdateAsync(
            r => r.Id == reviewId && r.Replies.Any(reply => reply.Id == replyId),
            update,
            new FindOneAndUpdateOptions<ReviewEntity> { ReturnDocument = ReturnDocument.After }
        );

        return result;
    }

    /// <summary>
    /// Gets the average rating for a doctor
    /// </summary>
    public async Task<double> GetAverageRatingByDoctorAsync(Guid doctorId)
    {
        return await GetAverageRatingAsync("doctorId", doctorId.ToString(), "DOCTOR");
    }

    /// <summary>
    /// Gets the average rating for a clinic service
    /// </summary>
    public async Task<double> GetAverageRatingByClinicServiceAsync(Guid clinicServiceId)
    {
        return await GetAverageRatingAsync("clinicServiceId", clinicServiceId.ToString(), "SERVICE");
    }

    /// <summary>
    /// Generic method to get average rating for any target type
    /// </summary>
    private async Task<double> GetAverageRatingAsync(string targetIdField, string targetIdValue, string targetType)
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { targetIdField, targetIdValue },
                { "targetType", targetType },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "averageRating", new BsonDocument("$avg", "$rating") },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (result != null && result.Contains("averageRating"))
        {
            return result["averageRating"].ToDouble();
        }

        return 0.0;
    }

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    public async Task<long> GetReviewCountByDoctorAsync(Guid doctorId)
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.DoctorId, doctorId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.DOCTOR)
        );
        return await _reviews.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Gets the total count of reviews for a clinic service
    /// </summary>
    public async Task<long> GetReviewCountByClinicServiceAsync(Guid clinicServiceId)
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.ClinicServiceId, clinicServiceId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.SERVICE)
        );
        return await _reviews.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Gets comprehensive statistics for a doctor
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetDoctorStatisticsAsync(Guid doctorId)
    {
        return await GetStatisticsAsync("doctorId", doctorId.ToString(), "DOCTOR", doctorId);
    }

    /// <summary>
    /// Gets comprehensive statistics for a clinic service
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetClinicServiceStatisticsAsync(Guid clinicServiceId)
    {
        return await GetStatisticsAsync("clinicServiceId", clinicServiceId.ToString(), "SERVICE", clinicServiceId);
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a doctor (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatisticsAsync(Guid doctorId)
    {
        return await GetDetailedStatisticsAsync("doctorId", doctorId.ToString(), "DOCTOR", doctorId);
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a clinic service (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetClinicServiceDetailedStatisticsAsync(Guid clinicServiceId)
    {
        return await GetDetailedStatisticsAsync("clinicServiceId", clinicServiceId.ToString(), "SERVICE", clinicServiceId);
    }

    /// <summary>
    /// Generic method to get optimized statistics for any target type (without rating distribution for batch)
    /// </summary>
    private async Task<ReviewStatisticsResponse> GetStatisticsAsync(
        string targetIdField,
        string targetIdValue,
        string targetType,
        Guid targetId)
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { targetIdField, targetIdValue },
                { "targetType", targetType },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "averageRating", new BsonDocument("$avg", "$rating") },
                { "totalReviews", new BsonDocument("$sum", 1) }
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (
            result == null
            || !result.Contains("totalReviews")
            || result["totalReviews"].ToInt64() == 0
        )
        {
            return new ReviewStatisticsResponse
            {
                TargetId = targetId,
                AverageRating = 0.0,
                TotalReviews = 0
            };
        }

        var averageRating = result.Contains("averageRating")
            ? result["averageRating"].ToDouble()
            : 0.0;
        var totalReviews = result["totalReviews"].ToInt64();

        return new ReviewStatisticsResponse
        {
            TargetId = targetId,
            AverageRating = Math.Round(averageRating, 2),
            TotalReviews = totalReviews
        };
    }

    /// <summary>
    /// Generic method to get detailed statistics with rating distribution (for single endpoints)
    /// </summary>
    private async Task<ReviewDetailedStatisticsResponse> GetDetailedStatisticsAsync(
        string targetIdField,
        string targetIdValue,
        string targetType,
        Guid targetId)
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { targetIdField, targetIdValue },
                { "targetType", targetType },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "averageRating", new BsonDocument("$avg", "$rating") },
                { "totalReviews", new BsonDocument("$sum", 1) },
                { "ratingDistribution", new BsonDocument("$push", "$rating") },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (
            result == null
            || !result.Contains("totalReviews")
            || result["totalReviews"].ToInt64() == 0
        )
        {
            return new ReviewDetailedStatisticsResponse
            {
                TargetId = targetId,
                AverageRating = 0.0,
                TotalReviews = 0,
                RatingDistribution = new Dictionary<int, long>(),
            };
        }

        var averageRating = result.Contains("averageRating")
            ? result["averageRating"].ToDouble()
            : 0.0;
        var totalReviews = result["totalReviews"].ToInt64();

        // Calculate rating distribution
        var ratings = result["ratingDistribution"].AsBsonArray.Select(r => r.ToInt32()).ToList();
        var ratingDistribution = new Dictionary<int, long>();

        for (int i = 1; i <= 5; i++)
        {
            ratingDistribution[i] = ratings.Count(r => r == i);
        }

        return new ReviewDetailedStatisticsResponse
        {
            TargetId = targetId,
            AverageRating = Math.Round(averageRating, 2),
            TotalReviews = totalReviews,
            RatingDistribution = ratingDistribution,
        };
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple doctors in a single query
    /// </summary>
    public async Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatisticsAsync(
        List<Guid> doctorIds
    )
    {
        var doctorIdsStrings = doctorIds.Select(id => id.ToString()).ToList();

        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { "doctorId", new BsonDocument("$in", new BsonArray(doctorIdsStrings)) },
                { "targetType", "DOCTOR" },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", "$doctorId" },
                { "averageRating", new BsonDocument("$avg", "$rating") },
                { "totalReviews", new BsonDocument("$sum", 1) }
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var results = await cursor.ToListAsync();

        var response = new BatchDoctorsStatisticsResponse();

        // Create a set of doctors that have reviews
        var foundDoctorIds = new HashSet<Guid>();

        // Add doctors that have reviews
        foreach (var result in results)
        {
            var doctorIdString = result["_id"].AsString;
            if (Guid.TryParse(doctorIdString, out var doctorId))
            {
                foundDoctorIds.Add(doctorId);

                var averageRating = result.Contains("averageRating")
                    ? result["averageRating"].ToDouble()
                    : 0.0;
                var totalReviews = result["totalReviews"].ToInt64();

                response.DoctorStatistics[doctorId] = new ReviewStatisticsResponse
                {
                    TargetId = doctorId,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews
                };
            }
        }

        // Add doctors that don't have reviews with 0 values
        foreach (var doctorId in doctorIds)
        {
            if (!foundDoctorIds.Contains(doctorId))
            {
                response.DoctorStatistics[doctorId] = new ReviewStatisticsResponse
                {
                    TargetId = doctorId,
                    AverageRating = 0.0,
                    TotalReviews = 0
                };
            }
        }

        return response;
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple clinic services in a single query
    /// </summary>
    public async Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(
        List<Guid> serviceIds
    )
    {
        var serviceIdsStrings = serviceIds.Select(id => id.ToString()).ToList();

        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { "clinicServiceId", new BsonDocument("$in", new BsonArray(serviceIdsStrings)) },
                { "targetType", "SERVICE" },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", "$clinicServiceId" },
                { "averageRating", new BsonDocument("$avg", "$rating") },
                { "totalReviews", new BsonDocument("$sum", 1) }
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var results = await cursor.ToListAsync();

        var response = new BatchServicesStatisticsResponse();

        // Create a set of services that have reviews
        var foundServiceIds = new HashSet<Guid>();

        // Add services that have reviews
        foreach (var result in results)
        {
            var serviceIdString = result["_id"].AsString;
            if (Guid.TryParse(serviceIdString, out var serviceId))
            {
                foundServiceIds.Add(serviceId);

                var averageRating = result.Contains("averageRating")
                    ? result["averageRating"].ToDouble()
                    : 0.0;
                var totalReviews = result["totalReviews"].ToInt64();

                response.ServiceStatistics[serviceId] = new ReviewStatisticsResponse
                {
                    TargetId = serviceId,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews
                };
            }
        }

        // Add services that don't have reviews with 0 values
        foreach (var serviceId in serviceIds)
        {
            if (!foundServiceIds.Contains(serviceId))
            {
                response.ServiceStatistics[serviceId] = new ReviewStatisticsResponse
                {
                    TargetId = serviceId,
                    AverageRating = 0.0,
                    TotalReviews = 0
                };
            }
        }

        return response;
    }

    /// <summary>
    /// Checks if a patient has already reviewed a specific doctor
    /// </summary>
    public async Task<ReviewEntity?> GetExistingDoctorReviewAsync(Guid patientId, Guid doctorId)
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.PatientId, patientId),
            Builders<ReviewEntity>.Filter.Eq(r => r.DoctorId, doctorId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.DOCTOR)
        );

        return await _reviews.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Checks if a patient has already reviewed a specific clinic service
    /// </summary>
    public async Task<ReviewEntity?> GetExistingServiceReviewAsync(
        Guid patientId,
        Guid clinicServiceId
    )
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.PatientId, patientId),
            Builders<ReviewEntity>.Filter.Eq(r => r.ClinicServiceId, clinicServiceId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.SERVICE)
        );

        return await _reviews.Find(filter).FirstOrDefaultAsync();
    }
}
