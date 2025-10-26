using AutoMapper;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;
using BookingCare.Services.Review.Repositories.Interfaces;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.Review.Exceptions;
using BookingCare.Services.Auth.Protos;
using Grpc.Core;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service for enriching account information from Auth service
/// </summary>
public class AccountEnrichmentService : BaseAuthEnrichmentService, IAccountEnrichmentService
{
    public AccountEnrichmentService(AuthService.AuthServiceClient authClient, ILogger<AccountEnrichmentService> logger)
   : base(authClient, logger)
    {
    }

    /// <summary>
    /// Gets account information for multiple account IDs
    /// </summary>
    /// <param name="accountIds">List of account IDs to fetch</param>
    /// <returns>Dictionary mapping account ID to AccountInfo</returns>
    public async Task<Dictionary<string, AccountInfo>> GetAccountDetailsAsync(List<string> accountIds)
    {
        return await GetAccountDetailsFromAuthServiceAsync(accountIds, "account enrichment");
    }
}

/// <summary>
/// Service implementation for Review operations
/// </summary>
public class ReviewService : BaseService, IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUserEnrichmentService _userEnrichmentService;
    private readonly IReplyEnrichmentService _replyEnrichmentService;
    private readonly IAppointmentValidationService _appointmentValidationService;
    private readonly IMapper _mapper;

    public ReviewService(
        IReviewRepository reviewRepository,
        IUserEnrichmentService userEnrichmentService,
        IReplyEnrichmentService replyEnrichmentService,
        IAppointmentValidationService appointmentValidationService,
        IMapper mapper,
        ILogger<ReviewService> logger) : base(logger)
    {
        _reviewRepository = reviewRepository;
        _userEnrichmentService = userEnrichmentService;
        _replyEnrichmentService = replyEnrichmentService;
        _appointmentValidationService = appointmentValidationService;
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

            // NEW: Validate appointment history before allowing review creation
            await ValidateAppointmentHistoryAsync(request);

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
    /// NEW: Validates that patient has completed appointment with the target before allowing review
    /// </summary>
    /// <param name="request">Create review request</param>
    private async Task ValidateAppointmentHistoryAsync(CreateReviewRequest request)
    {
        AppointmentHistoryValidationResult validationResult;

        if (request.TargetType == Enums.TargetType.DOCTOR && request.DoctorId.HasValue)
        {
            // Validate appointment history with doctor
            validationResult = await _appointmentValidationService.HasCompletedAppointmentWithDoctorAsync(
                request.PatientId, request.DoctorId.Value);

            if (!validationResult.HasCompletedAppointment)
            {
                LogWarning("Patient {PatientId} attempted to review doctor {DoctorId} without completed appointment",
                    null, request.PatientId, request.DoctorId.Value);

                throw new Exceptions.NoAppointmentHistoryException(
                    request.PatientId, request.DoctorId, null, "DOCTOR");
            }

            LogInfo("Patient {PatientId} has {Count} completed appointments with doctor {DoctorId} - review allowed",
                null, request.PatientId, validationResult.TotalCompletedAppointments, request.DoctorId.Value);
        }
        else if (request.TargetType == Enums.TargetType.SERVICE && request.ServiceId.HasValue)
        {
            // Validate appointment history with service
            validationResult = await _appointmentValidationService.HasCompletedAppointmentWithServiceAsync(
                request.PatientId, request.ServiceId.Value);

            if (!validationResult.HasCompletedAppointment)
            {
                LogWarning("Patient {PatientId} attempted to review service {ServiceId} without completed appointment",
                    null, request.PatientId, request.ServiceId.Value);

                throw new Exceptions.NoAppointmentHistoryException(
                    request.PatientId, null, request.ServiceId, "SERVICE");
            }

            LogInfo("Patient {PatientId} has {Count} completed appointments with service {ServiceId} - review allowed",
                null, request.PatientId, validationResult.TotalCompletedAppointments, request.ServiceId.Value);
        }
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
        else if (request.TargetType == Enums.TargetType.SERVICE && request.ServiceId.HasValue)
        {
            existingReview = await _reviewRepository.GetExistingServiceReviewAsync(request.PatientId, request.ServiceId.Value);
            targetName = $"service {request.ServiceId.Value}";
        }

        if (existingReview != null)
        {
            LogWarning("Duplicate review attempt - Patient: {PatientId}, Target: {TargetName}, ExistingReview: {ExistingReviewId}",
                null, request.PatientId, targetName, existingReview.Id);

            throw new DuplicateReviewException(
                request.PatientId,
                request.DoctorId,
                request.ServiceId,
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
            LogInfo("Getting reviews with filters - PatientId: {PatientId}, DoctorId: {DoctorId}, ServiceId: {ServiceId}",
                null, request.PatientId?.ToString() ?? "null", request.DoctorId?.ToString() ?? "null", request.ServiceId?.ToString() ?? "null");

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
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(doctorId, nameof(doctorId));

            LogInfo("Getting reviews for doctor: {DoctorId} with optimized enrichment", null, doctorId);

            // Get reviews from repository
            var pagedReviews = await _reviewRepository.GetReviewsByDoctorAsync(doctorId, page, pageSize);

            // Enrich with optimized information (UserService for patients, AuthService for reply authors)
            await EnrichReviewsWithOptimizedInfo(pagedReviews.Reviews);

            LogInfo("Retrieved {Count} reviews for doctor {DoctorId} with optimized enrichment",
                null, pagedReviews.Reviews.Count, doctorId);

            return pagedReviews;
        }, "GetReviewsByDoctorWithOptimizedEnrichment");
    }

    /// <summary>
    /// Gets reviews for a specific service
    /// </summary>
    public async Task<PagedReviewsResponse> GetReviewsByServiceAsync(Guid serviceId, int page = 1, int pageSize = 10)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(serviceId, nameof(serviceId));

            LogInfo("Getting reviews for service: {ServiceId} with optimized enrichment", null, serviceId);

            // Get reviews from repository
            var pagedReviews = await _reviewRepository.GetReviewsByServiceAsync(serviceId, page, pageSize);

            // Enrich with optimized information (UserService for patients, AuthService for reply authors)
            await EnrichReviewsWithOptimizedInfo(pagedReviews.Reviews);

            LogInfo("Retrieved {Count} reviews for service {ServiceId} with optimized enrichment",
                null, pagedReviews.Reviews.Count, serviceId);

            return pagedReviews;
        }, "GetReviewsByServiceWithOptimizedEnrichment");
    }

    /// <summary>
    /// Enriches reviews with optimized approach:
    /// - Patient info from UserService (direct user data)
    /// - Reply author info from AuthService (account data with roles)
    /// </summary>
    private async Task EnrichReviewsWithOptimizedInfo(List<ReviewResponse> reviews)
    {
        if (!reviews.Any())
        {
            return;
        }

        // Collect IDs for enrichment
        var (patientIds, replyAuthorIds) = CollectEnrichmentIds(reviews);

        LogInfo("Optimized enrichment: {PatientCount} patients via UserService, {ReplyAuthorCount} reply authors via AuthService",
          null, patientIds.Count, replyAuthorIds.Count);

        // Fetch enrichment data in parallel
        var (patientInfoDict, replyAuthorInfoDict) = await FetchEnrichmentDataAsync(patientIds, replyAuthorIds);

        // Apply enrichment to reviews
        ApplyEnrichmentToReviews(reviews, patientInfoDict, replyAuthorInfoDict);

        LogInfo("Successfully enriched {ReviewCount} reviews with optimized approach",
       null, reviews.Count);
    }

    /// <summary>
    /// Collects patient IDs and reply author IDs from reviews for enrichment
    /// </summary>
    private static (HashSet<string> patientIds, HashSet<string> replyAuthorIds) CollectEnrichmentIds(List<ReviewResponse> reviews)
    {
        var patientIds = new HashSet<string>();
        var replyAuthorIds = new HashSet<string>();

        foreach (var review in reviews)
        {
            patientIds.Add(review.PatientId.ToString());

            foreach (var reply in review.Replies)
            {
                replyAuthorIds.Add(reply.AuthorId.ToString());
            }
        }

        return (patientIds, replyAuthorIds);
    }

    /// <summary>
    /// Fetches enrichment data from UserService and AuthService in parallel
    /// </summary>
    private async Task<(Dictionary<string, UserInfo> patientInfoDict, Dictionary<string, AccountInfo> replyAuthorInfoDict)>
        FetchEnrichmentDataAsync(HashSet<string> patientIds, HashSet<string> replyAuthorIds)
    {
        // Parallel fetch from both services for better performance
        var patientInfoTask = patientIds.Any()
            ? _userEnrichmentService.GetUsersInfoAsync(patientIds.ToList())
            : Task.FromResult(new Dictionary<string, UserInfo>());

        var replyAuthorInfoTask = replyAuthorIds.Any()
            ? _replyEnrichmentService.GetReplyAuthorsInfoAsync(replyAuthorIds.ToList())
     : Task.FromResult(new Dictionary<string, AccountInfo>());

        await Task.WhenAll(patientInfoTask, replyAuthorInfoTask);

        return (patientInfoTask.Result, replyAuthorInfoTask.Result);
    }

    /// <summary>
    /// Applies enrichment data to reviews and their replies
    /// </summary>
    private static void ApplyEnrichmentToReviews(
        List<ReviewResponse> reviews,
        Dictionary<string, UserInfo> patientInfoDict,
 Dictionary<string, AccountInfo> replyAuthorInfoDict)
    {
        foreach (var review in reviews)
        {
            // Enrich patient info from UserService
            EnrichPatientInfo(review, patientInfoDict);

            // Enrich reply author info from AuthService
            EnrichReplyAuthorInfo(review.Replies, replyAuthorInfoDict);
        }
    }

    /// <summary>
    /// Enriches patient information for a single review
    /// </summary>
    private static void EnrichPatientInfo(ReviewResponse review, Dictionary<string, UserInfo> patientInfoDict)
    {
        var patientKey = review.PatientId.ToString();
        if (patientInfoDict.TryGetValue(patientKey, out var patientInfo))
        {
            review.PatientInfo = patientInfo;
        }
        else
        {
            // Fallback for missing patient info
            review.PatientInfo = new UserInfo
            {
                UserId = patientKey,
                Found = false
            };
        }
    }

    /// <summary>
    /// Enriches reply author information for all replies
    /// </summary>
    private static void EnrichReplyAuthorInfo(List<ReplyResponse> replies, Dictionary<string, AccountInfo> replyAuthorInfoDict)
    {
        foreach (var reply in replies)
        {
            var authorKey = reply.AuthorId.ToString();
            if (replyAuthorInfoDict.TryGetValue(authorKey, out var authorInfo))
            {
                reply.AuthorInfo = authorInfo;
            }
            else
            {
                // Fallback for missing author info
                reply.AuthorInfo = new AccountInfo
                {
                    AccountId = authorKey,
                    Found = false
                };
            }
        }
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
    /// Gets the average rating for a service
    /// </summary>
    public async Task<double> GetAverageRatingByServiceAsync(Guid serviceId)
    {
        ValidateGuid(serviceId, nameof(serviceId));
        return await _reviewRepository.GetAverageRatingByServiceAsync(serviceId);
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
    /// Gets the total count of reviews for a service
    /// </summary>
    public async Task<long> GetReviewCountByServiceAsync(Guid serviceId)
    {
        ValidateGuid(serviceId, nameof(serviceId));
        return await _reviewRepository.GetReviewCountByServiceAsync(serviceId);
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
    /// Gets optimized statistics for a service (used by batch operations)
    /// </summary>
    public async Task<ReviewStatisticsResponse> GetServiceStatisticsAsync(Guid serviceId)
    {
        ValidateGuid(serviceId, nameof(serviceId));
        return await _reviewRepository.GetServiceStatisticsAsync(serviceId);
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
    /// Gets detailed statistics with rating distribution for a service (single endpoint)
    /// </summary>
    public async Task<ReviewDetailedStatisticsResponse> GetServiceDetailedStatisticsAsync(Guid serviceId)
    {
        ValidateGuid(serviceId, nameof(serviceId));
        return await _reviewRepository.GetServiceDetailedStatisticsAsync(serviceId);
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