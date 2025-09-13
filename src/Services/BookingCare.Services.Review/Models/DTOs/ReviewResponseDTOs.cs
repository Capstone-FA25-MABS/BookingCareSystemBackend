using BookingCare.Services.Review.Enums;

namespace BookingCare.Services.Review.Models.DTOs;

/// <summary>
/// Response DTO for a reply
/// </summary>
public class ReplyResponse
{
    /// <summary>
    /// Unique identifier for the reply
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the author who replied
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Content of the reply
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the reply was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the reply was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for review statistics
/// </summary>
public class ReviewStatisticsResponse
{
    /// <summary>
    /// ID of the target (Doctor or Service)
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// Type of target being reviewed
    /// </summary>
    public TargetType TargetType { get; set; }

    /// <summary>
    /// Average rating (0.0 - 5.0)
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews
    /// </summary>
    public long TotalReviews { get; set; }

    /// <summary>
    /// Rating distribution breakdown
    /// </summary>
    public Dictionary<int, long> RatingDistribution { get; set; } = new();
}

/// <summary>
/// Response DTO for duplicate review error
/// </summary>
public class DuplicateReviewErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The existing review that conflicts
    /// </summary>
    public ReviewResponse ExistingReview { get; set; } = new();

    /// <summary>
    /// Suggested action for the client
    /// </summary>
    public string SuggestedAction { get; set; } = "Please update the existing review instead of creating a new one.";

    /// <summary>
    /// Update endpoint URL
    /// </summary>
    public string UpdateEndpoint { get; set; } = "/api/reviews";
}

/// <summary>
/// Response DTO for batch doctors statistics
/// </summary>
public class BatchDoctorsStatisticsResponse
{
    /// <summary>
    /// Dictionary mapping doctor ID to their statistics
    /// </summary>
    public Dictionary<Guid, ReviewStatisticsResponse> DoctorStatistics { get; set; } = new();

    /// <summary>
    /// List of doctor IDs that were not found or have no reviews
    /// </summary>
    public List<Guid> NotFoundDoctorIds { get; set; } = new();

    /// <summary>
    /// Total number of doctors processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of doctors with statistics
    /// </summary>
    public int WithStatistics { get; set; }
}

/// <summary>
/// Response DTO for batch services statistics
/// </summary>
public class BatchServicesStatisticsResponse
{
    /// <summary>
    /// Dictionary mapping service ID to their statistics
    /// </summary>
    public Dictionary<Guid, ReviewStatisticsResponse> ServiceStatistics { get; set; } = new();

    /// <summary>
    /// List of service IDs that were not found or have no reviews
    /// </summary>
    public List<Guid> NotFoundServiceIds { get; set; } = new();

    /// <summary>
    /// Total number of services processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of services with statistics
    /// </summary>
    public int WithStatistics { get; set; }
}

/// <summary>
/// Response DTO for a review
/// </summary>
public class ReviewResponse
{
    /// <summary>
    /// Unique identifier for the review
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the patient who created the review
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Type of target being reviewed
    /// </summary>
    public TargetType TargetType { get; set; }

    /// <summary>
    /// ID of the doctor being reviewed (null if reviewing service)
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// ID of the clinic service being reviewed (null if reviewing doctor)
    /// </summary>
    public Guid? ClinicServiceId { get; set; }

    /// <summary>
    /// Rating from 1 to 5 stars
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Comment content
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Replies to this review
    /// </summary>
    public List<ReplyResponse> Replies { get; set; } = new();

    /// <summary>
    /// When the review was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the review was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for paginated reviews
/// </summary>
public class PagedReviewsResponse
{
    /// <summary>
    /// List of reviews
    /// </summary>
    public List<ReviewResponse> Reviews { get; set; } = new();

    /// <summary>
    /// Total number of reviews
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
}