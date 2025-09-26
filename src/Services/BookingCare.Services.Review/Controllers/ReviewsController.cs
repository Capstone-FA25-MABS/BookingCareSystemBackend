using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Review.Controllers;

/// <summary>
/// Controller for Review operations
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class ReviewsController : BaseApiController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Creates a new review
    /// </summary>
    /// <param name="request">Review creation request</param>
    /// <returns>Created review</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        try
        {
            var result = await _reviewService.CreateReviewAsync(request);
            return Created(result, "Review created successfully");
        }
        catch (Exceptions.DuplicateReviewException ex)
        {
            var existingReview = await _reviewService.GetReviewByIdAsync(ex.ExistingReviewId);

            var errorResponse = new DuplicateReviewErrorResponse
            {
                Message = ex.Message,
                ExistingReview = existingReview ?? new ReviewResponse(),
                SuggestedAction = "Please update the existing review instead of creating a new one.",
                UpdateEndpoint = "/api/v1.0/reviews"
            };

            return Conflict(errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing review
    /// </summary>
    /// <param name="request">Review update request</param>
    /// <returns>Updated review</returns>
    [HttpPut]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
    {
        var result = await _reviewService.UpdateReviewAsync(request);
        return Success(result, "Review updated successfully");
    }

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    /// <param name="id">Review ID</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteReview(string id)
    {
        var result = await _reviewService.DeleteReviewAsync(id);
        return Success("Review deleted successfully");
    }

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    /// <param name="id">Review ID</param>
    /// <returns>Review details</returns>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReview(string id)
    {
        var result = await _reviewService.GetReviewByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Review with ID {id} not found");
        }
        return Success(result, "Review retrieved successfully");
    }

    /// <summary>
    /// Gets reviews with filtering and pagination
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <returns>Paginated reviews</returns>
    [HttpPost("search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReviews([FromBody] GetReviewsRequest request)
    {
        var result = await _reviewService.GetReviewsAsync(request);
        return Success(result, "Reviews retrieved successfully");
    }

    /// <summary>
    /// Gets reviews for a specific doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the doctor</returns>
    [HttpGet("doctor/{doctorId:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReviewsByDoctor(Guid doctorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetReviewsByDoctorAsync(doctorId, page, pageSize);
        return Success(result, "Doctor reviews retrieved successfully");
    }

    /// <summary>
    /// Gets reviews for a specific service
    /// </summary>
    /// <param name="serviceId">Service ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the service</returns>
    [HttpGet("service/{serviceId:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReviewsByService(Guid serviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetReviewsByServiceAsync(serviceId, page, pageSize);
        return Success(result, "Service reviews retrieved successfully");
    }

    /// <summary>
    /// Gets reviews by a specific patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews by the patient</returns>
    [HttpGet("patient/{patientId:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReviewsByPatient(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetReviewsByPatientAsync(patientId, page, pageSize);
        return Success(result, "Patient reviews retrieved successfully");
    }

    /// <summary>
    /// Adds a reply to a review
    /// </summary>
    /// <param name="request">Reply request</param>
    /// <returns>Updated review with the new reply</returns>
    [HttpPost("reply")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> AddReply([FromBody] AddReplyRequest request)
    {
        var result = await _reviewService.AddReplyAsync(request);
        return Success(result, "Reply added successfully");
    }

    /// <summary>
    /// Updates a reply in a review
    /// </summary>
    /// <param name="request">Update reply request</param>
    /// <returns>Updated review with the modified reply</returns>
    [HttpPut("reply")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateReply([FromBody] UpdateReplyRequest request)
    {
        var result = await _reviewService.UpdateReplyAsync(request);
        return Success(result, "Reply updated successfully");
    }

    /// <summary>
    /// Removes a reply from a review
    /// </summary>
    /// <param name="reviewId">Review ID</param>
    /// <param name="replyId">Reply ID</param>
    /// <returns>Updated review without the reply</returns>
    [HttpDelete("{reviewId}/reply/{replyId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemoveReply(string reviewId, string replyId)
    {
        var result = await _reviewService.RemoveReplyAsync(reviewId, replyId);
        return Success(result, "Reply removed successfully");
    }

    /// <summary>
    /// Gets the average rating for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Average rating</returns>
    [HttpGet("doctor/{doctorId:guid}/average-rating")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAverageRatingByDoctor(Guid doctorId)
    {
        var result = await _reviewService.GetAverageRatingByDoctorAsync(doctorId);
        return Success(new { DoctorId = doctorId, AverageRating = result }, "Average rating retrieved successfully");
    }

    /// <summary>
    /// Gets comprehensive statistics for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Complete statistics including average rating, count, and rating distribution</returns>
    [HttpGet("doctor/{doctorId:guid}/statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorStatistics(Guid doctorId)
    {
        var result = await _reviewService.GetDoctorDetailedStatisticsAsync(doctorId);
        return Success(result, "Doctor statistics retrieved successfully");
    }

    /// <summary>
    /// Gets the average rating for a service
    /// </summary>
    /// <param name="serviceId">Service ID</param>
    /// <returns>Average rating</returns>
    [HttpGet("service/{serviceId:guid}/average-rating")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAverageRatingByService(Guid serviceId)
    {
        var result = await _reviewService.GetAverageRatingByServiceAsync(serviceId);
        return Success(new { ServiceId = serviceId, AverageRating = result }, "Average rating retrieved successfully");
    }

    /// <summary>
    /// Gets comprehensive statistics for a service
    /// </summary>
    /// <param name="serviceId">Service ID</param>
    /// <returns>Complete statistics including average rating, count, and rating distribution</returns>
    [HttpGet("service/{serviceId:guid}/statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetServiceStatistics(Guid serviceId)
    {
        var result = await _reviewService.GetServiceDetailedStatisticsAsync(serviceId);
        return Success(result, "Service statistics retrieved successfully");
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple doctors in a single request
    /// </summary>
    /// <param name="request">Batch doctors statistics request</param>
    /// <returns>Complete statistics for all requested doctors</returns>
    [HttpPost("doctors/batch-statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBatchDoctorsStatistics([FromBody] BatchDoctorsStatisticsRequest request)
    {
        var result = await _reviewService.GetBatchDoctorsStatisticsAsync(request);
        return Success(result, $"Batch doctor statistics retrieved successfully for {result.DoctorStatistics.Count} doctors");
    }

    /// <summary>
    /// Gets comprehensive statistics for multiple clinic services in a single request
    /// </summary>
    /// <param name="request">Batch services statistics request</param>
    /// <returns>Complete statistics for all requested services</returns>
    [HttpPost("services/batch-statistics")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetBatchServicesStatistics([FromBody] BatchServicesStatisticsRequest request)
    {
        var result = await _reviewService.GetBatchServicesStatisticsAsync(request);
        return Success(result, $"Batch service statistics retrieved successfully for {result.ServiceStatistics.Count} services");
    }

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Review count</returns>
    [HttpGet("doctor/{doctorId:guid}/count")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetReviewCountByDoctor(Guid doctorId)
    {
        var result = await _reviewService.GetReviewCountByDoctorAsync(doctorId);
        return Success(new { DoctorId = doctorId, ReviewCount = result }, "Review count retrieved successfully");
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        var healthData = new
        {
            Status = "Healthy",
            Service = "Review",
            Timestamp = DateTime.UtcNow,
            Version = "1.0"
        };
        return Success(healthData, "Review service is healthy");
    }
}