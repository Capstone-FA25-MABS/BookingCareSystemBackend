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
    Task<PagedReviewsResponse> GetReviewsByDoctorAsync(Guid doctorId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets reviews for a specific clinic service
    /// </summary>
    /// <param name="clinicServiceId">The clinic service ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the clinic service</returns>
    Task<PagedReviewsResponse> GetReviewsByClinicServiceAsync(Guid clinicServiceId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews by the patient</returns>
    Task<PagedReviewsResponse> GetReviewsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10);

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
    /// <returns>The average rating</returns>
    Task<double> GetAverageRatingByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the average rating for a clinic service
    /// </summary>
    /// <param name="clinicServiceId">The clinic service ID</param>
    /// <returns>The average rating</returns>
    Task<double> GetAverageRatingByClinicServiceAsync(Guid clinicServiceId);

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>The total count of reviews</returns>
    Task<long> GetReviewCountByDoctorAsync(Guid doctorId);

    /// <summary>
    /// Gets the total count of reviews for a clinic service
    /// </summary>
    /// <param name="clinicServiceId">The clinic service ID</param>
    /// <returns>The total count of reviews</returns>
    Task<long> GetReviewCountByClinicServiceAsync(Guid clinicServiceId);
}