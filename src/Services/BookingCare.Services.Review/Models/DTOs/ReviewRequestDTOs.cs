using BookingCare.Services.Review.Models.Enums;

namespace BookingCare.Services.Review.Models.DTOs;

/// <summary>
/// Request DTO for creating a new review
/// </summary>
public class CreateReviewRequest
{
    /// <summary>
    /// ID of the patient creating the review
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Type of target being reviewed (DOCTOR or SERVICE)
    /// </summary>
    public TargetType TargetType { get; set; }

    /// <summary>
    /// ID of the doctor being reviewed (required if TargetType is DOCTOR)
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// ID of the clinic service being reviewed (required if TargetType is SERVICE)
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
}

/// <summary>
/// Request DTO for updating an existing review
/// </summary>
public class UpdateReviewRequest
{
    /// <summary>
    /// ID of the review to update
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Updated rating from 1 to 5 stars
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Updated comment content
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for adding a reply to a review
/// </summary>
public class AddReplyRequest
{
    /// <summary>
    /// ID of the review to reply to
    /// </summary>
    public string ReviewId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the author replying
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Content of the reply
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating an existing reply
/// </summary>
public class UpdateReplyRequest
{
    /// <summary>
    /// ID of the review containing the reply
    /// </summary>
    public string ReviewId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the reply to update
    /// </summary>
    public string ReplyId { get; set; } = string.Empty;

    /// <summary>
    /// Updated content of the reply
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for getting batch statistics for multiple doctors
/// </summary>
public class BatchDoctorsStatisticsRequest
{
    /// <summary>
    /// List of doctor IDs to get statistics for
    /// </summary>
    public List<Guid> DoctorIds { get; set; } = new();
}

/// <summary>
/// Request DTO for getting batch statistics for multiple clinic services
/// </summary>
public class BatchServicesStatisticsRequest
{
    /// <summary>
    /// List of clinic service IDs to get statistics for
    /// </summary>
    public List<Guid> ServiceIds { get; set; } = new();
}

/// <summary>
/// Request DTO for getting reviews with filters
/// </summary>
public class GetReviewsRequest
{
    /// <summary>
    /// Filter by patient ID
    /// </summary>
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Filter by doctor ID
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// Filter by clinic service ID
    /// </summary>
    public Guid? ClinicServiceId { get; set; }

    /// <summary>
    /// Filter by target type
    /// </summary>
    public TargetType? TargetType { get; set; }

    /// <summary>
    /// Minimum rating filter
    /// </summary>
    public int? MinRating { get; set; }

    /// <summary>
    /// Maximum rating filter
    /// </summary>
    public int? MaxRating { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;
}