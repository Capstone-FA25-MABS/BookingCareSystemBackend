using Grpc.Core;
using AutoMapper;
using BookingCare.Services.Review.Services.Interfaces;

namespace BookingCare.Services.Review.Grpc.Services;

/// <summary>
/// gRPC service implementation for Review operations
/// </summary>
public class ReviewGrpcService : ReviewService.ReviewServiceBase
{
    private readonly IReviewService _reviewService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReviewGrpcService> _logger;

    public ReviewGrpcService(
        IReviewService reviewService,
        IMapper mapper,
        ILogger<ReviewGrpcService> logger)
    {
        _reviewService = reviewService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for gRPC
    /// </summary>
    public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC Health check called");

        return Task.FromResult(new HealthCheckResponse
        {
            Status = "Healthy",
            Service = "Review gRPC Service",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a single doctor
    /// </summary>
    public override async Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatistics(GetDoctorStatisticsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting detailed doctor statistics for ID: {DoctorId}", request.DoctorId);

        try
        {
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var statistics = await _reviewService.GetDoctorDetailedStatisticsAsync(doctorId);
            return MapToGrpcDetailedStatistics(statistics);
            // TargetType no longer needed - clients can infer DOCTOR from endpoint
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed doctor statistics for ID: {DoctorId}", request.DoctorId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a single clinic service
    /// </summary>
    public override async Task<ReviewDetailedStatisticsResponse> GetServiceDetailedStatistics(GetServiceStatisticsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting detailed service statistics for ID: {ServiceId}", request.ServiceId);

        try
        {
            if (!Guid.TryParse(request.ServiceId, out var serviceId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service ID format"));
            }

            var statistics = await _reviewService.GetClinicServiceDetailedStatisticsAsync(serviceId);
            return MapToGrpcDetailedStatistics(statistics);
            // TargetType no longer needed - clients can infer SERVICE from endpoint
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed service statistics for ID: {ServiceId}", request.ServiceId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Gets statistics for multiple doctors in batch
    /// </summary>
    public override async Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatistics(BatchDoctorsStatisticsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting batch doctor statistics for {Count} doctors", request.DoctorIds.Count);

        try
        {
            var doctorIds = new List<Guid>();
            foreach (var idString in request.DoctorIds)
            {
                if (Guid.TryParse(idString, out var id))
                {
                    doctorIds.Add(id);
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid doctor ID format: {idString}"));
                }
            }

            // Create DTO request
            var dtoRequest = new Models.DTOs.BatchDoctorsStatisticsRequest();
            dtoRequest.DoctorIds.AddRange(doctorIds);

            var result = await _reviewService.GetBatchDoctorsStatisticsAsync(dtoRequest);

            var response = new Grpc.BatchDoctorsStatisticsResponse();

            // Map doctor statistics
            foreach (var kvp in result.DoctorStatistics)
            {
                response.DoctorStatistics[kvp.Key.ToString()] = MapToGrpcStatistics(kvp.Value);
                // No need to set TargetType - removed from proto
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch doctor statistics");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Gets statistics for multiple clinic services in batch
    /// </summary>
    public override async Task<BatchServicesStatisticsResponse> GetBatchServicesStatistics(BatchServicesStatisticsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting batch service statistics for {Count} services", request.ServiceIds.Count);

        try
        {
            var serviceIds = new List<Guid>();
            foreach (var idString in request.ServiceIds)
            {
                if (Guid.TryParse(idString, out var id))
                {
                    serviceIds.Add(id);
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid service ID format: {idString}"));
                }
            }

            // Create DTO request
            var dtoRequest = new Models.DTOs.BatchServicesStatisticsRequest();
            dtoRequest.ServiceIds.AddRange(serviceIds);

            var result = await _reviewService.GetBatchServicesStatisticsAsync(dtoRequest);

            var response = new Grpc.BatchServicesStatisticsResponse();

            // Map service statistics
            foreach (var kvp in result.ServiceStatistics)
            {
                response.ServiceStatistics[kvp.Key.ToString()] = MapToGrpcStatistics(kvp.Value);
                // No need to set TargetType - removed from proto
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch service statistics");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Gets paginated reviews for a doctor
    /// </summary>
    public override async Task<PagedReviewsResponse> GetDoctorReviews(GetDoctorReviewsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting doctor reviews for ID: {DoctorId}", request.DoctorId);

        try
        {
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            var reviews = await _reviewService.GetReviewsByDoctorAsync(doctorId, request.Page, request.PageSize);
            return MapToGrpcPagedReviews(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor reviews for ID: {DoctorId}", request.DoctorId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Gets paginated reviews for a clinic service
    /// </summary>
    public override async Task<PagedReviewsResponse> GetServiceReviews(GetServiceReviewsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting service reviews for ID: {ServiceId}", request.ServiceId);

        try
        {
            if (!Guid.TryParse(request.ServiceId, out var serviceId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid service ID format"));
            }

            var reviews = await _reviewService.GetReviewsByClinicServiceAsync(serviceId, request.Page, request.PageSize);
            return MapToGrpcPagedReviews(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service reviews for ID: {ServiceId}", request.ServiceId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps to optimized gRPC statistics (without rating distribution for batch operations)
    /// </summary>
    private static ReviewStatisticsResponse MapToGrpcStatistics(Models.DTOs.ReviewStatisticsResponse dto)
    {
        return new ReviewStatisticsResponse
        {
            TargetId = dto.TargetId.ToString(),
            AverageRating = dto.AverageRating,
            TotalReviews = dto.TotalReviews
            // TargetType removed from proto - clients can infer from endpoint context
        };
    }

    /// <summary>
    /// Maps to detailed gRPC statistics (with rating distribution for single endpoints)
    /// </summary>
    private static ReviewDetailedStatisticsResponse MapToGrpcDetailedStatistics(Models.DTOs.ReviewDetailedStatisticsResponse dto)
    {
        var response = new ReviewDetailedStatisticsResponse
        {
            TargetId = dto.TargetId.ToString(),
            AverageRating = dto.AverageRating,
            TotalReviews = dto.TotalReviews
            // TargetType removed from proto - clients can infer from endpoint context
        };

        // Map rating distribution
        foreach (var kvp in dto.RatingDistribution)
        {
            response.RatingDistribution[kvp.Key] = kvp.Value;
        }

        return response;
    }

    private static PagedReviewsResponse MapToGrpcPagedReviews(Models.DTOs.PagedReviewsResponse dto)
    {
        var response = new PagedReviewsResponse
        {
            TotalCount = dto.TotalCount,
            Page = dto.Page,
            PageSize = dto.PageSize,
            TotalPages = dto.TotalPages,
            HasNextPage = dto.HasNextPage,
            HasPreviousPage = dto.HasPreviousPage
        };

        // Map reviews
        foreach (var review in dto.Reviews)
        {
            response.Reviews.Add(MapToGrpcReview(review));
        }

        return response;
    }

    private static Grpc.ReviewResponse MapToGrpcReview(Models.DTOs.ReviewResponse dto)
    {
        var response = new Grpc.ReviewResponse
        {
            Id = dto.Id,
            PatientId = dto.PatientId.ToString(),
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = ((DateTimeOffset)dto.CreatedAt).ToUnixTimeSeconds(),
            UpdatedAt = ((DateTimeOffset)dto.UpdatedAt).ToUnixTimeSeconds()
            // TargetType removed from proto - clients can infer from doctor_id vs clinic_service_id
        };

        // Set optional fields
        if (dto.DoctorId.HasValue)
        {
            response.DoctorId = dto.DoctorId.Value.ToString();
        }

        if (dto.ClinicServiceId.HasValue)
        {
            response.ClinicServiceId = dto.ClinicServiceId.Value.ToString();
        }

        // Map replies
        foreach (var reply in dto.Replies)
        {
            response.Replies.Add(new Grpc.ReplyResponse
            {
                Id = reply.Id,
                AuthorId = reply.AuthorId.ToString(),
                Content = reply.Content,
                CreatedAt = ((DateTimeOffset)reply.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)reply.UpdatedAt).ToUnixTimeSeconds()
            });
        }

        return response;
    }

    #endregion
}