using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Service interface for Review operations
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a new review
    /// </summary>
    /// <param name="request">The review creation request</param>
    /// <returns>The created review response</returns>
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);

    /// <summary>
    /// Updates an existing review
    /// </summary>
    /// <param name="request">The review update request</param>
    /// <returns>The updated review response</returns>
    Task<ReviewResponse> UpdateReviewAsync(UpdateReviewRequest request);

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    /// <param name="id">The ID of the review to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteReviewAsync(string id);

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    /// <param name="id">The ID of the review</param>
    /// <returns>The review response</returns>
    Task<ReviewResponse?> GetReviewByIdAsync(string id);

    /// <summary>
    /// Gets reviews with filtering and pagination
    /// </summary>
    /// <param name="request">The filter and pagination parameters</param>
    /// <returns>Paginated reviews</returns>
    Task<PagedReviewsResponse> GetReviewsAsync(GetReviewsRequest request);

    /// <summary>
    /// Gets reviews for a specific doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the doctor</returns>
    Task<PagedReviewsResponse> GetReviewsByDoctorAsync(
        Guid doctorId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Gets reviews for a specific service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the service</returns>
    Task<PagedReviewsResponse> GetReviewsByServiceAsync(
        Guid serviceId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews by the patient</returns>
    Task<PagedReviewsResponse> GetReviewsByPatientAsync(
        Guid patientId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Gets reviews for a specific hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the hospital</returns>
    Task<PagedReviewsResponse> GetReviewsByHospitalAsync(
        Guid hospitalId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Adds a reply to a review
    /// </summary>
    /// <param name="request">The reply request</param>
    /// <returns>The updated review response</returns>
    Task<ReviewResponse> AddReplyAsync(AddReplyRequest request);

    /// <summary>
    /// Removes a reply from a review
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="replyId">The reply ID to remove</param>
    /// <returns>The updated review response</returns>
    Task<ReviewResponse> RemoveReplyAsync(string reviewId, string replyId);

    /// <summary>
    /// Updates a reply in a review
    /// </summary>
    /// <param name="request">The update reply request</param>
    /// <returns>The updated review response</returns>
    Task<ReviewResponse> UpdateReplyAsync(UpdateReplyRequest request);

    /// <summary>
    /// Gets the average rating for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the average rating for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByServiceAsync(Guid serviceId);

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the total count of reviews for a service
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByServiceAsync(Guid serviceId);

    /// <summary>
    /// Gets optimized statistics for a doctor (used by batch operations)
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetDoctorStatisticsAsync(Guid doctorId);

    /// <summary>
    /// Gets optimized statistics for a service (used by batch operations)
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetServiceStatisticsAsync(Guid serviceId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a doctor (single endpoint)
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetDoctorDetailedStatisticsAsync(Guid doctorId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a service (single endpoint)
    /// </summary>
    /// <param name="serviceId">The service ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetServiceDetailedStatisticsAsync(Guid serviceId);

    /// <summary>
    /// Gets comprehensive statistics for multiple doctors in a single request
    /// </summary>
    /// <param name="request">Batch doctors statistics request</param>
    /// <returns>Batch statistics response for all doctors</returns>
    Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatisticsAsync(
        BatchDoctorsStatisticsRequest request
    );

    /// <summary>
    /// Gets comprehensive statistics for multiple services in a single request
    /// </summary>
    /// <param name="request">Batch services statistics request</param>
    /// <returns>Batch statistics response for all services</returns>
    Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(
        BatchServicesStatisticsRequest request
    );

    /// <summary>
    /// Gets the average rating for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Average rating</returns>
    Task<double> GetAverageRatingByHospitalAsync(Guid hospitalId);

    /// <summary>
    /// Gets the total count of reviews for a hospital
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Review count</returns>
    Task<long> GetReviewCountByHospitalAsync(Guid hospitalId);

    /// <summary>
    /// Gets optimized statistics for a hospital (used by batch operations)
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetHospitalStatisticsAsync(Guid hospitalId);

    /// <summary>
    /// Gets detailed statistics with rating distribution for a hospital (single endpoint)
    /// </summary>
    /// <param name="hospitalId">The hospital ID</param>
    /// <returns>Detailed review statistics including rating distribution</returns>
    Task<ReviewDetailedStatisticsResponse> GetHospitalDetailedStatisticsAsync(Guid hospitalId);

    /// <summary>
    /// Gets comprehensive statistics for multiple hospitals in a single request
    /// </summary>
    /// <param name="request">Batch hospitals statistics request</param>
    /// <returns>Batch statistics response for all hospitals</returns>
    Task<BatchHospitalsStatisticsResponse> GetBatchHospitalsStatisticsAsync(
        BatchHospitalsStatisticsRequest request
    );
}
