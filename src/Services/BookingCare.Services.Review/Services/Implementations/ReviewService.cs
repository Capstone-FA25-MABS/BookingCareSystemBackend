using AutoMapper;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;
using BookingCare.Services.Review.Repositories.Interfaces;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.Review.Exceptions;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service implementation for Review operations
/// </summary>
public class ReviewService : BaseService, IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IMapper _mapper;

    public ReviewService(
        IReviewRepository reviewRepository,
        IMapper mapper,
        ILogger<ReviewService> logger) : base(logger)
    {
        _reviewRepository = reviewRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new review
    /// </summary>
    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating new review for patient: {PatientId}", null, request.PatientId);

            // ✅ ValidationFilter đã handle tất cả validation rồi, không cần manual validation nữa

            // Check for duplicate review (business rule)
            await CheckForDuplicateReviewAsync(request);

            // Create entity
            var reviewEntity = _mapper.Map<ReviewEntity>(request);
            var createdReview = await _reviewRepository.CreateAsync(reviewEntity);

            LogInfo("Review created successfully with ID: {ReviewId}", null, createdReview.Id);
            return _mapper.Map<ReviewResponse>(createdReview);
        }, "CreateReview");
    }

    /// <summary>
    /// Checks for duplicate review by the same patient for the same target
    /// </summary>
    private async Task CheckForDuplicateReviewAsync(CreateReviewRequest request)
    {
        ReviewEntity? existingReview = null;
        string targetName = "";

        if (request.TargetType == Enums.TargetType.DOCTOR && request.DoctorId.HasValue)
        {
            existingReview = await _reviewRepository.GetExistingDoctorReviewAsync(request.PatientId, request.DoctorId.Value);
            targetName = $"doctor {request.DoctorId.Value}";
        }
        else if (request.TargetType == Enums.TargetType.SERVICE && request.ClinicServiceId.HasValue)
        {
            existingReview = await _reviewRepository.GetExistingServiceReviewAsync(request.PatientId, request.ClinicServiceId.Value);
            targetName = $"service {request.ClinicServiceId.Value}";
        }

        if (existingReview != null)
        {
            LogWarning("Duplicate review attempt - Patient: {PatientId}, Target: {TargetName}, ExistingReview: {ExistingReviewId}",
                null, request.PatientId, targetName, existingReview.Id);

            throw new DuplicateReviewException(
                request.PatientId,
                request.DoctorId,
                request.ClinicServiceId,
                existingReview.Id,
                targetName
            );
        }
    }

    /// <summary>
    /// Updates an existing review
    /// </summary>
    public async Task<ReviewResponse> UpdateReviewAsync(UpdateReviewRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating review with ID: {ReviewId}", null, request.Id);

            // ✅ ValidationFilter đã handle tất cả validation rồi

            // Get existing review
            var existingReview = await _reviewRepository.GetByIdAsync(request.Id);
            if (existingReview == null)
            {
                throw new NotFoundException("Review", request.Id);
            }

            // Update only allowed fields
            existingReview.Rating = request.Rating;
            existingReview.Comment = request.Comment;
            existingReview.UpdatedAt = DateTime.UtcNow;

            var updatedReview = await _reviewRepository.UpdateAsync(existingReview);
            if (updatedReview == null)
            {
                throw new InvalidOperationException("Failed to update review");
            }

            LogInfo("Review updated successfully with ID: {ReviewId}", null, updatedReview.Id);
            return _mapper.Map<ReviewResponse>(updatedReview);
        }, "UpdateReview");
    }

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    public async Task<bool> DeleteReviewAsync(string id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting review with ID: {ReviewId}", null, id);

            // ✅ Basic validation still needed for simple parameters
            ValidateRequiredString(id, nameof(id));

            var result = await _reviewRepository.DeleteAsync(id);
            if (!result)
            {
                throw new NotFoundException("Review", id);
            }

            LogInfo("Review deleted successfully with ID: {ReviewId}", null, id);
            return result;
        }, "DeleteReview");
    }

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    public async Task<ReviewResponse?> GetReviewByIdAsync(string id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        return review != null ? _mapper.Map<ReviewResponse>(review) : null;
    }

    /// <summary>
    /// Gets reviews with filtering and pagination
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsAsync(GetReviewsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting reviews with filters - PatientId: {PatientId}, DoctorId: {DoctorId}, ClinicServiceId: {ClinicServiceId}",
                null, request.PatientId?.ToString() ?? "null", request.DoctorId?.ToString() ?? "null", request.ClinicServiceId?.ToString() ?? "null");

            // ✅ ValidationFilter đã handle tất cả validation rồi

            var result = await _reviewRepository.GetReviewsAsync(request);

            LogInfo("Retrieved {Count} reviews out of {Total} total", null, result.Reviews.Count, result.TotalCount);
            return result;
        }, "GetReviews");
    }

    /// <summary>
    /// Gets reviews for a specific doctor
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByDoctorAsync(Guid doctorId, int page = 1, int pageSize = 10)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _reviewRepository.GetReviewsByDoctorAsync(doctorId, page, pageSize);
    }

    /// <summary>
    /// Gets reviews for a specific clinic service
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByClinicServiceAsync(Guid clinicServiceId, int page = 1, int pageSize = 10)
    {
        ValidateGuid(clinicServiceId, nameof(clinicServiceId));
        return await _reviewRepository.GetReviewsByClinicServiceAsync(clinicServiceId, page, pageSize);
    }

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10)
    {
        ValidateGuid(patientId, nameof(patientId));
        return await _reviewRepository.GetReviewsByPatientAsync(patientId, page, pageSize);
    }

    /// <summary>
    /// Adds a reply to a review
    /// </summary>
    public async Task<ReviewResponse> AddReplyAsync(AddReplyRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Adding reply to review: {ReviewId} by author: {AuthorId}", null, request.ReviewId, request.AuthorId);

            // ✅ ValidationFilter đã handle tất cả validation rồi

            // Check if review exists
            var existingReview = await _reviewRepository.GetByIdAsync(request.ReviewId);
            if (existingReview == null)
            {
                throw new NotFoundException("Review", request.ReviewId);
            }

            // Create reply entity
            var replyEntity = _mapper.Map<ReplyEntity>(request);
            var updatedReview = await _reviewRepository.AddReplyAsync(request.ReviewId, replyEntity);

            if (updatedReview == null)
            {
                throw new InvalidOperationException("Failed to add reply to review");
            }

            LogInfo("Reply added successfully to review: {ReviewId}", null, request.ReviewId);
            return _mapper.Map<ReviewResponse>(updatedReview);
        }, "AddReply");
    }

    /// <summary>
    /// Removes a reply from a review
    /// </summary>
    public async Task<ReviewResponse> RemoveReplyAsync(string reviewId, string replyId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Removing reply: {ReplyId} from review: {ReviewId}", null, replyId, reviewId);

            ValidateRequiredString(reviewId, nameof(reviewId));
            ValidateRequiredString(replyId, nameof(replyId));

            // Check if review exists
            var existingReview = await _reviewRepository.GetByIdAsync(reviewId);
            if (existingReview == null)
            {
                throw new NotFoundException("Review", reviewId);
            }

            // Check if reply exists
            var replyExists = existingReview.Replies.Any(r => r.Id == replyId);
            if (!replyExists)
            {
                throw new NotFoundException("Reply", replyId);
            }

            var updatedReview = await _reviewRepository.RemoveReplyAsync(reviewId, replyId);
            if (updatedReview == null)
            {
                throw new InvalidOperationException("Failed to remove reply from review");
            }

            LogInfo("Reply removed successfully from review: {ReviewId}", null, reviewId);
            return _mapper.Map<ReviewResponse>(updatedReview);
        }, "RemoveReply");
    }

    /// <summary>
    /// Updates a reply in a review
    /// </summary>
    public async Task<ReviewResponse> UpdateReplyAsync(UpdateReplyRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating reply: {ReplyId} in review: {ReviewId}", null, request.ReplyId, request.ReviewId);

            // ✅ ValidationFilter đã handle tất cả validation rồi

            // Check if review exists
            var existingReview = await _reviewRepository.GetByIdAsync(request.ReviewId);
            if (existingReview == null)
            {
                throw new NotFoundException("Review", request.ReviewId);
            }

            // Check if reply exists
            var replyExists = existingReview.Replies.Any(r => r.Id == request.ReplyId);
            if (!replyExists)
            {
                throw new NotFoundException("Reply", request.ReplyId);
            }

            var updatedReview = await _reviewRepository.UpdateReplyAsync(request.ReviewId, request.ReplyId, request.Content);
            if (updatedReview == null)
            {
                throw new InvalidOperationException("Failed to update reply in review");
            }

            LogInfo("Reply updated successfully in review: {ReviewId}", null, request.ReviewId);
            return _mapper.Map<ReviewResponse>(updatedReview);
        }, "UpdateReply");
    }

    /// <summary>
    /// Gets the average rating for a doctor
    /// </summary>
    public async Task<double> GetAverageRatingByDoctorAsync(Guid doctorId)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _reviewRepository.GetAverageRatingByDoctorAsync(doctorId);
    }

    /// <summary>
    /// Gets the average rating for a clinic service
    /// </summary>
    public async Task<double> GetAverageRatingByClinicServiceAsync(Guid clinicServiceId)
    {
        ValidateGuid(clinicServiceId, nameof(clinicServiceId));
        return await _reviewRepository.GetAverageRatingByClinicServiceAsync(clinicServiceId);
    }

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    public async Task<long> GetReviewCountByDoctorAsync(Guid doctorId)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _reviewRepository.GetReviewCountByDoctorAsync(doctorId);
    }

    /// <summary>
    /// Gets the total count of reviews for a clinic service
    /// </summary>
    public async Task<long> GetReviewCountByClinicServiceAsync(Guid clinicServiceId)
    {
        ValidateGuid(clinicServiceId, nameof(clinicServiceId));
        return await _reviewRepository.GetReviewCountByClinicServiceAsync(clinicServiceId);
    }

    /// <summary>
    /// Gets optimized statistics for a doctor (used by batch operations)
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetDoctorStatisticsAsync(Guid doctorId)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _reviewRepository.GetDoctorStatisticsAsync(doctorId);
    }

    /// <summary>
    /// Gets optimized statistics for a clinic service (used by batch operations)
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetClinicServiceStatisticsAsync(Guid clinicServiceId)
    {
        ValidateGuid(clinicServiceId, nameof(clinicServiceId));
        return await _reviewRepository.GetClinicServiceStatisticsAsync(clinicServiceId);
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a doctor (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatisticsAsync(Guid doctorId)
    {
        ValidateGuid(doctorId, nameof(doctorId));
        return await _reviewRepository.GetDoctorDetailedStatisticsAsync(doctorId);
    }

    /// <summary>
    /// Gets detailed statistics with rating distribution for a clinic service (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetClinicServiceDetailedStatisticsAsync(Guid clinicServiceId)
    {
        ValidateGuid(clinicServiceId, nameof(clinicServiceId));
        return await _reviewRepository.GetClinicServiceDetailedStatisticsAsync(clinicServiceId);
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple doctors in a single request
    /// </summary>
    public async Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatisticsAsync(BatchDoctorsStatisticsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting batch statistics for {Count} doctors", null, request.DoctorIds.Count);

            // ✅ ValidationFilter đã handle tất cả validation rồi

            // Remove duplicates (business logic)
            var uniqueDoctorIds = request.DoctorIds.Distinct().ToList();

            var result = await _reviewRepository.GetBatchDoctorsStatisticsAsync(uniqueDoctorIds);

            LogInfo("Batch doctor statistics retrieved successfully for {Count} doctors", null, result.DoctorStatistics.Count);
            return result;
        }, "GetBatchDoctorsStatistics");
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple clinic services in a single request
    /// </summary>
    public async Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(BatchServicesStatisticsRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Getting batch statistics for {Count} services", null, request.ServiceIds.Count);

            // ✅ ValidationFilter đã handle tất cả validation rồi

            // Remove duplicates (business logic)
            var uniqueServiceIds = request.ServiceIds.Distinct().ToList();

            var result = await _reviewRepository.GetBatchServicesStatisticsAsync(uniqueServiceIds);

            LogInfo("Batch service statistics retrieved successfully for {Count} services", null, result.ServiceStatistics.Count);
            return result;
        }, "GetBatchServicesStatistics");
    }
}