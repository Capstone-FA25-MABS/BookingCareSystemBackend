using AutoMapper;
using BookingCare.Services.Review.Data;
using BookingCare.Services.Review.Enums;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;
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

    // MongoDB field name constants (SonarQube S1192)
    private const string TotalReviewsField = "totalReviews";
    private const string AverageRatingField = "averageRating";
    private const string RatingDistributionField = "ratingDistribution";

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

        if (request.ServiceId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.ServiceId, request.ServiceId.Value);
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

        if (request.HospitalId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.HospitalId, request.HospitalId.Value);
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
        int pageSize = 10,
        int? minRating = null,
        int? maxRating = null
    )
    {
        var request = new GetReviewsRequest
        {
            DoctorId = doctorId,
            TargetType = TargetType.DOCTOR,
            Page = page,
            PageSize = pageSize,
            MinRating = minRating,
            MaxRating = maxRating,
        };
        return await GetReviewsAsync(request);
    }

    /// <summary>
    /// Gets reviews for a specific service
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByServiceAsync(
        Guid serviceId,
        int page = 1,
        int pageSize = 10
    )
    {
        var request = new GetReviewsRequest
        {
            ServiceId = serviceId,
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
    /// Gets reviews for a specific hospital
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByHospitalAsync(
        Guid hospitalId,
        int page = 1,
        int pageSize = 10,
        int? minRating = null,
        int? maxRating = null
    )
    {
        var request = new GetReviewsRequest
        {
            HospitalId = hospitalId,
            Page = page,
            PageSize = pageSize,
            MinRating = minRating,
            MaxRating = maxRating,
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
    /// Gets the average rating for a service
    /// </summary>
    public async Task<double> GetAverageRatingByServiceAsync(Guid serviceId)
    {
        return await GetAverageRatingAsync("serviceId", serviceId.ToString(), "SERVICE");
    }

    /// <summary>
    /// Gets the average rating for a hospital
    /// </summary>
    public async Task<double> GetAverageRatingByHospitalAsync(Guid hospitalId)
    {
        return await GetAverageRatingByHospitalIdAsync(hospitalId);
    }

    /// <summary>
    /// Generic method to get average rating for any target type
    /// </summary>
    private async Task<double> GetAverageRatingAsync(
        string targetIdField,
        string targetIdValue,
        string targetType
    )
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument { { targetIdField, targetIdValue }, { "targetType", targetType } }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (result != null && result.Contains(AverageRatingField))
        {
            return result[AverageRatingField].ToDouble();
        }

        return 0.0;
    }

    /// <summary>
    /// Generic method to get average rating by a single field (without targetType filter)
    /// Used for hospital reviews where we filter by hospitalId only
    /// </summary>
    private async Task<double> GetAverageRatingByFieldAsync(string fieldName, string fieldValue)
    {
        var matchStage = new BsonDocument("$match", new BsonDocument { { fieldName, fieldValue } });

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (result != null && result.Contains(AverageRatingField))
        {
            return result[AverageRatingField].ToDouble();
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
    /// Gets the total count of reviews for a service
    /// </summary>
    public async Task<long> GetReviewCountByServiceAsync(Guid serviceId)
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.ServiceId, serviceId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.SERVICE)
        );
        return await _reviews.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Gets the total count of reviews for a hospital
    /// </summary>
    public async Task<long> GetReviewCountByHospitalAsync(Guid hospitalId)
    {
        var filter = Builders<ReviewEntity>.Filter.Eq(r => r.HospitalId, hospitalId);
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
    /// Gets comprehensive statistics for a service
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetServiceStatisticsAsync(Guid serviceId)
    {
        return await GetStatisticsAsync("serviceId", serviceId.ToString(), "SERVICE", serviceId);
    }

    /// <summary>
    /// Gets comprehensive statistics for a hospital
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetHospitalStatisticsAsync(Guid hospitalId)
    {
        return await GetStatisticsByHospitalIdAsync(hospitalId);
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a doctor (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatisticsAsync(
        Guid doctorId
    )
    {
        return await GetDetailedStatisticsAsync(
            "doctorId",
            doctorId.ToString(),
            "DOCTOR",
            doctorId
        );
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a service (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetServiceDetailedStatisticsAsync(
        Guid serviceId
    )
    {
        return await GetDetailedStatisticsAsync(
            "serviceId",
            serviceId.ToString(),
            "SERVICE",
            serviceId
        );
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a hospital (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetHospitalDetailedStatisticsAsync(
        Guid hospitalId
    )
    {
        return await GetDetailedStatisticsByHospitalIdAsync(hospitalId);
    }

    /// <summary>
    /// Generic method to get optimized statistics for any target type (without rating distribution for batch)
    /// </summary>
    private async Task<ReviewStatisticsResponse> GetStatisticsAsync(
        string targetIdField,
        string targetIdValue,
        string targetType,
        Guid targetId
    )
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument { { targetIdField, targetIdValue }, { "targetType", targetType } }
        );

        return await ProcessStatisticsAggregationAsync(matchStage, targetId);
    }

    /// <summary>
    /// Generic method to get statistics by a single field (without targetType filter)
    /// Used for hospital reviews where we filter by hospitalId only
    /// </summary>
    private async Task<ReviewStatisticsResponse> GetStatisticsByFieldAsync(
        string fieldName,
        string fieldValue,
        Guid targetId
    )
    {
        var matchStage = new BsonDocument("$match", new BsonDocument { { fieldName, fieldValue } });

        return await ProcessStatisticsAggregationAsync(matchStage, targetId);
    }

    /// <summary>
    /// Helper method to process statistics aggregation pipeline
    /// Eliminates code duplication between different statistics methods
    /// </summary>
    private async Task<ReviewStatisticsResponse> ProcessStatisticsAggregationAsync(
        BsonDocument matchStage,
        Guid targetId
    )
    {
        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                { TotalReviewsField, new BsonDocument("$sum", 1) },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (
            result == null
            || !result.Contains(TotalReviewsField)
            || result[TotalReviewsField].ToInt64() == 0
        )
        {
            return new ReviewStatisticsResponse
            {
                TargetId = targetId,
                AverageRating = 0.0,
                TotalReviews = 0,
            };
        }

        var averageRating = result.Contains(AverageRatingField)
            ? result[AverageRatingField].ToDouble()
            : 0.0;
        var totalReviews = result[TotalReviewsField].ToInt64();

        return new ReviewStatisticsResponse
        {
            TargetId = targetId,
            AverageRating = Math.Round(averageRating, 2),
            TotalReviews = totalReviews,
        };
    }

    /// <summary>
    /// Generic method to get detailed statistics with rating distribution (for single endpoints)
    /// </summary>
    private async Task<ReviewDetailedStatisticsResponse> GetDetailedStatisticsAsync(
        string targetIdField,
        string targetIdValue,
        string targetType,
        Guid targetId
    )
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument { { targetIdField, targetIdValue }, { "targetType", targetType } }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                { TotalReviewsField, new BsonDocument("$sum", 1) },
                { RatingDistributionField, new BsonDocument("$push", "$rating") },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (
            result == null
            || !result.Contains(TotalReviewsField)
            || result[TotalReviewsField].ToInt64() == 0
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

        var averageRating = result.Contains(AverageRatingField)
            ? result[AverageRatingField].ToDouble()
            : 0.0;
        var totalReviews = result[TotalReviewsField].ToInt64();

        // Calculate rating distribution
        var ratings = result[RatingDistributionField].AsBsonArray.Select(r => r.ToInt32()).ToList();
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
        List<Guid> doctorIds,
        Guid? hospitalId = null
    )
    {
        var doctorIdsStrings = doctorIds.Select(id => id.ToString()).ToList();

        var matchConditions = new BsonDocument
        {
            { "doctorId", new BsonDocument("$in", new BsonArray(doctorIdsStrings)) },
            { "targetType", "DOCTOR" },
        };

        // Add hospital filter if provided
        if (hospitalId.HasValue)
        {
            matchConditions.Add("hospitalId", hospitalId.Value.ToString());
        }

        var matchStage = new BsonDocument("$match", matchConditions);

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", "$doctorId" },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                { TotalReviewsField, new BsonDocument("$sum", 1) },
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

                var averageRating = result.Contains(AverageRatingField)
                    ? result[AverageRatingField].ToDouble()
                    : 0.0;
                var totalReviews = result[TotalReviewsField].ToInt64();

                response.DoctorStatistics[doctorId] = new ReviewStatisticsResponse
                {
                    TargetId = doctorId,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews,
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
                    TotalReviews = 0,
                };
            }
        }

        return response;
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple services in a single query
    /// </summary>
    public async Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(
        List<Guid> serviceIds,
        Guid? hospitalId = null
    )
    {
        var serviceIdsStrings = serviceIds.Select(id => id.ToString()).ToList();

        var matchConditions = new BsonDocument
        {
            { "serviceId", new BsonDocument("$in", new BsonArray(serviceIdsStrings)) },
            { "targetType", "SERVICE" },
        };

        // Add hospital filter if provided
        if (hospitalId.HasValue)
        {
            matchConditions.Add("hospitalId", hospitalId.Value.ToString());
        }

        var matchStage = new BsonDocument("$match", matchConditions);

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", "$serviceId" },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                { TotalReviewsField, new BsonDocument("$sum", 1) },
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

                var averageRating = result.Contains(AverageRatingField)
                    ? result[AverageRatingField].ToDouble()
                    : 0.0;
                var totalReviews = result[TotalReviewsField].ToInt64();

                response.ServiceStatistics[serviceId] = new ReviewStatisticsResponse
                {
                    TargetId = serviceId,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews,
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
                    TotalReviews = 0,
                };
            }
        }

        return response;
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple hospitals in a single query
    /// </summary>
    public async Task<BatchHospitalsStatisticsResponse> GetBatchHospitalsStatisticsAsync(
        List<Guid> hospitalIds
    )
    {
        var hospitalIdsStrings = hospitalIds.Select(id => id.ToString()).ToList();

        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument
            {
                { "hospitalId", new BsonDocument("$in", new BsonArray(hospitalIdsStrings)) },
            }
        );

        var groupStage = new BsonDocument(
            "$group",
            new BsonDocument
            {
                { "_id", "$hospitalId" },
                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                { TotalReviewsField, new BsonDocument("$sum", 1) },
            }
        );

        var pipeline = new[] { matchStage, groupStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var results = await cursor.ToListAsync();

        var response = new BatchHospitalsStatisticsResponse();

        // Create a set of hospitals that have reviews
        var foundHospitalIds = new HashSet<Guid>();

        // Add hospitals that have reviews
        foreach (var result in results)
        {
            var hospitalIdString = result["_id"].AsString;
            if (Guid.TryParse(hospitalIdString, out var hospitalId))
            {
                foundHospitalIds.Add(hospitalId);

                var averageRating = result.Contains(AverageRatingField)
                    ? result[AverageRatingField].ToDouble()
                    : 0.0;
                var totalReviews = result[TotalReviewsField].ToInt64();

                response.HospitalStatistics[hospitalId] = new ReviewStatisticsResponse
                {
                    TargetId = hospitalId,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews,
                };
            }
        }

        // Add hospitals that don't have reviews with 0 values (Fix S3267: Use LINQ Where)
        foreach (var hospitalId in hospitalIds.Where(id => !foundHospitalIds.Contains(id)))
        {
            response.HospitalStatistics[hospitalId] = new ReviewStatisticsResponse
            {
                TargetId = hospitalId,
                AverageRating = 0.0,
                TotalReviews = 0,
            };
        }

        return response;
    }

    /// <summary>
    /// Helper method to get average rating by hospital ID
    /// </summary>
    private async Task<double> GetAverageRatingByHospitalIdAsync(Guid hospitalId)
    {
        return await GetAverageRatingByFieldAsync("hospitalId", hospitalId.ToString());
    }

    /// <summary>
    /// Helper method to get statistics by hospital ID
    /// </summary>
    private async Task<ReviewStatisticsResponse> GetStatisticsByHospitalIdAsync(Guid hospitalId)
    {
        return await GetStatisticsByFieldAsync("hospitalId", hospitalId.ToString(), hospitalId);
    }

    /// <summary>
    /// Helper method to get detailed statistics by hospital ID
    /// </summary>
    private async Task<ReviewDetailedStatisticsResponse> GetDetailedStatisticsByHospitalIdAsync(
        Guid hospitalId
    )
    {
        var matchStage = new BsonDocument(
            "$match",
            new BsonDocument { { "hospitalId", hospitalId.ToString() } }
        );

        var facetStage = new BsonDocument(
            "$facet",
            new BsonDocument
            {
                {
                    "statistics",
                    new BsonArray
                    {
                        new BsonDocument(
                            "$group",
                            new BsonDocument
                            {
                                { "_id", BsonNull.Value },
                                { AverageRatingField, new BsonDocument("$avg", "$rating") },
                                { TotalReviewsField, new BsonDocument("$sum", 1) },
                            }
                        ),
                    }
                },
                {
                    RatingDistributionField,
                    new BsonArray
                    {
                        new BsonDocument(
                            "$group",
                            new BsonDocument
                            {
                                { "_id", "$rating" },
                                { "count", new BsonDocument("$sum", 1) },
                            }
                        ),
                        new BsonDocument("$sort", new BsonDocument("_id", 1)),
                    }
                },
            }
        );

        var pipeline = new[] { matchStage, facetStage };

        var cursor = await _reviews.AggregateAsync<BsonDocument>(pipeline);
        var result = await cursor.FirstOrDefaultAsync();

        if (result == null)
        {
            return new ReviewDetailedStatisticsResponse
            {
                TargetId = hospitalId,
                AverageRating = 0.0,
                TotalReviews = 0,
                RatingDistribution = new Dictionary<int, long>
                {
                    { 1, 0 },
                    { 2, 0 },
                    { 3, 0 },
                    { 4, 0 },
                    { 5, 0 },
                },
            };
        }

        var statistics = result["statistics"].AsBsonArray;
        var ratingDistributionArray = result[RatingDistributionField].AsBsonArray;

        double averageRating = 0.0;
        long totalReviews = 0;

        if (statistics.Count > 0)
        {
            var statsDoc = statistics[0].AsBsonDocument;
            averageRating = statsDoc.Contains(AverageRatingField)
                ? statsDoc[AverageRatingField].ToDouble()
                : 0.0;
            totalReviews = statsDoc[TotalReviewsField].ToInt64();
        }

        var ratingDistribution = new Dictionary<int, long>
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 },
        };

        // Fix S3267: Use LINQ Select instead of foreach loop
        foreach (var doc in ratingDistributionArray.Select(item => item.AsBsonDocument))
        {
            var rating = doc["_id"].ToInt32();
            var count = doc["count"].ToInt64();
            ratingDistribution[rating] = count;
        }

        return new ReviewDetailedStatisticsResponse
        {
            TargetId = hospitalId,
            AverageRating = Math.Round(averageRating, 2),
            TotalReviews = totalReviews,
            RatingDistribution = ratingDistribution,
        };
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
    /// Checks if a patient has already reviewed a specific service
    /// </summary>
    public async Task<ReviewEntity?> GetExistingServiceReviewAsync(Guid patientId, Guid serviceId)
    {
        var filter = Builders<ReviewEntity>.Filter.And(
            Builders<ReviewEntity>.Filter.Eq(r => r.PatientId, patientId),
            Builders<ReviewEntity>.Filter.Eq(r => r.ServiceId, serviceId),
            Builders<ReviewEntity>.Filter.Eq(r => r.TargetType, TargetType.SERVICE)
        );

        return await _reviews.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets high-quality reviews from across the platform for testimonial display
    /// Fetches reviews with high ratings (4-5 stars) from all sources
    /// </summary>
    public async Task<PagedReviewsResponse> GetTestimonialReviewsAsync(
        int page = 1,
        int pageSize = 20,
        int minRating = 4
    )
    {
        // Build filter for high-quality reviews
        var filter = Builders<ReviewEntity>.Filter.Gte(r => r.Rating, minRating);

        // Sort by created date descending to get recent reviews
        var sort = Builders<ReviewEntity>.Sort.Descending(r => r.CreatedAt);

        // Calculate skip
        var skip = (page - 1) * pageSize;

        // Get total count
        var totalCount = await _reviews.CountDocumentsAsync(filter);

        // Get reviews
        var reviews = await _reviews
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var hasNextPage = page < totalPages;

        return new PagedReviewsResponse
        {
            Reviews = _mapper.Map<List<ReviewResponse>>(reviews),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = hasNextPage,
        };
    }
}
