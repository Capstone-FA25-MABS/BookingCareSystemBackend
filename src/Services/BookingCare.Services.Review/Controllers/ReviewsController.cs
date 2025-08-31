using BookingCare.Shared.Common.Controllers;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Review.Controllers;

/// <summary>
/// Controller for Review operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : BaseApiController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Review", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Creates a new review
    /// </summary>
    /// <param name="request">Review creation request</param>
    /// <returns>Created review</returns>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateReviewAsync(request);
        return Created(result, "Review created successfully");
    }

    /// <summary>
    /// Updates an existing review
    /// </summary>
    /// <param name="request">Review update request</param>
    /// <returns>Updated review</returns>
    [HttpPut]
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
    public async Task<IActionResult> GetReviewsByDoctor(Guid doctorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetReviewsByDoctorAsync(doctorId, page, pageSize);
        return Success(result, "Doctor reviews retrieved successfully");
    }

    /// <summary>
    /// Gets reviews for a specific clinic service
    /// </summary>
    /// <param name="clinicServiceId">Clinic service ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated reviews for the clinic service</returns>
    [HttpGet("service/{clinicServiceId:guid}")]
    public async Task<IActionResult> GetReviewsByClinicService(Guid clinicServiceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetReviewsByClinicServiceAsync(clinicServiceId, page, pageSize);
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
    public async Task<IActionResult> GetAverageRatingByDoctor(Guid doctorId)
    {
        var result = await _reviewService.GetAverageRatingByDoctorAsync(doctorId);
        return Success(new { DoctorId = doctorId, AverageRating = result }, "Average rating retrieved successfully");
    }

    /// <summary>
    /// Gets the average rating for a clinic service
    /// </summary>
    /// <param name="clinicServiceId">Clinic service ID</param>
    /// <returns>Average rating</returns>
    [HttpGet("service/{clinicServiceId:guid}/average-rating")]
    public async Task<IActionResult> GetAverageRatingByService(Guid clinicServiceId)
    {
        var result = await _reviewService.GetAverageRatingByClinicServiceAsync(clinicServiceId);
        return Success(new { ClinicServiceId = clinicServiceId, AverageRating = result }, "Average rating retrieved successfully");
    }

    /// <summary>
    /// Gets the total count of reviews for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Review count</returns>
    [HttpGet("doctor/{doctorId:guid}/count")]
    public async Task<IActionResult> GetReviewCountByDoctor(Guid doctorId)
    {
        var result = await _reviewService.GetReviewCountByDoctorAsync(doctorId);
        return Success(new { DoctorId = doctorId, ReviewCount = result }, "Review count retrieved successfully");
    }

    /// <summary>
    /// Gets the total count of reviews for a clinic service
    /// </summary>
    /// <param name="clinicServiceId">Clinic service ID</param>
    /// <returns>Review count</returns>
    [HttpGet("service/{clinicServiceId:guid}/count")]
    public async Task<IActionResult> GetReviewCountByService(Guid clinicServiceId)
    {
        var result = await _reviewService.GetReviewCountByClinicServiceAsync(clinicServiceId);
        return Success(new { ClinicServiceId = clinicServiceId, ReviewCount = result }, "Review count retrieved successfully");
    }
}