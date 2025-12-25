using BookingCare.Services.Review.Common.Interfaces;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Entities;

namespace BookingCare.Services.Review.Repositories.Interfaces;

/// <summary>
/// Repository interface for Review operations
/// Extends IReviewQueryOperations for common query methods
/// </summary>
public interface IReviewRepository : IReviewQueryOperations
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
    /// Gets comprehensive statistics for multiple doctors in a single query
    /// </summary>
    /// <param name="doctorIds">List of doctor IDs</param>
    /// <param name="hospitalId">Optional hospital ID to filter by</param>
    /// <returns>Batch statistics response for all doctors</returns>
    Task<BatchDoctorsStatisticsResponse> GetBatchDoctorsStatisticsAsync(
        List<Guid> doctorIds,
        Guid? hospitalId = null
    );

    /// <summary>
    /// Gets comprehensive statistics for multiple services in a single query
    /// </summary>
    /// <param name="serviceIds">List of service IDs</param>
    /// <param name="hospitalId">Optional hospital ID to filter by</param>
    /// <returns>Batch statistics response for all services</returns>
    Task<BatchServicesStatisticsResponse> GetBatchServicesStatisticsAsync(
        List<Guid> serviceIds,
        Guid? hospitalId = null
    );

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
