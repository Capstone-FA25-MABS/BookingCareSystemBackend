using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;

namespace BookingCare.Services.Review.Repositories.Interfaces;

/// <summary>
/// Repository interface for Review operations
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Creates a new review
    /// </summary>
    /// <param name="review">The review to create</param>
    /// <returns>The created review</returns>
    Task<ReviewEntity> CreateAsync(ReviewEntity review);

    /// <summary>
    /// Updates an existing review
    /// </summary>
    /// <param name="review">The review to update</param>
    /// <returns>The updated review</returns>
    Task<ReviewEntity?> UpdateAsync(ReviewEntity review);

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    /// <param name="id">The ID of the review to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    /// <param name="id">The ID of the review</param>
    /// <returns>The review if found, null otherwise</returns>
    Task<ReviewEntity?> GetByIdAsync(string id);

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
    /// <param name="reviewId">The review ID</param>
    /// <param name="reply">The reply to add</param>
    /// <returns>The updated review</returns>
    Task<ReviewEntity?> AddReplyAsync(string reviewId, ReplyEntity reply);

    /// <summary>
    /// Removes a reply from a review
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="replyId">The reply ID to remove</param>
    /// <returns>The updated review</returns>
    Task<ReviewEntity?> RemoveReplyAsync(string reviewId, string replyId);

    /// <summary>
    /// Updates a reply in a review
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="replyId">The reply ID to update</param>
    /// <param name="content">The new content for the reply</param>
    /// <returns>The updated review</returns>
    Task<ReviewEntity?> UpdateReplyAsync(string reviewId, string replyId, string content);

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
    /// Gets optimized statistics for a doctor (batch-friendly, no rating distribution)
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Review statistics without rating distribution</returns>
    Task<ReviewStatisticsResponse> GetDoctorStatisticsAsync(Guid doctorId);

    /// <summary>
    /// Gets optimized statistics for a service (batch-friendly, no rating distribution)
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
    /// Gets comprehensive statistics for multiple doctors in a single query
    /// </summary>
    /// <param name="doctorIds">List of doctor IDs</param>
    /// <returns>Batch statistics response for all doctors</returns>
    Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatisticsAsync(List<Guid> doctorIds);

    /// <summary>
    /// Gets comprehensive statistics for multiple services in a single query
    /// </summary>
    /// <param name="serviceIds">List of service IDs</param>
    /// <returns>Batch statistics response for all services</returns>
    Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(List<Guid> serviceIds);

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
    /// Gets optimized statistics for a hospital (batch-friendly, no rating distribution)
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
    /// Gets comprehensive statistics for multiple hospitals in a single query
    /// </summary>
    /// <param name="hospitalIds">List of hospital IDs</param>
    /// <returns>Batch statistics response for all hospitals</returns>
    Task<BatchHospitalsStatisticsResponse> GetBatchHospitalsStatisticsAsync(List<Guid> hospitalIds);

    /// <summary>
    /// Checks if a patient has already reviewed a specific doctor
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>The existing review if found, null otherwise</returns>
    Task<ReviewEntity?> GetExistingDoctorReviewAsync(Guid patientId, Guid doctorId);

    /// <summary>
    /// Checks if a patient has already reviewed a specific service
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="serviceId">The service ID</param>
    /// <returns>The existing review if found, null otherwise</returns>
    Task<ReviewEntity?> GetExistingServiceReviewAsync(Guid patientId, Guid serviceId);
}
