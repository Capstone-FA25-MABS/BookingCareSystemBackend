using BookingCare.Services.Review.Common.Interfaces;
using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Service interface for Review operations
/// Extends IReviewQueryOperations for common query methods
/// </summary>
public interface IReviewService : IReviewQueryOperations
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
    /// Gets comprehensive statistics for multiple hospitals in a single request
    /// </summary>
    /// <param name="request">Batch hospitals statistics request</param>
    /// <returns>Batch statistics response for all hospitals</returns>
    Task<BatchHospitalsStatisticsResponse> GetBatchHospitalsStatisticsAsync(
        BatchHospitalsStatisticsRequest request
    );
}
